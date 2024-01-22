namespace LazyStack.Auth;

/// <summary>
/// This ILzHttpClient supports calling Local, CloudFront or ApiGateway endpoints.
/// It is not an HttClient, instead it services SendAsync() calls made from the *SvcClientSDK and 
/// dispatches these calls to cached HttpClient(s) configured for each API. This allows each 
/// endpoint to be separately configured for security etc.
/// </summary>
public class LzHttpClient : NotifyBase, ILzHttpClient
{
    public LzHttpClient(
        ILzClientConfig clientConfig, // service connection info
        IMethodMapWrapper methodMap, // map of methods to api endpoints
        IAuthProvider authProvider, // Auth service. ex: AuthProviderCognito
        ILzHost lzHost // Runtime environment. IsMAUI, IsWASM, BaseURL etc.
        )
    {
        this.clientConfig = clientConfig; 
        this.methodMap = methodMap; // map of methods to api endpoints
        this.authProvider = authProvider;
        this.lzHost= lzHost;
    }
    protected ILzClientConfig clientConfig;
    protected LzRunConfig runConfig => clientConfig.RunConfig;
    protected LzService service => clientConfig.Services[runConfig.Service];
    protected JObject authenticator => clientConfig.Authenticators[service.Auth];

    protected IMethodMapWrapper methodMap;
    protected IAuthProvider authProvider;
    protected ILzHost lzHost;
    protected Dictionary<string, HttpClient> httpClients = new();
    protected bool isServiceAvailable = false;
    public bool IsServiceAvailable
    {
        get { return isServiceAvailable; }  
        set
        {
            SetProperty(ref isServiceAvailable, value);
        }
    }
    protected int[] serviceUnavailableCodes = new int[] { 400 };

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage requestMessage,
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken,
        [CallerMemberName] string? callerMemberName = null!)
    {
        // Lookup callerMemberName in methodMap to get api name, if not found then throw
        if (!methodMap.MethodMap.TryGetValue(callerMemberName!, out string? api))
            throw new Exception($"{nameof(LzHttpClient)}.{nameof(SendAsync)} failed. callerMemberName {callerMemberName} not found in MethodMap");

        // Find api endpoint data
        JObject resource = service.Resources[api];
        if (resource == null)
            throw new Exception($"{nameof(LzHttpClient)}.{nameof(SendAsync)} failed. Apis {api} not found in ClientConfig.");

        var securityLevel = (int)resource["SecurityLevel"]!;
        var resourceType = (string)resource["ResourceType"]!;
        var isLocal = resourceType == "Local" || resourceType == "LocalAndroid";
        var baseUrl = (string)resource["Url"]!;
        if(!baseUrl.EndsWith("/"))
            baseUrl += "/"; // baseUrl must end with a / or contcat with relative path may fail

        // Create new HttpClient for endpoint if one doesn't exist
        if (!httpClients.TryGetValue(baseUrl, out HttpClient? httpclient))
        {
            httpclient = isLocal && lzHost.IsMAUI
                ? new HttpClient(GetInsecureHandler())
                : new HttpClient();
            httpclient.BaseAddress = new Uri(baseUrl);
            httpClients.Add(baseUrl, httpclient);
        }

        try
        {
            HttpResponseMessage? response = null;
            switch (securityLevel)
            {
                case 0: // No security 
                    try
                    {
                        response = await httpclient.SendAsync(
                            requestMessage,
                            httpCompletionOption,
                            cancellationToken);
                        IsServiceAvailable = true;
                        return response;
                    }
                    catch (HttpRequestException e) 
                    {
                        // request failed due to an underlying issue such as network connectivity,
                        // DNS failure, server certificate validation or timeout
                        isServiceAvailable = false;
                        Console.WriteLine($"HttpRequestException {e.Message}");
                        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                    } 
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error: {callerMemberName} {e.Message}");
                        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                    }

                case 1: // Use JWT Token signing process
                    try
                    {
                        string? token = "";
                        try
                        {
                            token = await authProvider!.GetJWTAsync();
                            requestMessage.Headers.Add("Authorization", token);
                        }
                        catch
                        {
                            // Ignore. We ignore this error and let the 
                            // api handle the missing token. This gives us a 
                            // way of testing an improperly configured API.
                            Debug.WriteLine("authProvider.GetJWTAsync() failed");
                            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                        }

                        response = await httpclient.SendAsync(
                            requestMessage,
                            httpCompletionOption,
                            cancellationToken);
                        //Console.WriteLine(callerMemberName);
                        IsServiceAvailable = true;  
                        return response;
                    }
                    catch (HttpRequestException e)
                    {
                        // request failed due to an underlying issue such as network connectivity,
                        // DNS failure, server certificate validation or timeout
                        Console.WriteLine($"HttpRequestException {e.Message}");
                        isServiceAvailable = false;
                        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error: {callerMemberName} {e.Message}");
                        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                    }
                case 2: // Use AWS Signature V4 signing process
                    try
                    {
                        return await SendV4SigAsync(httpclient, requestMessage, httpCompletionOption, cancellationToken, callerMemberName);
                    }
                    catch (System.Exception e)
                    {
                        Debug.WriteLine($"Error: {e.Message}");
                    }
                    break;
                    throw new Exception($"Security Level {securityLevel} not supported.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }
        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
    }
    // This method is virtual so that it can be overridden by the LzHttpClientSigV4 class. This allows us 
    // to avoid dragging in the SigV4 package (and associated crypto libs) if we don't need it.
    public virtual async Task<HttpResponseMessage> SendV4SigAsync(
        HttpClient httpclient,
        HttpRequestMessage requestMessage,
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken,
        string callerMemberName)
    {
        await Task.Delay(0);
        throw new NotImplementedException("AWS Signature V4 signing requires the LzHttpClientSigV4 class. Use the LazyStack.Auth.Cognito.SigV4 package.");
    }
    public void Dispose()
    {
        foreach (var httpclient in httpClients.Values)
            httpclient.Dispose();
    }

    //https://docs.microsoft.com/en-us/xamarin/cross-platform/deploy-test/connect-to-local-web-services
    //Attempting to invoke a local secure web service from an application running in the iOS simulator 
    //or Android emulator will result in a HttpRequestException being thrown, even when using the managed 
    //network stack on each platform.This is because the local HTTPS development certificate is self-signed, 
    //and self-signed certificates aren't trusted by iOS or Android.
    public static HttpClientHandler GetInsecureHandler()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert!.Issuer.Equals("CN=localhost"))
                    return true;
                return errors == System.Net.Security.SslPolicyErrors.None;
            }
        };
        return handler;
    }

}
