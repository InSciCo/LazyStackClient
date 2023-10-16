using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace LazyStack.Auth;

// This interface surfaces only those HttpClient members actually
// required by the generated code.
public interface ILzHttpClient : IDisposable
{
    // Note: CallerMember is inserted as a literal by the compiler in the IL so 
    // there is no performance penalty for using it.
    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage requestMessage,
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken,
        [CallerMemberName] string? callerMemberName = null);
    public bool IsServiceAvailable { get; }

}
