
namespace LazyStack.Client.Base;
public interface ILzClientConfig
{
    bool ConfigureError { get; set; }
    bool ConfigFound { get; set; }

    JObject AuthConfig { get; set; }    
    JObject TenancyConfig { get; set; } 
    Task ReadAuthConfigAsync(string configFilePath, string userPoolName);
    Task ReadTenancyConfigAsync(string configFilePath);
    void SetOSAccess(IOSAccess osAccess);   
}