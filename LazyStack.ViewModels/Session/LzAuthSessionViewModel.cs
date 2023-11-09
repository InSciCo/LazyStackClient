namespace LazyStack.ViewModels;

/// <summary>
/// Orchastrates the connection to services.
/// Decouples IAuthProcess from ILzClientConfig.
/// </summary>
public class LzAuthSessionViewModel : LzBaseSessionViewModel, ILzAuthSessionViewModel
{
    public LzAuthSessionViewModel(
        IOSAccess osAccess,
        IAuthProcess authProcess, 
        ILzClientConfig clientConfig, 
        IInternetConnectivitySvc internetConnectivity
        ) : base(osAccess, clientConfig, internetConnectivity)    
    {
        AuthProcess = authProcess ?? throw new ArgumentNullException(nameof(authProcess));    

        // ReactiveUI statements. I've added some comments for folks who have 
        // never used ReactiveUI
        // When Authprocess.IsSignedIn changes, update this classes observable IsSignedIn property
        this.WhenAnyValue(x => x.AuthProcess.IsSignedIn)
            .ToPropertyEx(this, x => x.IsSignedIn);

        // When Authprocess.IsBusy changes, update this classes observable IsBusy property
        this.WhenAnyValue(x => x.AuthProcess.IsBusy)
            .ToProperty(this, x => x.IsBusy);

        // When Authprocess.HasChallenge changes, update this classes observable HasChallenge property
        this.WhenAnyValue(x => x.AuthProcess.HasChallenge)
            .ToPropertyEx(this, x => x.HasChallenge);

        // When the login changes, update the IsAdmin property. Note the virtual method IsAdminCheck().
        // Implement your application specific logic in the IsAdminCheck() method.  
        this.WhenAnyValue(x => x.AuthProcess.Login)
            .SelectMany(async x => await IsAdminCheck()) // SelectMany allows the use of async in the reactive chain
            .ToPropertyEx(this, x => x.IsAdmin);
    }
    public IAuthProcess AuthProcess { get; set; }

    // The ObservableAsProperty annotation is defined in ReactiveUI.Fody
    public async virtual Task<bool> IsAdminCheck()
    {
        await Task.Delay(0);
        // This is not recommended. Override this method in your app 
        // to do an appropriate check.
        return AuthProcess.Login == "Administrator";
    }
}
