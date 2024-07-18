using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LazyStack.Client.Base;

namespace LazyStack.MudBlazorComponents.Auth;

public static class ConfigureLazyStackMudBlazorComponentsMessages
{
public static ILzMessages AddLazyStackMudBlazorComponentsAuthMessages(this ILzMessages lzMessages)
    {
        List<string> messages = [
            ];
        lzMessages.MessageFiles.AddRange(messages);
        return lzMessages;
    }
}
