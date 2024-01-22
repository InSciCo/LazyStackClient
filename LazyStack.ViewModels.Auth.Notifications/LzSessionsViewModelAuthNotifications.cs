

namespace LazyStack.ViewModels;

public class LzSessionsViewModelAuthNotifications<T> : LzSessionsViewModelAuth<T>, ILzSessionsViewModelAuthNotifications<T>
    where T : ILzSessionViewModelAuthNotifications
{
    public LzSessionsViewModelAuthNotifications(
                      ILzMessages messages
                      ) : base(messages)
    {
    }
}
