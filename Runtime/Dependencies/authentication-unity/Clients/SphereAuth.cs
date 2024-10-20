using Cdm.Authentication.OAuth2;

namespace Cdm.Authentication.Clients
{
    public class SphereAuth : AuthorizationCodeFlowWithPkce
    {
        public const string AuthorizationPath = "/oauth/authorize";
        public const string TokenPath = "/oauth/token";
        
        public override string authorizationUrl => $"{serverUrl}{AuthorizationPath}";
        public override string accessTokenUrl => $"{serverUrl}{TokenPath}";
        
        public string serverUrl { get; }
        
        public SphereAuth(Configuration configuration, string serverUrl) : base(configuration)
        {
            this.serverUrl = serverUrl;
        }
    }
}