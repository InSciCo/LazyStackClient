namespace LazyStack.ViewModels;

/// <summary>
/// Orchestrates the connection to services.
/// </summary>
public abstract class LzSessionViewModelAuthNotifications : LzSessionViewModelAuth, ILzSessionViewModelAuthNotifications
{
    public LzSessionViewModelAuthNotifications(
        IAuthProcess authProcess,
        IOSAccess oSAccess,
        ILzClientConfig clientConfig, 
        IInternetConnectivitySvc internetConnectivity,
    	ILzMessages messages
		) : base(authProcess, oSAccess, clientConfig, internetConnectivity, messages)
	{
    }

    public ILzNotificationSvc? NotificationsSvc { get; set; }
}
