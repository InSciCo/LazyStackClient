using System;
using System.Collections.Generic;
using System.Text;

namespace LazyStack.Auth;

public class Creds
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
