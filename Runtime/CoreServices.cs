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
        /// <summary>
        /// Whether Sphere Kit has been initialised.
        /// </summary>
        public static bool HasInitialized { get; private set; }

        /// <summary>
        /// The signed in player information (present even if authentication has expired). Null if no player has been signed in.
        /// </summary>
        public static Player? CurrentPlayer { get; private set; }

        /// <summary>
        /// The settings for the Achievements module.
        /// </summary>
        public static AchievementsSettings? AchievementsSettings { get; private set; }

        /// <summary>
        /// The settings for the Database module.
        /// </summary>
        public static DatabaseSettings DatabaseSettings { get; private set; }

        /// <summary>
        /// The access token for the current player. Null if no player is currently signed in.
        /// </summary>
        internal static string? AccessToken => _accessTokenResponse?.accessToken;

        /// <summary>
        /// The client ID for the project.
        /// </summary>
        internal static string ProjectId => _projectId;

        /// <summary>
        /// The server URL for the project.
        /// </summary>
        internal static string ServerUrl => _serverUrl;

        /// <summary>
        /// Whether the user is signed in.
        /// </summary>
        private static bool IsSignedIn => _accessTokenResponse != null;

        private static string _clientId = "";
        private static string _projectId = "";
        private static string _serverUrl = "";
        private static string _deepLinkScheme = "";
        private static bool _internalDevelopmentMode = false;
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

        private const string AccessTokenResponseKey = "accessTokenResponse";

        /// <summary>
        /// Initialises Sphere Kit. This method must be called before any other methods in the Sphere Kit SDK.
        /// </summary>
        /// <exception cref="MissingFieldException">Some required settings fields were not configured.</exception>
        /// 
        public static async Task Initialize()
        {
            // Load configuration
            var config = ProjectConfig.GetOrCreateConfig();
            if (config == null || config.clientID == "")
                throw new MissingFieldException(
                    "The Client ID has not been configured yet. Please configure Sphere Kit in Project Settings.");

            if (config == null || config.projectID == "")
                throw new MissingFieldException(
                    "The Project ID has not been configured yet. Please configure Sphere Kit in Project Settings.");

            if (config.serverURL == "")
                throw new MissingFieldException(
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
            _internalDevelopmentMode = config.internalDevelopmentMode;

#if UNITY_IOS || UNITY_ANDROID
            _redirectUri = _deepLinkScheme + "://oauth";
#else
            _redirectUri = "http://localhost:8000/spherekit/oauth";
#endif

            // Initialise authentication session
            InitialiseAuthenticationSession();

            // Get database settings
            await GetDatabaseSettings();

            // Load access token response from player prefs
            if (PlayerPrefs.HasKey(AccessTokenResponseKey))
            {
                try
                {
                    _accessTokenResponse =
                        JsonConvert.DeserializeObject<AccessTokenResponse>(
                            PlayerPrefs.GetString(AccessTokenResponseKey));
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
                        await RefreshAccessToken();
                    else
                        try
                        {
                            await InternalGetPlayerInfo(_uid!);
                            await GetAchievementsSettings();
                        }
                        catch (Exception e)
                        {
                            if (e is AuthenticationException)
                            {
                                _accessTokenResponse = null;
                                await SignOut();
                            }
                            else
                            {
                                throw;
                            }
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

        /// <summary>
        /// Sets up the authentication session.
        /// </summary>
        private static void InitialiseAuthenticationSession()
        {
            // ReSharper disable once AccessToStaticMemberViaDerivedType
            var authConfig = new AuthorizationCodeFlowWithPkce.Configuration()
            {
                clientId = _clientId,
                scope = "profile project",
                redirectUri = _redirectUri,
                internalDevelopmentMode = _internalDevelopmentMode
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

        /// <summary>
        /// Checks if Sphere Kit has been initialised. Throws an exception if it has not been initialised.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sphere Kit has not been initialised.</exception>
        internal static void CheckInitialized()
        {
            if (!HasInitialized)
                throw new InvalidOperationException(
                    "Sphere Kit has not been initialized. Please call SphereKit.Core.Initialize() before calling any other methods.");
        }

        /// <summary>
        /// Signs in the player to the game using their Sphere account.
        /// </summary>
        /// <exception cref="InvalidOperationException">Authentication will not work on iOS and Android because the Deep Link Scheme has not been configured.</exception>
        /// <exception cref="AuthenticationException">OAuth authentication may fail due to user cancellation or operation timeout.</exception>
        public static async Task SignInWithSphere()
        {
            CheckInitialized();

            // Check if the user is already signed in
            if (IsSignedIn) return;

            // Check platform specific requirements
#if UNITY_IOS || UNITY_ANDROID
            if (string.IsNullOrEmpty(_deepLinkScheme))
                throw new InvalidOperationException(
                    "The Deep Link Scheme has not been configured in Project Settings yet. Authentication will not work on iOS and Android.");
#endif

            // Start OAuth2 flow
            _accessTokenResponse = await _authenticationSession!.AuthenticateAsync();

            // Get player info and achievements settings
            await InternalGetPlayerInfo(_uid!);
            await GetAchievementsSettings();
            StoreAccessTokenResponse();

            Debug.Log("Access token received from server.");

            // Schedule access token refresh
            ScheduleAccessTokenRefresh();
        }

        /// <summary>
        /// Checks if the user is signed in. Throws an exception if the user is not signed in.
        /// </summary>
        /// <exception cref="AuthenticationException">User is not signed in.</exception>
        internal static void CheckSignedIn()
        {
            if (!IsSignedIn)
                throw new AuthenticationException("auth/not-signed-in", "User is not signed in.");
        }

        /// <summary>
        /// Internal use only. Gets the player information for the given UID.
        /// If the UID matches the current signed in UID, the signed-in player information is also updated.
        /// Unlike the public <see cref="GetPlayerInfo"/>, this method does not check if Sphere Kit has been initialised.
        /// </summary>
        /// <param name="uid">The UID of the player to retrieve.</param>
        /// <returns>The player information for the UID specified.</returns>
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

        /// <summary>
        /// Gets the player information for the given UID.
        /// </summary>
        /// <param name="uid">The UID of the player to retrieve.</param>
        /// <returns>The player information for the UID specified.</returns>
        public static async Task<Player> GetPlayerInfo(string uid)
        {
            CheckInitialized();

            return await InternalGetPlayerInfo(uid);
        }

        /// <summary>
        /// Gets the settings for the Achievements module and sets the <see cref="AchievementsSettings"/> property.
        /// </summary>
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

                Debug.Log("Achievements settings retrieved and set.");
            }
            else
            {
                await HandleErrorResponse(settingsResponse);
            }
        }

        /// <summary>
        /// Gets the settings for the Database module and sets the <see cref="DatabaseSettings"/> property.
        /// </summary>
        private static async Task GetDatabaseSettings()
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/databases:list");
            requestMessage.Headers.Add("X-Sphere-Project-Name", _projectId);
            Debug.Log($"{ServerUrl}/databases:list");
            var databasesResponse = await _httpClient.SendAsync(requestMessage);
            if (databasesResponse.IsSuccessStatusCode)
            {
                var databaseResponse = JsonConvert.DeserializeObject<ListDatabasesResponse>(await databasesResponse
                    .Content
                    .ReadAsStringAsync());
                var databaseId = databaseResponse.Databases.Length > 0 ? databaseResponse.Databases.First() : null;
                DatabaseSettings = new DatabaseSettings(databaseId);

                Debug.Log("Database settings retrieved and set.");
            }
            else
            {
                await HandleErrorResponse(databasesResponse);
            }
        }

        /// <summary>
        /// Refreshes the access token using the refresh token, and schedules the next refresh if successful.
        /// </summary>
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

                // Get player info and achievements settings
                await InternalGetPlayerInfo(_uid!);
                await GetAchievementsSettings();
                StoreAccessTokenResponse();

                Debug.Log("Access token refreshed.");

                // Schedule next refresh
                ScheduleAccessTokenRefresh();
            }
            catch (AuthenticationException)
            {
                Debug.LogWarning("Failed to refresh token. User needs to sign in again.");
                _accessTokenResponse = null;
                NotifyPlayerStateChangeListeners();
            }
        }

        /// <summary>
        /// Schedules the next access token refresh.
        /// </summary>
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

        /// <summary>
        /// Stores the access token response in player prefs.
        /// </summary>
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
                PlayerPrefs.SetString(AccessTokenResponseKey, accessTokenResponseJson);
                Debug.Log("Stored access token in player prefs");
            });
        }

        /// <summary>
        /// Adds a listener for player authentication and information state changes.
        /// </summary>
        /// <param name="onPlayerStateChange">A callback for when the player state changes.</param>
        /// <param name="requireInitialState">Whether a call of the <see cref="onPlayerStateChange"/> function immediately after the listener is added is required.</param>
        public static void AddPlayerStateChangeListener(Action<AuthState> onPlayerStateChange,
            bool requireInitialState = false)
        {
            _playerStateChangeListeners.Add(onPlayerStateChange);

            if (requireInitialState) onPlayerStateChange(_authState);
        }

        /// <summary>
        /// Removes a listener for player authentication and information state changes.
        /// </summary>
        /// <param name="onPlayerStateChange">The callback to be removed.</param>
        public static void RemovePlayerStateChangeListener(Action<AuthState> onPlayerStateChange)
        {
            _playerStateChangeListeners.Remove(onPlayerStateChange);
        }

        /// <summary>
        /// Notifies all player state change listeners of a change in player state.
        /// </summary>
        private static void NotifyPlayerStateChangeListeners()
        {
            foreach (var listener in _playerStateChangeListeners) listener(_authState);
        }

        /// <summary>
        /// Gets the total number of players in the game.
        /// </summary>
        /// <returns>The total number of players in the game.</returns>
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

        /// <summary>
        /// Updates the signed-in player's metadata.
        /// </summary>
        /// <param name="update">The update specification.</param>
        /// <returns>The updated player information.</returns>
        public static async Task<Player> UpdatePlayerMetadata(Dictionary<string, PlayerDataOperation> update)
        {
            CheckInitialized();
            CheckSignedIn();

            var updateRequestData = PlayerDataOperation.ConvertUpdateToRequestData(update);

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

        /// <summary>
        /// Gets achievements available in the game.
        /// If no sort field/direction is provided, achievements will be returned in ascending order of their display name.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="pageSize">The number of achievements to retrieve at a time.</param>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <param name="ungrouped">Whether to get ungrouped achievements.</param>
        /// <param name="sortBy">The field to sort the achievements by.</param>
        /// <param name="sortDirection">The direction to sort the achievements by.</param>
        /// <returns>A cursor where the next <see cref="pageSize"/> achievements can be retrieved.</returns>
        public static AchievementsCursor GetAchievements(string? query = null, int pageSize = 30,
            string? groupName = null, bool? ungrouped = null,
            AchievementsSortField sortBy = AchievementsSortField.DisplayName,
            SortDirection sortDirection = SortDirection.Ascending)
        {
            if ((groupName == null || ungrouped == true) && sortBy == AchievementsSortField.GroupOrder)
                throw new ArgumentException(
                    "Cannot sort by group order without filtering by a group. Please provide a group name.");

            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;

            var cursor = new AchievementsCursor();
            cursor.Next = async () =>
            {
                if (!cursor.HasNext) return Array.Empty<Achievement>();

                var achievements = await GetAchievementsPage(query, pageSize, currentStartAfter, groupName, ungrouped,
                    sortBy, sortDirection);
                if (achievements.Length < pageSize || achievements.Length == 0) cursor.HasNext = false;

                currentStartAfter = achievements.LastOrDefault()?.Id;
                return achievements;
            };
            return cursor;
        }

        /// <summary>
        /// Gets a list of achievements. Used by the cursor to get the next page of achievements.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="limit">The number of achievements to retrieve at a time.</param>
        /// <param name="startAfter">The achievement ID to start retrieving achievements after.</param>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <param name="ungrouped">Whether to get ungrouped achievements.</param>
        /// <param name="sortBy">The field to sort the achievements by.</param>
        /// <param name="sortDirection">The direction to sort the achievements by.</param>
        /// <returns>A list of achievements matching the parameters.</returns>
        private static async Task<Achievement[]> GetAchievementsPage(string? query, int limit, string? startAfter,
            string? groupName, bool? ungrouped, AchievementsSortField sortBy, SortDirection sortDirection)
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

            if (!string.IsNullOrEmpty(groupName)) parameters["groupName"] = groupName;

            if (ungrouped != null) parameters["ungrouped"] = ungrouped.ToString();

            parameters["sortField"] = sortBy switch
            {
                AchievementsSortField.DisplayName => "displayName",
                AchievementsSortField.GroupOrder => "groupOrder",
                AchievementsSortField.PercentageAchieved => "percentageAchieved",
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
            };

            parameters["sortDirection"] = sortDirection switch
            {
                SortDirection.Ascending => "asc",
                SortDirection.Descending => "desc",
                _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
            };

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

        /// <summary>
        /// Gets an array of core achievement information in the game - including achievement ID, display name, group name and group order.
        /// </summary>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <param name="ungrouped">Whether to get ungrouped achievements.</param>
        /// <param name="sortBy">The field to sort the achievements by.</param>
        /// <param name="sortDirection">The direction to sort the achievements by.</param>
        /// <returns>An array of core achievement information.</returns>
        public static async Task<ListedAchievement[]> ListAllAchievements(string? groupName = null,
            bool? ungrouped = null,
            AchievementsSortField sortBy = AchievementsSortField.DisplayName,
            SortDirection sortDirection = SortDirection.Ascending)
        {
            if ((groupName == null || ungrouped == true) && sortBy == AchievementsSortField.GroupOrder)
                throw new ArgumentException(
                    "Cannot sort by group order without filtering by a group. Please provide a group name.");

            CheckInitialized();
            CheckSignedIn();

            var baseUrl = $"{ServerUrl}/achievements:list";
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(groupName)) parameters["groupName"] = groupName;

            if (ungrouped != null) parameters["ungrouped"] = ungrouped.ToString();

            parameters["sortField"] = sortBy switch
            {
                AchievementsSortField.DisplayName => "displayName",
                AchievementsSortField.GroupOrder => "groupOrder",
                AchievementsSortField.PercentageAchieved => "percentageAchieved",
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
            };

            parameters["sortDirection"] = sortDirection switch
            {
                SortDirection.Ascending => "asc",
                SortDirection.Descending => "desc",
                _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
            };

            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var listAchievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementsResponse.IsSuccessStatusCode)
            {
                var listAchievementsResponseData =
                    JsonConvert.DeserializeObject<ListAchievementsResponse>(await listAchievementsResponse.Content
                        .ReadAsStringAsync());
                return listAchievementsResponseData.Achievements;
            }
            else
            {
                await HandleErrorResponse(listAchievementsResponse);
                return Array.Empty<ListedAchievement>();
            }
        }

        /// <summary>
        /// Gets achievement groups available in the game.
        /// If no sort field/direction is provided, achievement groups will be returned in ascending order of their display name.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="pageSize">The number of achievement groups to retrieve at a time.</param>
        /// <param name="sortBy">The field to sort the achievement groups by.</param>
        /// <param name="sortDirection">The direction to sort the achievements groups by.</param>
        /// <returns>A cursor where the next <see cref="pageSize"/> achievement groups can be retrieved.</returns>
        public static AchievementGroupsCursor GetAchievementGroups(string? query = null, int pageSize = 30,
            AchievementGroupsSortField sortBy = AchievementGroupsSortField.DisplayName,
            SortDirection sortDirection = SortDirection.Ascending)
        {
            CheckInitialized();
            CheckSignedIn();

            string? currentStartAfter = null;

            var cursor = new AchievementGroupsCursor();
            cursor.Next = async () =>
            {
                if (!cursor.HasNext) return Array.Empty<AchievementGroup>();

                var achievementGroups = await GetAchievementGroupsPage(query, pageSize, currentStartAfter);
                if (achievementGroups.Length < pageSize || achievementGroups.Length == 0) cursor.HasNext = false;

                currentStartAfter = achievementGroups.LastOrDefault()?.Id;
                return achievementGroups;
            };
            return cursor;
        }

        /// <summary>
        /// Gets a list of achievement groups. Used by the cursor to get the next page of achievement groups.
        /// If no sort field/direction is provided, achievement groups will be returned in ascending order of their display name.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="limit">The number of achievement groups to retrieve at a time.</param>
        /// <param name="startAfter">The achievement group ID to start retrieving achievement groups after.</param>
        /// <param name="sortBy">The field to sort the achievement groups by.</param>
        /// <param name="sortDirection">The direction to sort the achievements groups by.</param>
        /// <returns>A list of achievement groups matching the parameters.</returns>
        private static async Task<AchievementGroup[]> GetAchievementGroupsPage(string? query, int limit,
            string? startAfter, AchievementGroupsSortField sortBy = AchievementGroupsSortField.DisplayName,
            SortDirection sortDirection = SortDirection.Ascending)
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

            parameters["sortField"] = sortBy switch
            {
                AchievementGroupsSortField.DisplayName => "displayName",
                _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
            };

            parameters["sortDirection"] = sortDirection switch
            {
                SortDirection.Ascending => "asc",
                SortDirection.Descending => "desc",
                _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
            };

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

        /// <summary>
        /// Gets an array of all achievement group IDs and their display name in the game.
        /// </summary>
        /// <returns>An array of all achievement group IDs and their display name.</returns>
        public static async Task<ListedAchievementGroup[]> ListAllAchievementGroups(
            AchievementGroupsSortField sortBy = AchievementGroupsSortField.DisplayName,
            SortDirection sortDirection = SortDirection.Ascending)
        {
            CheckInitialized();
            CheckSignedIn();

            var baseUrl = $"{ServerUrl}/achievements:groups:list";
            var parameters = new Dictionary<string, string>
            {
                ["sortField"] = sortBy switch
                {
                    AchievementGroupsSortField.DisplayName => "displayName",
                    _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
                },
                ["sortDirection"] = sortDirection switch
                {
                    SortDirection.Ascending => "asc",
                    SortDirection.Descending => "desc",
                    _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
                }
            };

            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
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

        /// <summary>
        /// Adds an achievement to the signed-in player's achievements.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to add.</param>
        public static async Task AcquireAchievement(string achievementId)
        {
            CheckInitialized();
            CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"{ServerUrl}/auth/players/{_uid}/achievements/{achievementId}");
            requestMessage.Headers.Add("Authorization", $"Bearer {AccessToken}");
            var addAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (!addAchievementResponse.IsSuccessStatusCode) await HandleErrorResponse(addAchievementResponse);
        }

        /// <summary>
        /// Removes an achievement from the signed-in player's achievements.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to remove.</param>
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

        /// <summary>
        /// Handles an error HTTP response from the server. Will throw an exception based on the error code.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="skipResetAuthTokenOnError">Whether to not reset the authentication state if there is a 401 error.</param>
        internal static async Task HandleErrorResponse(HttpResponseMessage response,
            bool skipResetAuthTokenOnError = false)
        {
            var errorString = await response.Content.ReadAsStringAsync();
            HandleErrorString(errorString, skipResetAuthTokenOnError);
        }

        /// <summary>
        /// Handles an error JSON string from the server. Will throw an exception based on the error code.
        /// </summary>
        /// <param name="errorString">The error JSON string.</param>
        /// <param name="skipResetAuthTokenOnError">Whether to not reset the authentication state if there is a 401 error.</param>
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

        /// <summary>
        /// Signs out of the current player account.
        /// </summary>
        public static async Task SignOut()
        {
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
            AchievementsSettings = null;
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
                PlayerPrefs.DeleteKey(AccessTokenResponseKey);
                Debug.Log("Access token removed from player prefs.");
            });

            // Create new authentication session
            InitialiseAuthenticationSession();
            Debug.Log("Signed out.");
        }

        /// <summary>
        /// Disposes of Sphere Kit, signs out the player and clears all listeners.
        /// </summary>
        public static async Task Dispose()
        {
            await SignOut();
            _playerStateChangeListeners.Clear();
        }
    }

    public enum AchievementsSortField
    {
        DisplayName,
        PercentageAchieved,
        GroupOrder
    }

    public enum AchievementGroupsSortField
    {
        DisplayName
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }
}