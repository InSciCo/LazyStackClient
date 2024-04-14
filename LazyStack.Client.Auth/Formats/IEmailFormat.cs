namespace LazyStack.Client.Auth;

public interface IEmailFormat
{
    IEnumerable<string> CheckEmailFormat(string email);
}