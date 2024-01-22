namespace LazyStack.Base;

public class LzClientConfig : ILzClientConfig
{
    public string Profile { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string StackName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, JObject> Authenticators { get; set; } = new();
    public string DefaultService { get; set; } = string.Empty;
    public Dictionary<string, LzService> Services { get; set; } = new();
    public LzRunConfig RunConfig { get; set; } = new();
    public Dictionary<string, string> RelatedResources { get; set; } = new();
    public JObject Resources { get; set; } = new();
    public JObject VARS { get; set; } = new();

    public virtual async Task ReadConfigAsync(string configFilePath)
    {
        await Task.Delay(0);
    }

}



