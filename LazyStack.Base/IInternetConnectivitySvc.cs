namespace LazyStack.Base;
public interface IInternetConnectivitySvc : IDisposable, INotifyPropertyChanged
{
    event Action<bool> NetworkStatusChanged;
    bool IsOnline { get; }
    Task<bool> CheckInternetConnectivityAsync();
}
