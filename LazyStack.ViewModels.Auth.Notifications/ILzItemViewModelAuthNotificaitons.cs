﻿
namespace LazyStack.ViewModels;

public interface ILzItemViewModelNotificationsBase<TModel> : ILzItemViewModel<TModel>
{
    public TModel? NotificationData { get; set; }
    public ILzNotificationSvc? NotificationsSvc { get; set; }
    public INotificationEditOption NotificationEditOption { get; set; }
    public long LastNotificationTick { get; set; }
    public bool NotificationReceived { get; set; }
    public bool IsMerge { get; set; }
    public abstract Task UpdateFromNotification(string payloadData, string payloadAction, long payloadCreatedAt);
   
}
