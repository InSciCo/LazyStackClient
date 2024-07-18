using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazyStack.BlazoriseComponents;

public static class ConfigureLazyStackBlazoriseComponentsAuthMessages
{
    public static ILzMessages AddLazyStackBlazoriseComponentsAuthMessages(this ILzMessages lzMessages)
    {
        List<string> messages = [
            ];
        lzMessages.MessageFiles.AddRange(messages);
        return lzMessages;
    }
}
