namespace LazyStack.Auth;

public interface IEmailFormat
{
    IEnumerable<string> CheckEmailFormat(string email);
}