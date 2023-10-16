using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace LazyStack.Auth;

public interface ILoginFormat
{
    public IEnumerable<string> CheckLoginFormat(string password);
}
