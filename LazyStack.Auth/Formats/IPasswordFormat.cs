namespace LazyStack.Auth;

public interface IPasswordFormat
{
    IEnumerable<string> CheckPasswordFormat(string password);
}