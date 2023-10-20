
namespace LazyStack.Auth;

public interface ICodeFormat
{
    IEnumerable<string> CheckCodeFormat(string code);
}
