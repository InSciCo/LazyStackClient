
namespace LazyStack.Client.Base;
public interface ILzClientConfig
{

    JObject AuthConfig { get; set; }    
    Task ReadConfigAsync(string configFilePath);
}