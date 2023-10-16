using System.Collections.Generic;

namespace LazyStack.Auth;

public interface IPasswordFormat
{
    IEnumerable<string> CheckPasswordFormat(string password);
}