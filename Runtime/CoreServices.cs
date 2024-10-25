using UnityEngine;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Cdm.Authentication.Browser;

namespace SphereKit
{
    public class CoreServices
    {
        static public bool HasInitialized { get; private set; } = false;
        static string _clientId;
        static string _serverUrl;
        static string _deepLinkScheme;
        static string _redirectUri;
        static AuthenticationSession _authenticationSession;
        static AccessTokenResponse _accessTokenResponse;

        public static void Initialize()
        {
            var config = ProjectConfig.GetOrCreateConfig();
            if (config == null || config.clientID == "")
            {
                throw new System.Exception("The Client ID has not been configured yet. Please configure Sphere Kit in Project Settings.");
            }
            if (config.serverURL == "")
            {
                throw new System.Exception("The Server URL has not been configured yet. Please configure Sphere Kit in Project Settings.");
            }
            if (config.deepLinkScheme == "")
            {
#if UNITY_EDITOR
                Debug.LogWarning("The Deep Link Scheme has not been configured in Project Settings yet. Authentication will not work on iOS and Android.");
#endif
            }

            _clientId = config.clientID;
            _serverUrl = config.serverURL;
            _deepLinkScheme = config.deepLinkScheme;

#if UNITY_IOS || UNITY_ANDROID
    _redirectUri = _deepLinkScheme + "://oauth";
#else
    _redirectUri = "http://localhost:8080/spherekit/oauth";
#endif

            HasInitialized = true;
            Debug.Log($"Sphere Kit has been initialized. Client ID is {_clientId}");
        }

        static void CheckInitialized()
        {
            if (!HasInitialized)
            {
                throw new System.Exception("Sphere Kit has not been initialized. Please call SphereKit.Core.Initialize() before calling any other methods.");
            }
        }

        public static async void SignInWithSphere()
        {
            CheckInitialized();

            // Check platform specific requirements
#if UNITY_IOS || UNITY_ANDROID
            if (string.IsNullOrEmpty(_deepLinkScheme))
            {
                throw new System.Exception("The Deep Link Scheme has not been configured in Project Settings yet. Authentication will not work on iOS and Android.");
            }
#endif

            // Start OAuth2 flow
            var authConfig = new AuthorizationCodeFlowWithPkce.Configuration()
            {
                clientId = _clientId,
                scope = "profile project",
                redirectUri = _redirectUri,
            };
            var auth = new SphereAuth(authConfig, _serverUrl);
            var crossPlatformBrowser = new CrossPlatformBrowser();
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.LinuxPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.LinuxEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.Android, new DeepLinkBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new StandaloneBrowser());
            _authenticationSession = new AuthenticationSession(auth, crossPlatformBrowser);
            _accessTokenResponse = await _authenticationSession.AuthenticateAsync();

            Debug.Log("Access token: " + _accessTokenResponse.accessToken);
        }

        public static void SignOut()
        {
            CheckInitialized();

            // TODO: Sign out of Sphere Kit
        }
    }
}
