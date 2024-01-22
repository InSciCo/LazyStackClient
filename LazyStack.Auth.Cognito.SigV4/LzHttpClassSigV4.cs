namespace LazyStack.Auth;
using LazyStack.Base;
using LazyStack.ClientSDK;  
using Amazon.Runtime;

public partial class LzHttpClientSigV4 : LzHttpClient, ILzHttpClient   
{
    public LzHttpClientSigV4(
        ILzClientConfig clientConfig, // service connection info
        IMethodMapWrapper methodMap, // map of methods to api endpoints
        IAuthProvider authProvider, // Auth service. ex: AuthProviderCognito
        ILzHost lzHost // Runtime environment. IsMAUI, IsWASM, BaseURL etc.
        ) : base(clientConfig, methodMap, authProvider, lzHost)
    {
    }
    public override async Task<HttpResponseMessage> SendV4SigAsync(
        HttpClient httpclient,
        HttpRequestMessage requestMessage,
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken,
        string callerMemberName) 
    {

        // Note: For this ApiGateway we need to add a copy of the JWT header 
        // to make it available to the Lambda. This type of ApiGateway will not
        // pass the authorization header through to the Lambda.
        var token = await authProvider.GetJWTAsync();
        requestMessage.Headers.Add("LzIdentity", token);

        var iCreds = await authProvider.GetCredsAsync();
        var awsCreds = new ImmutableCredentials(iCreds!.AccessKey, iCreds.SecretKey, iCreds.Token);

        // Note. Using named parameters to satisfy version >= 3.x.x  signature of 
        // AwsSignatureVersion4 SendAsync method.
        var response = await httpclient.SendAsync(
        request: requestMessage,
        completionOption: httpCompletionOption,
        cancellationToken: cancellationToken,
        regionName: clientConfig.Region,
        serviceName: "execute-api",
        credentials: awsCreds);
        return response;

    }

}
