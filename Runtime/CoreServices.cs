using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SphereKit.Utils;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("projectquik.spherekit.database")]
#nullable enable
namespace SphereKit
{
    public static class CoreServices
    {
        public static bool HasInitialized { get; private set; }
        public static Player? CurrentPlayer { get; private set; }
        public static AchievementsSettings? AchievementsSettings { get; private set; }
        public static DatabaseSettings DatabaseSettings { get; private set; }
        internal static string? AccessToken => _accessTokenResponse?.accessToken;
        internal static string ProjectId => _projectId;
        internal static string ServerUrl => _serverUrl;

        private static string _clientId = "";
        private static string _projectId = "";
        private static string _serverUrl = "";
        private static string _deepLinkScheme = "";
        private static string _redirectUri = "";
        private static AuthenticationSession? _authenticationSession;
        private static AccessTokenResponse? _accessTokenResponse;
#pragma warning disable CS8629 // Nullable value type may be null.
        private static long _accessTokenExpiringIn => _accessTokenResponse == null
            ? 0
            : Math.Max(
                Convert.ToInt64((_accessTokenResponse.expiresAt - DateTime.UtcNow).Value.TotalMilliseconds) -
                120 * 1000, 0);
#pragma warning restore CS8629 // Nullable value type may be null.
        private static Timer? _refreshAccessTokenTimer;
        private static readonly HttpClient _httpClient = new();
        private static readonly List<Action<AuthState>> _playerStateChangeListeners = new();
        private static AuthState _authState => new(_accessTokenResponse != null, CurrentPlayer);
        private static string? _uid => _accessTokenResponse?.user.uid;

        private const string accessTokenResponseKey = "accessTokenResponse";

        public static async Task Initialize()
        {
            // Load configuration
            var config = ProjectConfig.GetOrCreateConfig();
            if (config == null || config.clientID == "")
                throw new Exception(
                    "The Client ID has not been configured yet. Please configure Sphere Kit in Project Settings.");

            if (config == null || config.projectID == "")
                throw new Exception(
                    "The Project ID has not been configured yet. Please configure Sphere Kit in Project Settings.");

            if (config.serverURL == "")
                throw new Exception(
                    "The Server URL has not been configured yet. Please configure Sphere Kit in Project Settings.");

            if (config.deepLinkScheme == "")
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "The Deep Link Scheme has not been configured in Project Settings yet. Authentication will not work on iOS and Android.");
#endif
            }

            _clientId = config.clientID;
            _projectId = config.projectID;
            _serverUrl = config.serverURL;
            _deepLinkScheme = config.deepLinkScheme;

#if UNITY_IOS || UNITY_ANDROID
            _redirectUri = _deepLinkScheme + "://oauth";
#else
            _redirectUri = "http://localhost:8080/spherekit/oauth";
#endif

            // Check internet connectivity
            var uri = new Uri(_serverUrl);
            var ping = new System.Net.NetworkInformation.Ping();
            var pingReply = ping.Send(uri.Host);
            if (pingReply?.Status != IPStatus.Success)
                throw new Exception("Could not connect to Sphere Kit. Please check your network settings.");

            // Initialise authentication session
            InitialiseAuthenticationSession();

            // Get database settings
            await GetDatabaseSettings();

            // Load access token response from player prefs
            if (PlayerPrefs.HasKey(accessTokenResponseKey))
            {
                try
                {
                    _accessTokenResponse =
                        JsonConvert.DeserializeObject<AccessTokenResponse>(
                            PlayerPrefs.GetString(accessTokenResponseKey));
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
                    }
                    else
                    {
                        await InternalGetPlayerInfo(_uid!);
                        await GetAchievementsSettings();
                    }
                }
            }
            else
            {
                NotifyPlayerStateChangeListeners();
            }

            HasInitialized = true;
            Debug.Log($"Sphere Kit has been initialized.");
        }

        private static void InitialiseAuthenticationSession()
        {
            // ReSharper disable once AccessToStaticMemberViaDerivedType
            var authConfig = new AuthorizationCodeFlowWithPkce.Configuration()
            {
                clientId = _clientId,
                scope = "profile project",
                redirectUri = _redirectUri
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
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer,
                new ASWebAuthenticationSessionBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new StandaloneBrowser());
            _authenticationSession = new AuthenticationSession(auth, crossPlatformBrowser);
        }

        internal static void CheckInitialized()
        {
            if (!HasInitialized)
                throw new Exception(
                    "Sphere Kit has not been initialized. Please call SphereKit.Core.Initialize() before calling any other methods.");
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
            await InternalGetPlayerInfo(_uid!);
            await GetAchievementsSettings();
            StoreAccessTokenResponse();

            Debug.Log("Access token received from server.");
            ScheduleAccessTokenRefresh();
        }

        internal static void CheckSignedIn()
        {
            if (_accessTokenResponse == null)
                throw new Exception("User is not signed in."); // TODO: Use AuthenticationException
        }

        private static async Task<Player> InternalGetPlayerInfo(string uid)
        {
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/auth/players/{uid}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var playerResponse = await _httpClient.SendAsync(requestMessage);
            if (playerResponse.IsSuccessStatusCode)
            {
                var retrievedPlayer =
                    JsonConvert.DeserializeObject<Player>(await playerResponse.Content.ReadAsStringAsync())!;
                if (uid == _uid)
                {
                    CurrentPlayer = retrievedPlayer;
                    NotifyPlayerStateChangeListeners();
                }

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

        private static async Task GetAchievementsSettings()
        {
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/achievements:settings");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var settingsResponse = await _httpClient.SendAsync(requestMessage);
            if (settingsResponse.IsSuccessStatusCode)
            {
                AchievementsSettings = JsonConvert.DeserializeObject<AchievementsSettings>(await settingsResponse
                    .Content
                    .ReadAsStringAsync())!;

                Debug.Log("Settings retrieved and set.");
            }
            else
            {
                await HandleErrorResponse(settingsResponse);
            }
        }

        private static async Task GetDatabaseSettings()
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/databases:list");
            requestMessage.Headers.Add("X-Sphere-Project-Name", _projectId);
            var databasesResponse = await _httpClient.SendAsync(requestMessage);
            if (databasesResponse.IsSuccessStatusCode)
            {
                var databaseResponse = JsonConvert.DeserializeObject<ListDatabasesResponse>(await databasesResponse
                    .Content
                    .ReadAsStringAsync());
                var databaseId = databaseResponse.Databases.First();
                DatabaseSettings = new DatabaseSettings(databaseId);

                Debug.Log("Database settings retrieved and set.");
            }
            else
            {
                await HandleErrorResponse(databasesResponse);
            }
        }

        private static async Task RefreshAccessToken()
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
                await GetAchievementsSettings();
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

        private static void ScheduleAccessTokenRefresh()
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

        private static void StoreAccessTokenResponse()
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

        public static void AddPlayerStateChangeListener(Action<AuthState> onPlayerStateChange,
            bool requireInitialState = false)
        {
            _playerStateChangeListeners.Add(onPlayerStateChange);

            if (requireInitialState) onPlayerStateChange(_authState);
        }

        public static void RemovePlayerStateChangeListener(Action<AuthState> onPlayerStateChange)
        {
            _playerStateChangeListeners.Remove(onPlayerStateChange);
        }

        private static void NotifyPlayerStateChangeListeners()
        {
            foreach (var listener in _playerStateChangeListeners) listener(_authState);
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
                var playerCountResponseData =
                    JsonConvert.DeserializeObject<GetPlayerCountResponse>(await playerCountResponse.Content
                        .ReadAsStringAsync());
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
                var fieldKey = keyValuePair.Key.Key;
                var operationKey = keyValuePair.Value.OperationType;
                var operationValue = keyValuePair.Value.Value;
                var operationKeyStr = operationKey switch
                {
                    PlayerDataOperationType.Set => "$set",
                    PlayerDataOperationType.Inc => "$inc",
                    PlayerDataOperationType.Dec => "$dec",
                    PlayerDataOperationType.Min => "$min",
                    PlayerDataOperationType.Max => "$max",
                    PlayerDataOperationType.Mul => "$mul",
                    PlayerDataOperationType.Div => "$div",
                    PlayerDataOperationType.Unset => "$unset",
                    _ => ""
                };

                if (operationKey != PlayerDataOperationType.Unset)
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new Dictionary<string, object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }

                    var operationDataDict = (Dictionary<string, object>)operationData;
                    operationDataDict[fieldKey] = operationValue!;
                }
                else
                {
                    if (!updateRequestData.TryGetValue(operationKeyStr, out var operationData))
                    {
                        operationData = new List<object>();
                        updateRequestData[operationKeyStr] = operationData;
                    }

                    var operationDataList = (List<object>)operationData;
                    operationDataList.Add(fieldKey);
                }
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/auth/players/{_uid}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            requestMessage.Headers.Add("X-Http-Method-Override",
                "PATCH"); // PATCH method is not supported by UnityWebRequest (as of 6000)
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(updateRequestData),
                System.Text.Encoding.UTF8, "application/json");
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

        public static AchievementsCursor GetAllAchievements(string? query = null, int pageSize = 30,
            bool queryByGroup = false)
        {
            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;
            var reachedEnd = false;

            return new AchievementsCursor(async () =>
            {
                if (reachedEnd) return Array.Empty<Achievement>();

                var achievements = await GetAchievementsPage(query, pageSize, currentStartAfter, queryByGroup);
                if (achievements.Length < pageSize || achievements.Length == 0) reachedEnd = true;

                currentStartAfter = achievements.LastOrDefault()?.Id;
                return achievements;
            });
        }

        private static async Task<Achievement[]> GetAchievementsPage(string? query, int limit, string? startAfter,
            bool queryByGroup)
        {
            CheckInitialized();
            CheckSignedIn();

            var baseUrl = $"{ServerUrl}/achievements";
            var parameters = new Dictionary<string, string>
            {
                { "limit", limit.ToString() }
            };
            if (!string.IsNullOrEmpty(query)) parameters["query"] = query;

            if (!string.IsNullOrEmpty(startAfter)) parameters["startAfter"] = startAfter;

            if (queryByGroup) parameters["queryByGroup"] = "true";

            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var achievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData =
                    JsonConvert.DeserializeObject<GetAchievementsResponse>(await achievementsResponse.Content
                        .ReadAsStringAsync());
                return achievementsResponseData.Achievements;
            }
            else
            {
                await HandleErrorResponse(achievementsResponse);
                return Array.Empty<Achievement>();
            }
        }

        public static async Task<string[]> ListAllAchievements()
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/achievements:list");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var listAchievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementsResponse.IsSuccessStatusCode)
            {
                var listAchievementsResponseData =
                    JsonConvert.DeserializeObject<ListAchievementsResponse>(await listAchievementsResponse.Content
                        .ReadAsStringAsync());
                return listAchievementsResponseData.AchievementIDs;
            }
            else
            {
                await HandleErrorResponse(listAchievementsResponse);
                return Array.Empty<string>();
            }
        }

        public static AchievementGroupsCursor GetAchievementGroups(string? query = null, int pageSize = 30)
        {
            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;
            var reachedEnd = false;

            return new AchievementGroupsCursor(async () =>
            {
                if (reachedEnd) return Array.Empty<AchievementGroup>();

                var achievementGroups = await GetAchievementGroupsPage(query, pageSize, currentStartAfter);
                if (achievementGroups.Length < pageSize || achievementGroups.Length == 0) reachedEnd = true;

                currentStartAfter = achievementGroups.LastOrDefault()?.Id;
                return achievementGroups;
            });
        }

        private static async Task<AchievementGroup[]> GetAchievementGroupsPage(string? query, int limit,
            string? startAfter)
        {
            CheckInitialized();
            CheckSignedIn();

            var baseUrl = $"{ServerUrl}/achievements:groups";
            var parameters = new Dictionary<string, string>
            {
                { "limit", limit.ToString() }
            };
            if (!string.IsNullOrEmpty(query)) parameters["query"] = query;

            if (!string.IsNullOrEmpty(startAfter)) parameters["startAfter"] = startAfter;

            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var achievementGroupsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementGroupsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData =
                    JsonConvert.DeserializeObject<GetAchievementGroupsResponse>(
                        await achievementGroupsResponse.Content.ReadAsStringAsync());
                return achievementsResponseData.AchievementGroups;
            }
            else
            {
                await HandleErrorResponse(achievementGroupsResponse);
                return Array.Empty<AchievementGroup>();
            }
        }

        public static async Task<ListedAchievementGroup[]> ListAchievementGroups()
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/achievements:groups:list");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var listAchievementGroupsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementGroupsResponse.IsSuccessStatusCode)
            {
                var listAchievementGroupsResponseData =
                    JsonConvert.DeserializeObject<ListAchievementGroupsResponse>(
                        await listAchievementGroupsResponse.Content.ReadAsStringAsync());
                return listAchievementGroupsResponseData.AchievementGroups;
            }
            else
            {
                await HandleErrorResponse(listAchievementGroupsResponse);
                return Array.Empty<ListedAchievementGroup>();
            }
        }

        public static async Task AddAchievement(string achievementId)
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"{ServerUrl}/auth/players/{_uid}/achievements/{achievementId}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var addAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (!addAchievementResponse.IsSuccessStatusCode) await HandleErrorResponse(addAchievementResponse);
        }

        public static async Task RemoveAchievement(string achievementId)
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{ServerUrl}/auth/players/{_uid}/achievements/{achievementId}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var removeAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (!removeAchievementResponse.IsSuccessStatusCode) await HandleErrorResponse(removeAchievementResponse);
        }

        internal static async Task HandleErrorResponse(HttpResponseMessage response,
            bool skipResetAuthTokenOnError = false)
        {
            var errorString = await response.Content.ReadAsStringAsync();
            HandleErrorString(errorString, skipResetAuthTokenOnError);
        }

        internal static void HandleErrorString(string errorString,
            bool skipResetAuthTokenOnError = false)
        {
            Exception? error = null;
            try
            {
                var errorData =
                    JsonConvert.DeserializeObject<ErrorResponse>(errorString);
                switch (errorData.StatusCode)
                {
                    case 500:
                        error = new InternalServerException(errorData.ErrorMessage);
                        break;
                    case 429:
                        error = new RateLimitException(errorData.ErrorMessage);
                        break;
                    case 404:
                        error = new NotFoundException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                    case 403:
                        error = new ForbiddenException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                    case 401:
                        error = new AuthenticationException(errorData.ErrorCode, errorData.ErrorMessage);
                        if (!skipResetAuthTokenOnError)
                        {
                            _accessTokenResponse = null;
                            NotifyPlayerStateChangeListeners();
                        }

                        break;
                    case 400:
                        error = new BadRequestException(errorData.ErrorCode, errorData.ErrorMessage);
                        break;
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            error ??= new Exception($"An unknown error occurred while using Sphere Kit: {errorString}");

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
                if (!playerResponse.IsSuccessStatusCode) await HandleErrorResponse(playerResponse);
            }

            // Dispose variables
            CurrentPlayer = null;
            _authenticationSession?.Dispose();
            _authenticationSession = null;
            _accessTokenResponse = null;
            if (_refreshAccessTokenTimer != null)
            {
                await _refreshAccessTokenTimer.DisposeAsync();
                _refreshAccessTokenTimer = null;
            }

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