namespace LazyStack.Auth;

public static class ConfigureLazyStackAuthCognito
{
    public static IServiceCollection AddLazyStackAuthCognito(this IServiceCollection services)
    {
        // TryAdd only succeeds if the service is not already registered
        // It is used here to allow the calling programs to register their own
        // implementations of these classes.
        // Note: LzHost must be registered in the WASM Program.cs file so the current 
        // base url can be captured. MAUI programs are not loaded from a URL so they 
        // read their API params from a configuration file specific to the client build,
        // see the RunConfig class.
        services.TryAddTransient<ILzHttpClient, LzHttpClient>();
        services.TryAddTransient<IAuthProvider, AuthProviderCognito>();
        services.AddLazyStackAuth();
        return services;
    }

    public static ILzMessages AddLazyStackAuthCognito(this ILzMessages messages)
    {

        messages.AddLazyStackAuth();

        var assembly = MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly;
        var assemblyName = assembly!.GetName().Name;
        using var messagesStream = assembly.GetManifestResourceStream($"{assemblyName}.Config.Messages.json")!;
        // Add/Overwrite messages with messages in this library's LzMessages.json
        if (messagesStream != null)
        {
            using var messagesReader = new StreamReader(messagesStream);
            var messagesText = messagesReader.ReadToEnd();
            messages.MergeJson(messagesText);
        }

        return messages;
    }
}
