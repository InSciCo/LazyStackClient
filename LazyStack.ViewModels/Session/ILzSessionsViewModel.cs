using System.ComponentModel;

namespace LazyStack.ViewModels;
public interface ILzSessionsViewModel<T>: INotifyPropertyChanged
    where T : ILzSessionViewModel
{
    IDictionary<string, string> SessionLogins { get; }
    T? SessionViewModel { get; set; }
    bool IsInitialized { get; }
    bool IsOnline { get; }
    Task CreateSessionAsync();
    Task DeleteAsync(string sessionId);
    Task SetAsync(string sessionId);
    Task InitAsync(IOSAccess osAccess, ILzClientConfig clientConfig, IInternetConnectivitySvc internetConnectivitySvc);
    Task ReadConfigAsync();
}