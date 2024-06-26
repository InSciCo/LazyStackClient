using static System.Formats.Asn1.AsnWriter;

namespace BlazorizeTest.ViewModels;

[Factory]
public class SessionViewModel : LzSessionViewModelAuth, ISessionViewModel, ILzTransient
{
    public SessionViewModel(
        IOSAccess osAccess, // singleton
        ILzClientConfig clientConfig, // singleton
        IInternetConnectivitySvc internetConnectivity, // singleton
        [FactoryInject] ILzMessages messages, // singleton
        [FactoryInject] IAuthProcess authProcess, // transient
        [FactoryInject] ILzHost lzHost // singleton
        )
        : base(authProcess, osAccess, clientConfig, internetConnectivity, messages)
    {
        authProcess.SetAuthenticator(clientConfig.AuthConfig);

    }


}
