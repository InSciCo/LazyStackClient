using LazyStack.Client.Base;

namespace LazyStack.Client.ViewModels;

public abstract class LzSessionsViewModel<T> : LzViewModel, ILzSessionsViewModel<T>
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
    [Reactive] public virtual T? SessionViewModel { get; set; }
    private Dictionary<string, T> _sessions = new();
    public ILzMessages Messages { get; set; }
    public IOSAccess OSAccess { get; set; } = null!;
    public IInternetConnectivitySvc? InternetConnectivity { get; set; }
    public ILzClientConfig? ClientConfig { get; set; } = null!;
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
        ClientConfig!.SetOSAccess(OSAccess);    // allows ClientConfig to read configuration files
        await ReadConfigAsync();
        IsInitialized = true;
    }

    /// <summary>
    /// Implement ReadConfigAsync in your derived class to read the configuration file(s).
    /// Example:
    ///    ClientConfig.ReadAuthConfigAsync("path", "employeeuserpool");
    ///    // You can call ReadTenancyConfigAsync multiple times with different paths. Config content
    ///    // is merged.
    ///    ClientConfig.ReadTenancyConfigAsync("..");
    /// </summary>
    /// <returns></returns>
    public abstract Task ReadConfigAsync();

    public virtual async Task<bool> CreateSessionAsync()
    {
        if (SessionViewModel != null) return false;
        if (!IsInitialized) throw new Exception("SessionsViewModel not initialized");

        try
        {
            var online = await InternetConnectivity!.CheckInternetConnectivityAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        T sessionViewModel = CreateSessionViewModel();
        await sessionViewModel.InitAsync();
        SessionViewModel = sessionViewModel;
        return true;
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
