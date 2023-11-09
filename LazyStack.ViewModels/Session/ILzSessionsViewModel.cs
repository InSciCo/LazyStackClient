namespace LazyStack.ViewModels;
public interface ILzSessionsViewModel
{
    IDictionary<string, string> SessionLogins { get; }
    Task CreateSessionAsync();
    Task DeleteAsync(string sessionId);
    Task SetAsync(string sessionId);
}
