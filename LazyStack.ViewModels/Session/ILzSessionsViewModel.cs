namespace LazyStack.ViewModels;
public interface ILzSessionsViewModel
{
    IDictionary<string, string> SessionLogins { get; }
    Task CreateAsync();
    Task DeleteAsync(string sessionId);
    Task SetAsync(string sessionId);
}
