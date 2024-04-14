using System.Diagnostics;

namespace BlazorTest.WASM
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            ILzMessages messages = new LzMessages();
            messages
                .AddLazyStackComponents()
                .AddLazyStackViewModels()
                .AddLazyStackAuth()
                .ReplaceVars();

            // Set the baseaddress. 
            string baseaddress = (Debugger.IsAttached)
                ? baseaddress = "https://localhost:5001"
                : builder.HostEnvironment.BaseAddress;

            builder.Services
                .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                .AddSingleton<ILzHost>(sp => new LzHost(baseaddress, isMAUI: false, isAndroid: false))
                .AddSingleton(messages)
                .AddLazyStackComponents()
                .AddLazyStackAuthCognito()
                .AddSingleton<ISessionsViewModel, SessionsViewModel>();
                
            RegisterFactories.Register(builder.Services);
            

            await builder.Build().RunAsync();
        }
    }
}
