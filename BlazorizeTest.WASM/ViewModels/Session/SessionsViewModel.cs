namespace BlazorizeTest.ViewModels;
[Factory]

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
        
    }
    private ISessionViewModelFactory _sessionViewModelFactory;
    private ILzHost _host;

    public override ISessionViewModel CreateSessionViewModel()
    {
        return _sessionViewModelFactory.Create(OSAccess, ClientConfig!, InternetConnectivity!);
    }

    // ReadConfigAsync is called from InitAsync() just prior to the IsInitialized being set to true.
    public override async Task ReadConfigAsync()
    {
        await ClientConfig!.ReadAuthConfigAsync(_host.AssetsUrl + "config", "employeeuserpool");
        //await ClientConfig.ReadTenancyConfigAsync("tenancyconfig.json");
        await Messages.SetMessageSetAsync("en-US", LzMessageUnits.Imperial);
        await Task.Delay(0);
    }
}
