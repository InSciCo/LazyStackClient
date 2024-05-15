namespace LazyStack.Client.Base;

public class LzClientConfig : ILzClientConfig
{
    public LzClientConfig(ILzHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }
    public JObject AuthConfig { get; set; } = new();
    public JObject TenancyConfig { get; set; } = JObject.Parse("{}");   

    protected IOSAccess _oSAccess;
    protected ILzHost _host;

    public bool ConfigureError { get; set; }
    public bool ConfigFound { get; set; }


    public void SetOSAccess(IOSAccess osAccess)
    {
        _oSAccess = osAccess ?? throw new ArgumentNullException(nameof(osAccess));
    }

    /// <summary>
    /// You may call ReadTenancyConfigAsync multiple times with different paths. Config content 
    /// are merged with last value(s) found taking precedence.
    /// </summary>
    /// <param name="tenancyConfigPath"></param>
    /// <returns></returns>
    public virtual async Task ReadTenancyConfigAsync(string tenancyConfigPath)
    {
        try
        {
            if (_oSAccess == null)
                throw new Exception("OSAccess not set.");

            var json = await _oSAccess!.ReadTenancyConfigAsync(tenancyConfigPath);
            TenancyConfig.Merge(JObject.Parse(json), new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
            //JsonConvert.PopulateObject(json, TenancyConfig); // for some reason, this doesn't work
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading tenancy config: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads the AuthConfig file and sets the AuthConfig property based on the provided userPoolName.
    /// </summary>
    /// <param name="authConfigPath"></param>
    /// <param name="userPoolName"></param>
    /// <returns></returns>
    public virtual async Task ReadAuthConfigAsync(string authConfigPath, string? userPoolName = null)
    {
        try
        {
            if (_oSAccess == null)
                throw new Exception("OSAccess not set.");

            var authConfigJson = await _oSAccess!.ReadAuthConfigAsync(authConfigPath);
            var authConfigs = JArray.Parse(authConfigJson);

            if (!string.IsNullOrEmpty(userPoolName))
            {
                foreach (var authConfig in authConfigs)
                {
                    if (authConfig["userPoolName"]!.ToString().ToLower() == userPoolName)
                    {
                        AuthConfig = (JObject)authConfig;
                        var wsUrl = authConfig["wsUrl"]!.ToString();
                        if (!string.IsNullOrEmpty(wsUrl))
                            _host.WsUrl = wsUrl;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading AuthConfig: {ex.Message}");
        }
    }

}



