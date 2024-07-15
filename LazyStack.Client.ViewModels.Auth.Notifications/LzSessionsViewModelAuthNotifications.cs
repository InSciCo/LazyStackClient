

namespace LazyStack.Client.ViewModels;

public abstract class LzSessionsViewModelAuthNotifications<T> : LzSessionsViewModelAuth<T>, ILzSessionsViewModelAuthNotifications<T>
    where T : ILzSessionViewModelAuthNotifications
{
    public LzSessionsViewModelAuthNotifications(
                      ) 
    {
    }
}
