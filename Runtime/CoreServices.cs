using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SphereKit
{
    public class CoreServices
    {
        static public bool HasInitialized { get; private set; } = false;
        static public Player Player { get; private set; }
        static internal string AccessToken { get { return _accessTokenResponse?.accessToken; } }

        static string _clientId;
        static string _serverUrl;
        static string _deepLinkScheme;
        static string _redirectUri;
        static AuthenticationSession _authenticationSession;
        static AccessTokenResponse _accessTokenResponse;
        static Timer _refreshAccessTokenTimer;
        static readonly HttpClient _client = new();

        private const string accessTokenResponseKey = "accessTokenResponse";

        public static async Task Initialize()
        {
            // Load configuration
            var config = ProjectConfig.GetOrCreateConfig();
            if (config == null || config.clientID == "")
            {
                throw new Exception("The Client ID has not been configured yet. Please configure Sphere Kit in Project Settings.");
            }
            if (config.serverURL == "")
            {
                throw new Exception("The Server URL has not been configured yet. Please configure Sphere Kit in Project Settings.");
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

            // Initialise authentication session
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

            // Load access token response from player prefs
            try
            {
                if (PlayerPrefs.HasKey(accessTokenResponseKey))
                {
                    _accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(PlayerPrefs.GetString(accessTokenResponseKey));
                    _authenticationSession.SetAuthenticationInfo(_accessTokenResponse);
                    Debug.Log("Access token loaded from player prefs.");
                    try
                    {
                        await InternalGetPlayerInfo();
                    } catch (AuthenticationException e)
                    {
                        if (!_accessTokenResponse.IsExpired())
                        {
                            throw e;
                        }
                    }
                    ScheduleAccessTokenRefresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to load access token from player prefs: " + e.Message);
            }

            HasInitialized = true;
            Debug.Log($"Sphere Kit has been initialized. Client ID is {_clientId}");
        }

        static void CheckInitialized()
        {
            if (!HasInitialized)
            {
                throw new Exception("Sphere Kit has not been initialized. Please call SphereKit.Core.Initialize() before calling any other methods.");
            }
        }

        public static async Task SignInWithSphere()
        {
            CheckInitialized();

            // Check platform specific requirements
#if UNITY_IOS || UNITY_ANDROID
            if (string.IsNullOrEmpty(_deepLinkScheme))
            {
                throw new Exception("The Deep Link Scheme has not been configured in Project Settings yet. Authentication will not work on iOS and Android.");
            }
#endif

            // Start OAuth2 flow
            _accessTokenResponse = await _authenticationSession.AuthenticateAsync();
            await GetPlayerInfo();
            StoreAccessTokenResponse();

            Debug.Log("Access token received from server.");
            ScheduleAccessTokenRefresh();
        }

        static async Task InternalGetPlayerInfo()
        {
            if (AccessToken == null)
            {
                throw new Exception("User info requested but access token is not available");
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_serverUrl}/auth/players/{_accessTokenResponse.user.uid}");
            requestMessage.Headers.Add("Authorization", $"Bearer {_accessTokenResponse.accessToken}");
            var playerResponse = await _client.SendAsync(requestMessage);
            if (playerResponse.IsSuccessStatusCode)
            {
                Player = JsonConvert.DeserializeObject<Player>(await playerResponse.Content.ReadAsStringAsync());
                Debug.Log("Player info retrieved for " + Player.UserName);
            } else
            {
                await HandleErrorResponse(playerResponse);
            }
        }

        public static async Task GetPlayerInfo()
        {
            CheckInitialized();

            await InternalGetPlayerInfo();
        }

        static async Task RefreshAccessToken()
        {
            if (_accessTokenResponse == null)
            {
                Debug.LogWarning("Access token refresh requested but no access token is available.");
                return;
            }

            if ((_accessTokenResponse.refreshTokenExpiresAt - DateTime.UtcNow).Value.TotalSeconds <= 120)
            {
                Debug.LogWarning("Refresh token has expired. User needs to sign in again.");
                // TODO: Sign in again
                return;
            }

            try
            {
                _accessTokenResponse = await _authenticationSession.RefreshTokenAsync();
                await GetPlayerInfo();
                StoreAccessTokenResponse();

                Debug.Log("Access token refreshed.");

                ScheduleAccessTokenRefresh();
            } catch (AuthenticationException)
            {
                Debug.LogWarning("Failed to refresh token. User needs to sign in again");
            }
        }

        static void ScheduleAccessTokenRefresh()
        {
            if (_accessTokenResponse == null)
            {
                Debug.LogWarning("Access token refresh scheduling requested but no access token is available.");
                return;
            }

            var liveExpiresIn = Math.Max(Convert.ToInt64((_accessTokenResponse.expiresAt.Value - DateTime.UtcNow).TotalMilliseconds) - 120 * 1000, 0);

            // Schedule a refresh of the access token
            Debug.Log($"Scheduling access token refresh in {liveExpiresIn}ms");
            _refreshAccessTokenTimer?.Dispose();
            _refreshAccessTokenTimer = new Timer(static async (state) =>
            {
                Debug.Log("Refreshing access token");
                await RefreshAccessToken();
            });
            _refreshAccessTokenTimer.Change(liveExpiresIn, Timeout.Infinite);
        }

        static void StoreAccessTokenResponse()
        {
            if (_accessTokenResponse == null)
            {
                Debug.LogWarning("Store access token requested but no access token is available.");
                return;
            }

            var accessTokenResponseJson = JsonConvert.SerializeObject(_accessTokenResponse);
            MainThreadDispatcher.Execute(() =>
            {
                PlayerPrefs.SetString(accessTokenResponseKey, accessTokenResponseJson);
                Debug.Log("Stored access token in player prefs");
            });
        }

        internal static async Task HandleErrorResponse(HttpResponseMessage response)
        {
            Exception error = null;
            try
            {
                var errorData = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                switch (response.StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        error = new InternalServerException(errorData.ErrorMessage);
                        break;
                    case HttpStatusCode.TooManyRequests:
                        error = new RateLimitException(errorData.ErrorMessage);
                        break;
                    case HttpStatusCode.NotFound:
                        error = new NotFoundException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                    case HttpStatusCode.Forbidden:
                        error = new ForbiddenException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = new AuthenticationException(errorData.ErrorCode, errorData.ErrorMessage);
                        // TODO: Sign in again
                        break;
                    case HttpStatusCode.BadRequest:
                        error = new BadRequestException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            error ??= new Exception("An unknown error occurred while fetching player info.");

            throw error;
        }

        public static void SignOut()
        {
            CheckInitialized();

            // TODO: Sign out of Sphere Kit
            _refreshAccessTokenTimer?.Dispose();
        }
    }
}
