using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SphereKit.Utils;
using System.Linq;

#nullable enable
namespace SphereKit
{
    public class CoreServices
    {
        static public bool HasInitialized { get; private set; } = false;
        static public Player? CurrentPlayer { get; private set; } = null;
        static internal string? AccessToken { get => _accessTokenResponse?.accessToken; }
        static internal string ServerUrl { get => _serverUrl; }

        static string _clientId = "";
        static string _serverUrl = "";
        static string _deepLinkScheme = "";
        static string _redirectUri = "";
        static AuthenticationSession? _authenticationSession;
        static AccessTokenResponse? _accessTokenResponse;
#pragma warning disable CS8629 // Nullable value type may be null.
        static long _accessTokenExpiringIn { get => _accessTokenResponse == null ? 0: Math.Max(Convert.ToInt64((_accessTokenResponse.expiresAt - DateTime.UtcNow).Value.TotalMilliseconds) - 120 * 1000, 0); }
#pragma warning restore CS8629 // Nullable value type may be null.
        static Timer? _refreshAccessTokenTimer;
        static readonly HttpClient _httpClient = new();
        static List<Action<AuthState>> _playerStateChangeListeners = new();
        static AuthState _authState { get { return new AuthState(_accessTokenResponse != null, CurrentPlayer); } }
        static string? _uid { get { return _accessTokenResponse?.user.uid; } }

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
            InitialiseAuthenticationSession();

            // Load access token response from player prefs
            if (PlayerPrefs.HasKey(accessTokenResponseKey))
            {
                try
                {
                    _accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(PlayerPrefs.GetString(accessTokenResponseKey));
                    _authenticationSession!.SetAuthenticationInfo(_accessTokenResponse);
                    Debug.Log("Access token loaded from player prefs.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to load access token from player prefs: " + e.Message);
                }

                if (_accessTokenResponse != null)
                {
                    if (_accessTokenExpiringIn <= 0)
                    {
                        await RefreshAccessToken();
                    } else
                    {
                        await InternalGetPlayerInfo(_uid!);
                    }
                }
            }
            else
            {
                NotifyPlayerStateChangeListeners();
            }

            HasInitialized = true;
            Debug.Log($"Sphere Kit has been initialized. Client ID is {_clientId}");
        }

        static void InitialiseAuthenticationSession()
        {
            var authConfig = new AuthorizationCodeFlowWithPkce.Configuration()
            {
                clientId = _clientId,
                scope = "profile project",
                redirectUri = _redirectUri,
            };
            var auth = new SphereAuth(authConfig, ServerUrl);
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
        }

        internal static void CheckInitialized()
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
            _accessTokenResponse = await _authenticationSession!.AuthenticateAsync();
            await GetPlayerInfo(_uid!);
            StoreAccessTokenResponse();

            Debug.Log("Access token received from server.");
            ScheduleAccessTokenRefresh();
        }

        internal static void CheckSignedIn()
        {
            if (_accessTokenResponse == null)
            {
                throw new Exception("User is not signed in."); // TODO: Use AuthenticationException
            }
        }

        static async Task<Player> InternalGetPlayerInfo(string uid)
        {
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/auth/players/{uid}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var playerResponse = await _httpClient.SendAsync(requestMessage);
            if (playerResponse.IsSuccessStatusCode)
            {
                var retrievedPlayer = JsonConvert.DeserializeObject<Player>(await playerResponse.Content.ReadAsStringAsync())!;
                if (uid == _uid)
                {
                    CurrentPlayer = retrievedPlayer;
                    NotifyPlayerStateChangeListeners();
                }
                Debug.Log("Player info retrieved for " + retrievedPlayer.UserName);

                return retrievedPlayer;
            }
            else
            {
                await HandleErrorResponse(playerResponse);
                return new Player();
            }
        }

        public static async Task<Player> GetPlayerInfo(string uid)
        {
            CheckInitialized();

            return await InternalGetPlayerInfo(uid);
        }

        static async Task RefreshAccessToken()
        {
            if (_accessTokenResponse == null)
            {
                Debug.LogWarning("Access token refresh requested but no access token is available.");
                return;
            }

# pragma warning disable CS8629 // Nullable value type may be null.
            if ((_accessTokenResponse.refreshTokenExpiresAt - DateTime.UtcNow).Value.TotalSeconds <= 120)
# pragma warning restore CS8629 // Nullable value type may be null.
            {
                Debug.LogWarning("Refresh token has expired. User needs to sign in again.");
                _accessTokenResponse = null;
                NotifyPlayerStateChangeListeners();
                return;
            }

            try
            {
                _accessTokenResponse = await _authenticationSession!.RefreshTokenAsync();
                await InternalGetPlayerInfo(_uid!);
                StoreAccessTokenResponse();

                Debug.Log("Access token refreshed.");

                ScheduleAccessTokenRefresh();
            }
            catch (AuthenticationException)
            {
                Debug.LogWarning("Failed to refresh token. User needs to sign in again.");
                _accessTokenResponse = null;
                NotifyPlayerStateChangeListeners();
            }
        }

        static void ScheduleAccessTokenRefresh()
        {
            if (_accessTokenResponse == null)
            {
                Debug.LogWarning("Access token refresh scheduling requested but no access token is available.");
                return;
            }

            // Schedule a refresh of the access token
            Debug.Log($"Scheduling access token refresh in {_accessTokenExpiringIn}ms");
            _refreshAccessTokenTimer?.Dispose();
            _refreshAccessTokenTimer = new Timer(static async (state) =>
            {
                Debug.Log("Refreshing access token");
                await RefreshAccessToken();
            });
            _refreshAccessTokenTimer.Change(_accessTokenExpiringIn, Timeout.Infinite);
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

        public static void AddPlayerStateChangeListener(Action<AuthState> onPlayerStateChange, bool requireInitialState = false)
        {
            _playerStateChangeListeners.Add(onPlayerStateChange);

            if (requireInitialState)
            {
                onPlayerStateChange(_authState);
            }
        }

        public static void RemovePlayerStateChangeListener(Action<AuthState> onPlayerStateChange)
        {
            _playerStateChangeListeners.Remove(onPlayerStateChange);
        }

        static void NotifyPlayerStateChangeListeners()
        {
            foreach (var listener in _playerStateChangeListeners)
            {
                listener(_authState);
            }
        }

        public static async Task<long> GetPlayerCount()
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/auth/players:count");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var playerCountResponse = await _httpClient.SendAsync(requestMessage);
            if (playerCountResponse.IsSuccessStatusCode)
            {
                var playerCountResponseData = JsonConvert.DeserializeObject<GetPlayerCountResponse>(await playerCountResponse.Content.ReadAsStringAsync());
                return playerCountResponseData.PlayerCount;
            }
            else
            {
                await HandleErrorResponse(playerCountResponse);
                return 0;
            }
        }

        public static async Task<Player> UpdatePlayerInfo(Dictionary<PlayerDataField, PlayerDataOperation> update)
        {
            CheckInitialized();
            CheckSignedIn();

            var updateRequestData = new Dictionary<string, object>();
            foreach (var keyValuePair in update)
            {
                var fieldKey = keyValuePair.Key.key;
                var operationKey = keyValuePair.Value.OperationType;
                var operationValue = keyValuePair.Value.Value;

                string operationKeyStr = "";

                switch (operationKey)
                {
                    case PlayerDataOperationType.Set:
                        operationKeyStr = "$set";
                        break;
                    case PlayerDataOperationType.Inc:
                        operationKeyStr = "$inc";
                        break;
                    case PlayerDataOperationType.Dec:
                        operationKeyStr = "$dec";
                        break;
                    case PlayerDataOperationType.Min:
                        operationKeyStr = "$min";
                        break;
                    case PlayerDataOperationType.Max:
                        operationKeyStr = "$max";
                        break;
                    case PlayerDataOperationType.Mul:
                        operationKeyStr = "$mul";
                        break;
                    case PlayerDataOperationType.Div:
                        operationKeyStr = "$div";
                        break;
                    case PlayerDataOperationType.Unset:
                        operationKeyStr = "$unset";
                        break;
                }

                if (operationKey != PlayerDataOperationType.Unset)
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new Dictionary<string, object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }
                    var operationDataDict = (Dictionary<string, object>) operationData;
                    operationDataDict[fieldKey] = operationValue;
                } else
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new List<object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }
                    var operationDataList = (List<object>) operationData;
                    operationDataList.Add(fieldKey);
                }
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/auth/players/{_uid}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            requestMessage.Headers.Add("X-Http-Method-Override", "PATCH"); // PATCH method is not supported by UnityWebRequest (as of 6000)
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(updateRequestData), System.Text.Encoding.UTF8, "application/json");
            Debug.Log("Updating player with update json: " + await requestMessage.Content.ReadAsStringAsync());
            var playerUpdateResponse = await _httpClient.SendAsync(requestMessage);

            if (playerUpdateResponse.IsSuccessStatusCode)
            {
                return await GetPlayerInfo(_uid!);
            }
            else
            {
                await HandleErrorResponse(playerUpdateResponse);
                return new Player();
            }
        }

        public static AchievementsCursor GetAllAchievements(string? query = null, int pageSize = 30, bool queryByGroup = false)
        {
            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;
            bool reachedEnd = false;

            return new AchievementsCursor(async () =>
            {
                if (reachedEnd) return new Achievement[0];

                var achievements = await GetAchievementsPage(query, pageSize, currentStartAfter, queryByGroup);
                if (achievements.Length < pageSize || achievements.Length == 0)
                {
                    reachedEnd = true;
                }
                currentStartAfter = achievements.LastOrDefault()?.Id;
                return achievements;
            });
        }

        static async Task<Achievement[]> GetAchievementsPage(string? query, int limit, string? startAfter, bool queryByGroup)
        {
            var baseUrl = $"{ServerUrl}/achievements";
            var parameters = new Dictionary<string, string>
            {
                { "limit", limit.ToString() },
            };
            if (!string.IsNullOrEmpty(query))
            {
                parameters["query"] = query;
            }
            if (!string.IsNullOrEmpty(startAfter))
            {
                parameters["startAfter"] = startAfter;
            }
            if (queryByGroup)
            {
                parameters["queryByGroup"] = "true";
            }
            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var achievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData = JsonConvert.DeserializeObject<GetAchievementsResponse>(await achievementsResponse.Content.ReadAsStringAsync())!;
                return achievementsResponseData.Achievements;
            }
            else
            {
                await HandleErrorResponse(achievementsResponse);
                return new Achievement[0];
            }
        }

        public static async Task<string[]> ListAllAchievements()
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/achievements:list");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var listAchievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementsResponse.IsSuccessStatusCode)
            {
                var listAchievementsResponseData = JsonConvert.DeserializeObject<ListAchievementsResponse>(await listAchievementsResponse.Content.ReadAsStringAsync())!;
                return listAchievementsResponseData.AchievementIDs;
            }
            else
            {
                await HandleErrorResponse(listAchievementsResponse);
                return new string[0];
            }
        }

        public static AchievementGroupsCursor GetAchievementGroups(string? query = null, int pageSize = 30)
        {
            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;
            bool reachedEnd = false;

            return new AchievementGroupsCursor(async () =>
            {
                if (reachedEnd) return new AchievementGroup[0];

                var achievementGroups = await GetAchievementGroupsPage(query, pageSize, currentStartAfter);
                if (achievementGroups.Length < pageSize || achievementGroups.Length == 0)
                {
                    reachedEnd = true;
                }
                currentStartAfter = achievementGroups.LastOrDefault()?.Id;
                return achievementGroups;
            });
        }

        static async Task<AchievementGroup[]> GetAchievementGroupsPage(string? query, int limit, string? startAfter)
        {
            var baseUrl = $"{ServerUrl}/achievements:groups";
            var parameters = new Dictionary<string, string>
            {
                { "limit", limit.ToString() },
            };
            if (!string.IsNullOrEmpty(query))
            {
                parameters["query"] = query;
            }
            if (!string.IsNullOrEmpty(startAfter))
            {
                parameters["startAfter"] = startAfter;
            }
            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var achievementGroupsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementGroupsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData = JsonConvert.DeserializeObject<GetAchievementGroupsResponse>(await achievementGroupsResponse.Content.ReadAsStringAsync())!;
                return achievementsResponseData.AchievementGroups;
            }
            else
            {
                await HandleErrorResponse(achievementGroupsResponse);
                return new AchievementGroup[0];
            }
        }

        internal static async Task HandleErrorResponse(HttpResponseMessage response, bool skipResetAuthTokenOnError = false)
        {
            Exception? error = null;
            try
            {
                var errorData = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync())!;
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
                        if (!skipResetAuthTokenOnError)
                        {
                            _accessTokenResponse = null;
                            NotifyPlayerStateChangeListeners();
                        }
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

            if (error == null)
            {
                Debug.LogWarning("Unknown error occured: " + await response.Content.ReadAsStringAsync());
            }
            error ??= new Exception("An unknown error occurred while using Sphere Kit.");

            throw error;
        }

        public static async Task SignOut()
        {
            CheckInitialized();

            // Sign out of the server
            if (_accessTokenResponse != null)
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/oauth/signout");
                requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
                var playerResponse = await _httpClient.SendAsync(requestMessage);
                if (!playerResponse.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(playerResponse);
                }
            }

            // Dispose variables
            CurrentPlayer = null;
            _authenticationSession?.Dispose();
            _authenticationSession = null;
            _accessTokenResponse = null;
            _refreshAccessTokenTimer?.Dispose();
            _refreshAccessTokenTimer = null;
            NotifyPlayerStateChangeListeners();
            Debug.Log("Variables disposed.");

            // Clear player prefs
            MainThreadDispatcher.Execute(() =>
            {
                PlayerPrefs.DeleteKey(accessTokenResponseKey);
                Debug.Log("Access token removed from player prefs.");
            });

            // Create new authentication session
            InitialiseAuthenticationSession();
            Debug.Log("Signed out.");
        }

        public static async Task Dispose()
        {
            await SignOut();
            _playerStateChangeListeners.Clear();
        }
    }
}
