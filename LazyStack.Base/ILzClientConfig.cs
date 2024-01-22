
namespace LazyStack.Base;
public interface ILzClientConfig
{
    string Account { get; set; }
    Dictionary<string, JObject> Authenticators { get; set; }
    string DefaultService { get; set; }
    string Profile { get; set; }
    string Region { get; set; }
    Dictionary<string, string> RelatedResources { get; set; }
    JObject Resources { get; set; }
    LzRunConfig RunConfig { get; set; }
    Dictionary<string, LzService> Services { get; set; }
    string StackName { get; set; }
    JObject VARS { get; set; }
    Task ReadConfigAsync(string configFilePath);
}