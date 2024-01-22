using System.ComponentModel;

namespace LazyStack.ViewModels;

public interface ILzSessionViewModel : INotifyPropertyChanged
{
    IInternetConnectivitySvc InternetConnectivity { get; set; }
    ILzClientConfig ClientConfig { get; set; }
    IOSAccess OSAccess { get; set; }
    string SessionName { get; set; }
    string SessionId { get; set; }
    bool IsOnline { get; }
    bool IsLoaded { get; set; }
    bool IsLoading { get; set; }
    LzMessageSet MessageSet { get; set; }
    Task InitAsync();
    Task<bool> CheckInternetConnectivityAsync();
    Task LoadAsync();
    Task UnloadAsync();


}
