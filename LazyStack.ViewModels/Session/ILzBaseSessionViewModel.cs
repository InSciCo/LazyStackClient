using System.ComponentModel;

namespace LazyStack.ViewModels;

public interface ILzBaseSessionViewModel : INotifyPropertyChanged
{
    bool IsSignedIn { get; }
    IInternetConnectivitySvc InternetConnectivity { get; set; }
    ILzClientConfig ClientConfig { get; set; }
    IOSAccess OSAccess { get; set; }  
    string SessionId { get; set; }
    bool HasChallenge { get; }
    bool IsAdmin { get; }
    bool IsBusy { get; }
    bool IsOnline { get; }
    bool IsLoaded { get; set; }
    bool IsLoading { get; set; }
    LzMessageSet MessageSet { get; set; }    
    Task<bool> CheckInternetConnectivityAsync();
    Task LoadAsync();
    Task OnSignedInAsync();
    Task OnSignedOutAsync();
    Task UnloadAsync();

}
