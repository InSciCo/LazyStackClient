namespace LazyStack.ViewModels;

public class LzSessionsViewModelAuth<T> : LzSessionsViewModel<T>, ILzSessionsViewModelAuth<T>
    where T : ILzSessionViewModelAuth
{
    public LzSessionsViewModelAuth(
               ILzMessages messages
               ) : base(messages)   
    {
        this.WhenAnyValue(x => x.InternetConnectivity!.IsOnline)
            .Select(x => { Console.WriteLine("IsOnline:" + x); return x; })
            .ToPropertyEx(this, x => x.IsOnline);
    }
    public override T? SessionViewModel { get; set; }
    public IAuthProcess? AuthProcess { get; set; }
    [ObservableAsProperty] public bool IsSignedIn { get; }
    [ObservableAsProperty] public bool IsAdmin { get; }
    public override async Task CreateSessionAsync()
    {
        await base.CreateSessionAsync();
        AuthProcess = SessionViewModel!.AuthProcess;
        this.WhenAnyValue(x => x.AuthProcess!.IsSignedIn)
            .ToPropertyEx(this, x => x.IsSignedIn)
            .DisposeWith(sessionDisposables);

        this.WhenAnyValue(x => x.SessionViewModel!.IsAdmin)
            .ToPropertyEx(this, x => x.IsAdmin)
            .DisposeWith(sessionDisposables);
    }
}
