﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace LazyStack.Auth;

public class LoginFormat : ILoginFormat
{

    /// <summary>
    /// Creates an enumeration with input requirements.
    /// </summary>
    /// <param name="login"></param>
    /// <returns></returns>
    public IEnumerable<string> CheckLoginFormat(string login)
    {
        if (login.Length < 8)
            yield return "AuthFormatMessages_Login01";
    }
}
