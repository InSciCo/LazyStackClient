using LazyStack.Base;

namespace LazyStack.ViewModels;

public class LzSessionsViewModel<T> : LzViewModel, ILzSessionsViewModel<T>
    where T : ILzSessionViewModel
{
    public LzSessionsViewModel(
        ILzMessages messages
        )
    {
        Messages = messages;
        Messages.MessageFiles = new List<string>() { };

        this.WhenAnyValue(x => x.InternetConnectivity!.IsOnline)
            .Select(x => { Console.WriteLine("IsOnline:" + x); return x; })
            .ToPropertyEx(this, x => x.IsOnline);
    }
    public virtual T? SessionViewModel { get; set; }
    private Dictionary<string, T> _sessions = new();
    public ILzMessages Messages { get; set; }
    public IOSAccess OSAccess { get; set; } = null!;
    public IInternetConnectivitySvc? InternetConnectivity { get; set; }
    public ILzClientConfig? ClientConfig { get; set; } = null!;
    public Dictionary<string, ILzClientConfig> ClientConfigs { get; set; } = new(); // profiles 
    [Reactive] public bool IsInitialized { get; protected set; }
    [ObservableAsProperty] public bool IsOnline { get; }
    protected readonly CompositeDisposable sessionDisposables = new();
    //public virtual async Task InitAsync(IOSAccess osAccess, ILzClientConfig clientConfig, IInternetConnectivitySvc internetConnectivitySvc)
    public virtual async Task InitAsync(IOSAccess osAccess, IInternetConnectivitySvc internetConnectivitySvc)
	{
        await Task.Delay(0);
        // The objects passed in to init are those that must be created within a WebView context.
        if (osAccess == null) throw new Exception("OSAccess is null");
        if (internetConnectivitySvc == null) throw new Exception("InternetConnectivitySvc is null");
        OSAccess = osAccess;
        InternetConnectivity = internetConnectivitySvc;
        Messages.SetOSAccess(OSAccess); // allows MessageSets to be read from configuration files
        await ReadConfigAsync();
        IsInitialized = true;
    }

    /// <summary>
    /// ReadConfigAsync() reads the client configuration from the _content/Config folder.
    /// _content/Config refers to the wwwroot folder in the Config Razor Class Library.
    /// When the application is being served from a AWS CloudFront distribution, the 
    /// _content/Config folder is served from an S3 bucket
    /// When running in a local development environment, the _content/Config folder is 
    /// read from the local Config Razor Class Library.
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public virtual async Task ReadConfigAsync()
    {
        if (OSAccess == null) throw new Exception("OSAccess is null in SessionsViewModel.InitAsync()");

        // Load one or more configuration profiles. 
        var configManifestJson = await OSAccess.ReadContentAsync("_content/Config/configfiles.json");
        var configManifest = (configManifestJson is null) 
            ? new List<string>() 
            : JsonConvert.DeserializeObject<List<string>>(configManifestJson);   

        if (configManifest is null || configManifest.Count == 0)
        {
            // Read global client configuration "config.json".  If no profile configurations are 
            // found, the global configuration is used as the default. Generally, when config.json
            // is loaded from an S3 bucket, it is the only configuration file.
            // The other configuration profiles are mainly useful for local development and testing.
            // and are stored in the Config Razor Class Library as non-tracked files.
            // Use the LazyStack Generate Stack Configuration tool to create the {profile}.config.json
            // files.
            var clientConfigJson = await OSAccess.ReadConfigAsync("_content/Config/config.json");
            var clientConfig = new LzClientConfig();
            clientConfig.Profile = "default";
            JsonConvert.PopulateObject(clientConfigJson, clientConfig);
            ClientConfigs.Add(clientConfig.Profile, clientConfig);
            ClientConfig = clientConfig; // use the default client config
        }
        else
            foreach (var profileConfigFile in configManifest)
            {
                var profileConfig = new LzClientConfig();
                var filePath = Path.Combine("_content/Config", profileConfigFile);
                var json = await OSAccess.ReadConfigAsync(filePath);
                JsonConvert.PopulateObject(json, profileConfig);
                ClientConfigs.Add(profileConfig.Profile, profileConfig);
                ClientConfig ??= profileConfig; // assign first profile config as the current client config
            }
    }
    public virtual async Task CreateSessionAsync()
    {
        if (SessionViewModel != null) return;
        if (!IsInitialized) throw new Exception("SessionsViewModel not initialized");

        try
        {
            var online = await InternetConnectivity!.CheckInternetConnectivityAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        //await ClientConfig!.ReadConfigAsync("_content/Tenancy/config.json");
        //await ClientConfig.ReadConfigAsync("_content/Tenancy/config.webapi.json");

        //await Messages.SetMessageSetAsync(new LzMessageSet("en-US", LzMessageUnits.Imperial));
        ////var sessionViewModel = _sessionViewModelFactory.Create(OSAccess, ClientConfig, InternetConnectivity!);
        T sessionViewModel = CreateSessionViewModel();
        await sessionViewModel.InitAsync();
        SessionViewModel = sessionViewModel;
    }
    public virtual T CreateSessionViewModel() { throw new NotImplementedException(); }
    public virtual async Task DeleteAsync(string sessionId)
    {
        await Task.Delay(0);
        sessionDisposables.Dispose();
        if (!_sessions.ContainsKey(sessionId))
            throw new Exception("Bad session id");
        _sessions.Remove(sessionId);
    }
    public virtual async Task SetAsync(string sessionId)
    {
        await Task.Delay(0);
        if (_sessions.ContainsKey(sessionId))
            SessionViewModel = _sessions[sessionId];
        else throw new Exception("Bad session id");
    }
    public IDictionary<string, string> SessionLogins => _sessions.ToDictionary(x => x.Key, x => x.Value.SessionName);
}
