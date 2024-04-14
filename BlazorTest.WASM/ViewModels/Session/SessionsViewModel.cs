namespace BlazorTest.ViewModels;
[Factory]
public class SessionsViewModel : LzSessionsViewModelAuth<ISessionViewModel>, ISessionsViewModel
{
    public SessionsViewModel(
        ILzMessages messages,
        ISessionViewModelFactory sessionViewModelFactory
        ) : base(messages)
    {
        _sessionViewModelFactory = sessionViewModelFactory;
    }
    private ISessionViewModelFactory _sessionViewModelFactory;

    public override ISessionViewModel CreateSessionViewModel()
    {
        return _sessionViewModelFactory.Create(OSAccess, ClientConfig!, InternetConnectivity!);
    }

    // ReadConfigAsync is called from InitAsync() just prior to the IsInitialized being set to true.
    public override async Task ReadConfigAsync()
    {
        await base.ReadConfigAsync();
        await Messages.SetMessageSetAsync(new LzMessageSet("en-US", LzMessageUnits.Imperial));
    }
}
