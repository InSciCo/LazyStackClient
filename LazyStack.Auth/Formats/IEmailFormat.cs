using System.Collections.Generic;

namespace LazyStack.Auth;

public interface IEmailFormat
{
    IEnumerable<string> CheckEmailFormat(string email);
}