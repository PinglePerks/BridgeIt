namespace BridgeIt.Core.Configuration.Yaml;

public class YamlSystem
{
    public string SystemName { get; set; }
    public int Priority { get; set; }
    public YamlTrigger Trigger { get; set; }
    public string Description { get; set; }
    public List<YamlNode> Nodes { get; set; }
}