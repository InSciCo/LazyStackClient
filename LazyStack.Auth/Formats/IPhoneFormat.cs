
namespace LazyStack.Auth;

public interface IPhoneFormat
{
    IEnumerable<string> CheckPhoneFormat(string phone);
}