using System.Collections.Generic;

namespace LazyStack.Auth;

public interface ICodeFormat
{
    IEnumerable<string> CheckCodeFormat(string code);
}
