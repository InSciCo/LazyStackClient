namespace LazyStack.ViewModels;

public static class ConfigureLazyStackViewModels
{
    public static IServiceCollection AddLazyStackViewModels(this IServiceCollection services)
    {
        services.TryAddSingleton<DevConnectViewModel>();
        return services;
    }
    public static ILzMessages AddLazyStackViewModels(this ILzMessages messages)
    {
        var assembly = MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly;
        var assemblyName = assembly!.GetName().Name;
        
        // Add/Overwrite messages with messages in this library's LzMessages.json
        using var messagesStream = assembly?.GetManifestResourceStream($"{assemblyName}.Config.Messages.json")!;
        if (messagesStream != null)
        {
            using var messagesReader = new StreamReader(messagesStream);
            var messagesText = messagesReader.ReadToEnd();
            messages.MergeJson(messagesText);
        }
        return messages;
    }
}
