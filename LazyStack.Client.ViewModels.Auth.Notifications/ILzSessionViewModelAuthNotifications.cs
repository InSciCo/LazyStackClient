using System.ComponentModel;

namespace LazyStack.Client.ViewModels;

public interface ILzSessionViewModelAuthNotifications : ILzSessionViewModelAuth, INotifyPropertyChanged
{
    ILzNotificationSvc? NotificationsSvc { get; set; }
}