namespace LazyStack.Client.Auth;

public interface IPasswordFormat
{
    IEnumerable<string> CheckPasswordFormat(string password);
}