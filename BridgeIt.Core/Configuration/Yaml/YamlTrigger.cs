using YamlDotNet.Serialization;

namespace BridgeIt.Core.Configuration.Yaml;

public class YamlTrigger
{
    public Dictionary<string, object> Conditions { get; set; }
}