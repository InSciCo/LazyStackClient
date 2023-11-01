namespace LazyStack.ViewModels;

public interface ILzAuthSessionViewModel : ILzBaseSessionViewModel
{
    IAuthProcess AuthProcess { get; set; }
    ILzNotificationSvc? NotificationsSvc { get; set; }
    Task<bool> IsAdminCheck();
}