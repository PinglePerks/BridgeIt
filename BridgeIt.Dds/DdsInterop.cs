using System.Reflection;
using System.Runtime.InteropServices;

namespace BridgeIt.Dds;

/// <summary>
/// P/Invoke bindings for Bo Haglund's DDS library (v2.9.0).
///
/// Native library locations:
///   macOS:   runtimes/osx-arm64/native/libdds.dylib
///   Linux:   runtimes/linux-x64/native/libdds.so
///   Windows: runtimes/win-x64/native/dds.dll
/// </summary>
public static class DdsInterop
{
    private const string LibName = "dds";

    /// <summary>
    /// Register a custom resolver so .NET can find libdds in the runtimes/ folder.
    /// Must be called before any P/Invoke call (handled by DdsService constructor).
    /// </summary>
    static DdsInterop()
    {
        NativeLibrary.SetDllImportResolver(typeof(DdsInterop).Assembly, ResolveNativeLibrary);
    }

    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibName) return IntPtr.Zero;

        // Try standard resolution first
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle))
            return handle;

        // Probe runtimes/<rid>/native/ next to the assembly
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? ".";
        var rid = RuntimeInformation.RuntimeIdentifier;

        // Try exact RID
        var candidate = Path.Combine(assemblyDir, "runtimes", rid, "native", $"libdds.dylib");
        if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out handle))
            return handle;

        // Try osx-arm64 explicitly (common on Apple Silicon)
        candidate = Path.Combine(assemblyDir, "runtimes", "osx-arm64", "native", "libdds.dylib");
        if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out handle))
            return handle;

        // Try .so extension (Linux or macOS .so)
        candidate = Path.Combine(assemblyDir, "runtimes", rid, "native", "libdds.so");
        if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out handle))
            return handle;

        return IntPtr.Zero;
    }

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Mandatory first call. Pass 0 to let DDS auto-detect thread count.
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetMaxThreads(int userThreads);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FreeMemory();

    // ─── Trick Table ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculate the full double-dummy trick table (4 declarers × 5 strains)
    /// for a single deal supplied in PBN format.
    /// Returns RETURN_NO_FAULT (1) on success.
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcDDtablePBN(
        DdTableDealPbn tableDealPBN,
        out DdTableResults tablep);

    // ─── Par ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculate par result for a given dealer and vulnerability using the structured
    /// parResultsMaster output (includes contractType details).
    ///
    /// dealer:     0=North, 1=East, 2=South, 3=West
    /// vulnerable: 0=None, 1=Both, 2=NS only, 3=EW only
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int DealerParBin(
        ref DdTableResults tablep,
        out ParResultsMaster presp,
        int dealer,
        int vulnerable);

    // ─── Error ───────────────────────────────────────────────────────────────────

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ErrorMessage(int code, [Out] byte[] line);

    public static string GetErrorMessage(int code)
    {
        var buf = new byte[80];
        ErrorMessage(code, buf);
        var str = System.Text.Encoding.ASCII.GetString(buf);
        var idx = str.IndexOf('\0');
        return idx >= 0 ? str[..idx] : str;
    }

    // ─── Info ────────────────────────────────────────────────────────────────────

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetDDSInfo(out DdsInfo info);

    // ─── Constants ───────────────────────────────────────────────────────────────

    public const int ReturnNoFault = 1;

    /// <summary>
    /// DDS strain indices: 0=Spades, 1=Hearts, 2=Diamonds, 3=Clubs, 4=NT.
    /// DDS seat indices:   0=North, 1=East, 2=South, 3=West.
    /// </summary>
    public const int DdsHands = 4;
    public const int DdsSuits = 4;
    public const int DdsStrains = 5;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Interop structs — must match dll.h layout exactly
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// PBN format deal for CalcDDtablePBN.
/// cards: "N:AKQJ.T987.654.32 T987.654.32.AKQJ ..."
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DdTableDealPbn
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
    public byte[] Cards;

    public DdTableDealPbn(string pbn)
    {
        Cards = new byte[80];
        var bytes = System.Text.Encoding.ASCII.GetBytes(pbn);
        Array.Copy(bytes, Cards, Math.Min(bytes.Length, 79));
    }
}

/// <summary>
/// Result table: resTable[strain][hand] = tricks.
/// strain: 0=Spades, 1=Hearts, 2=Diamonds, 3=Clubs, 4=NT.
/// hand:   0=North,  1=East,   2=South,    3=West.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DdTableResults
{
    /// <summary>
    /// 5 strains × 4 hands, flattened. Access: resTable[strain * 4 + hand].
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public int[] ResTable;
}

/// <summary>
/// Detailed par contract info returned by DealerParBin.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ContractType
{
    public int UnderTricks; // 0 = make, 1-13 = sacrifice
    public int OverTricks;  // 0-3
    public int Level;       // 1-7
    public int Denom;       // 0=NT, 1=S, 2=H, 3=D, 4=C
    public int Seats;       // 0=N, 1=E, 2=S, 3=W, 4=NS, 5=EW
}

/// <summary>
/// DealerParBin output: par score + up to 10 par contracts.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParResultsMaster
{
    public int Score;   // Signed from N/S perspective
    public int Number;  // Number of contracts giving par

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public ContractType[] Contracts;
}

[StructLayout(LayoutKind.Sequential)]
public struct DdsInfo
{
    public int Major, Minor, Patch;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public byte[] VersionString;

    public int System;
    public int NumBits;
    public int Compiler;
    public int Constructor;
    public int NumCores;
    public int Threading;
    public int NoOfThreads;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    public byte[] ThreadSizes;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public byte[] SystemString;
}
