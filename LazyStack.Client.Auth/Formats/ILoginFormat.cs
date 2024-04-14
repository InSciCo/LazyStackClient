namespace LazyStack.Client.Auth;

public interface ILoginFormat
{
    public IEnumerable<string> CheckLoginFormat(string password);
}
