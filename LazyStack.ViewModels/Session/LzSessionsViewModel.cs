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
    [Reactive] public bool IsInitialized { get; protected set; }
    [ObservableAsProperty] public bool IsOnline { get; }
    protected readonly CompositeDisposable sessionDisposables = new();
    public virtual async Task InitAsync(IOSAccess osAccess, ILzClientConfig clientConfig, IInternetConnectivitySvc internetConnectivitySvc)
    {
        await Task.Delay(0);
        // The objects passed in to init are those that must be created within a WebView context.
        if (osAccess == null) throw new Exception("OSAccess is null");
        if (clientConfig == null) throw new Exception("ClientConfig is null");
        if (internetConnectivitySvc == null) throw new Exception("InternetConnectivitySvc is null");
        OSAccess = osAccess;
        ClientConfig = clientConfig;
        InternetConnectivity = internetConnectivitySvc;
        Messages.SetOSAccess(OSAccess); // allows MessageSets to be read from configuration files
        await ReadConfigAsync();
        IsInitialized = true;
    }

    public virtual async Task ReadConfigAsync()
    {
        await Task.Delay(0);
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
