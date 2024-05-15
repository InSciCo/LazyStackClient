namespace BlazorTest.ViewModels;
[Factory]
public class SessionsViewModel : LzSessionsViewModelAuth<ISessionViewModel>, ISessionsViewModel
{
    public SessionsViewModel(
        ILzMessages messages,
        ISessionViewModelFactory sessionViewModelFactory,
        ILzClientConfig clientConfig
        ) : base(messages)
    {
        _sessionViewModelFactory = sessionViewModelFactory;
        ClientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
    }
    private ISessionViewModelFactory _sessionViewModelFactory;

    public override ISessionViewModel CreateSessionViewModel()
    {
        return _sessionViewModelFactory.Create(OSAccess, ClientConfig!, InternetConnectivity!);
    }

    // ReadConfigAsync is called from InitAsync() just prior to the IsInitialized being set to true.
    public override async Task ReadConfigAsync()
    {
        await ClientConfig!.ReadAuthConfigAsync("authconfig.json", "employeeuserpool");
        await ClientConfig.ReadTenancyConfigAsync("tenancyconfig.json");
        await Messages.SetMessageSetAsync(new LzMessageSet("en-US", LzMessageUnits.Imperial));
    }
}
