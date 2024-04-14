
namespace LazyStack.Client.Auth;

public interface ICodeFormat
{
    IEnumerable<string> CheckCodeFormat(string code);
}
