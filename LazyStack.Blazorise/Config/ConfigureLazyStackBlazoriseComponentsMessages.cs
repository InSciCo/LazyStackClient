using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazyStack.BlazoriseComponents;
public static class ConfigureLazyStackBlazoriseComponentsMessages
{
    public static ILzMessages AddLazyStackBlazoriseComponentsMessages(this ILzMessages lzMessages)
    {
        List<string> messages = [
            ];
        lzMessages.MessageFiles.AddRange(messages);
        return lzMessages;
    }
}