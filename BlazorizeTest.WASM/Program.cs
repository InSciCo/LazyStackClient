using Blazorise;
using Blazorise.Bootstrap5;
using System.Diagnostics;

namespace BlazorizeTest.WASM
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // We use the launchSettings.json profile ASPNETCORE_ENVIRONMENT environment variable
            // to determine the host addresses for the API host and Tenancy host.
            //
            // Examples:
            // Production: "ASPNETCORE_ENVIRONMENT": "Production" 
            //  The API and Tenancy host are the same and are the base address of the cloudfront distribution
            //  the app is loaded from.
            //
            // Debug against LocalHost API:
            //  "ASPNETCORE_ENVIRONMENT": "https://localhost:5001,https://admin.lazymagicdev.click"
            //  The first url is the API host, the second is the tenancy host.
            //
            // Debug against CloundFront deployment:
            //   "ASPNETCORE_ENVIRONMENT": "https://admin.lazymagicdev.click"
            //   The url is the API host and the tenancy host.


            var hostEnvironment = builder.HostEnvironment;
            var apiUrl = string.Empty;
            var assetsUrl = string.Empty;
            var isLocal = false; // Is the code being served from a local development host?
            switch (hostEnvironment.Environment)
            {
                case "Production":
                    Console.WriteLine("Production environment");
                    apiUrl = assetsUrl = builder.HostEnvironment.BaseAddress;
                    break;
                default:
                    Console.WriteLine("Development environment");
                    var envVar = hostEnvironment.Environment;
                    // The variable is a list of urls, separated by a comma.
                    var urls = envVar.Split(',');
                    apiUrl = urls[0];
                    apiUrl = apiUrl.EndsWith('/') ? apiUrl : apiUrl + '/';
                    assetsUrl = urls.Length > 1 ? urls[1] : urls[0];
                    assetsUrl = assetsUrl.EndsWith('/') ? assetsUrl : assetsUrl + '/';
                    Console.WriteLine($"apiUrl: {apiUrl}");
                    Console.WriteLine($"assetsUrl: {assetsUrl}");
                    isLocal = true;
                    break;
            }


            builder.Services
            .AddSingleton<ILzMessages, LzMessages>()
            .AddSingleton<ILzClientConfig, LzClientConfig>()
            .AddSingleton(sp => new HttpClient { BaseAddress = new Uri(apiUrl) })
            .AddSingleton<BlazorInternetConnectivity>()
            .AddSingleton<IBlazorInternetConnectivity>(sp => sp.GetRequiredService<BlazorInternetConnectivity>())
            .AddSingleton<IInternetConnectivitySvc>(sp => sp.GetRequiredService<BlazorInternetConnectivity>())
            .AddSingleton<ILzHost>(sp => new LzHost(
                url: apiUrl,  // api url
                assetsUrl: assetsUrl, // tenancy assets url
                isMAUI: false, // sets isWASM to true
                isAndroid: false,
                isLocal: isLocal))
            .AddSingleton<IOSAccess, BlazorOSAccess>()
            .AddLazyStackAuthCognito()
            .AddSingleton<ISessionsViewModel, SessionsViewModel>()
            .AddBlazorise(options => { options.Immediate = true; })
            .AddBootstrap5Providers();
            //.AddFontAwesomeIcons();
                
            RegisterFactories.Register(builder.Services);

            await builder.Build().RunAsync();
        }
    }
}
