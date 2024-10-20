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
#if UNITY_IOS && !UNITY_EDITOR
    const string RedirectUri = "co.projectquik.spherekit:/oauth";
#else
    const string RedirectUri = "http://localhost:8080/spherekit/oauth";
#endif

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

            _clientId = config.clientID;
            _serverUrl = config.serverURL;

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

            // Start OAuth2 flow

            var authConfig = new AuthorizationCodeFlowWithPkce.Configuration()
            {
                clientId = _clientId,
                scope = "profile project",
                redirectUri = RedirectUri,
            };
            var auth = new SphereAuth(authConfig, _serverUrl);
            var crossPlatformBrowser = new CrossPlatformBrowser();
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.LinuxPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.LinuxEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.Android, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new StandaloneBrowser());
            var authenticationSession = new AuthenticationSession(auth, crossPlatformBrowser);
            var accessTokenResponse = await authenticationSession.AuthenticateAsync();

            Debug.Log("Access token: " + accessTokenResponse.accessToken);
        }

        public static void SignOut()
        {
            CheckInitialized();

            // TODO: Sign out of Sphere Kit
        }
    }
}
