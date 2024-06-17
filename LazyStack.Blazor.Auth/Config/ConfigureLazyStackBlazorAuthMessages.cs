using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LazyStack.Client.Base;


namespace LazyStack.Blazor;

public static class ConfigureLazyStackBlazorAuthMessages
{

    public static ILzMessages AddLazyStackBlazorMessages(this ILzMessages lzMessages)
    {
        List<string> messages = [
            "_content/LazyStack.Blazor.Auth/Messages.json"
            ];
        lzMessages.MessageFiles.AddRange(messages);
        return lzMessages;
    }
}