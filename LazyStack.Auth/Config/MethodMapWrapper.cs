﻿

namespace LazyStack.Auth;

public interface IMethodMapWrapper
{
    Dictionary<string, string> MethodMap { get; }
}

public class MethodMapWrapper : IMethodMapWrapper
{
    public Dictionary<string, string> MethodMap { get; init; } = new();
}
