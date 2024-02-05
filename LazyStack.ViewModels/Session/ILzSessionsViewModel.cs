using System.ComponentModel;

namespace LazyStack.ViewModels;
public interface ILzSessionsViewModel<T>: INotifyPropertyChanged
    where T : ILzSessionViewModel
{
    IDictionary<string, string> SessionLogins { get; }
    T? SessionViewModel { get; set; }
    bool IsInitialized { get; }
    bool IsOnline { get; }
    ILzMessages Messages { get; set; }
    IOSAccess OSAccess { get; set; } 
    IInternetConnectivitySvc? InternetConnectivity { get; set; }
    ILzClientConfig? ClientConfig { get; set; }
    Dictionary<string, ILzClientConfig> ClientConfigs { get; set; } // profiles 
    Task CreateSessionAsync();
    Task DeleteAsync(string sessionId);
    Task SetAsync(string sessionId);
	Task InitAsync(IOSAccess osAccess, IInternetConnectivitySvc internetConnectivitySvc);
	Task ReadConfigAsync();
}