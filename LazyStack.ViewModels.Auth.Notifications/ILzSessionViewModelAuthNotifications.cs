using System.ComponentModel;

namespace LazyStack.ViewModels;

public interface ILzSessionViewModelAuthNotifications : ILzSessionViewModelAuth, INotifyPropertyChanged
{
    ILzNotificationSvc? NotificationsSvc { get; set; }
}