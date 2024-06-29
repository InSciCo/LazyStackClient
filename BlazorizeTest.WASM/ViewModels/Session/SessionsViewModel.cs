namespace BlazorizeTest.ViewModels;
public class SessionsViewModel : LzSessionsViewModelAuth<ISessionViewModel>, ISessionsViewModel
{
    public SessionsViewModel(
        ILzMessages messages,
        ISessionViewModelFactory sessionViewModelFactory,
        ILzClientConfig clientConfig,
        ILzHost host
        ) : base(messages)
    {
        _sessionViewModelFactory = sessionViewModelFactory;
        _host = host;   
        ClientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
        IsInitialized = true;
        
    }
    private ISessionViewModelFactory _sessionViewModelFactory;
    private ILzHost _host;

    public override ISessionViewModel CreateSessionViewModel()
    {
        return _sessionViewModelFactory.Create();
    }

}
