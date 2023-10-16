using System.Collections.Generic;

namespace LazyStack.Auth;

public interface IPhoneFormat
{
    IEnumerable<string> CheckPhoneFormat(string phone);
}