using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LazyStack.Client.Base;

namespace LazyStack.Blazor;

public static class ConfigureLazyStackBlazorMessages
{

    public static ILzMessages AddLazyStackBlazorMessages(this ILzMessages lzMessages)
    {
        List<string> messages = [
            ];
        lzMessages.MessageFiles.AddRange(messages);
        return lzMessages;
    }
}
