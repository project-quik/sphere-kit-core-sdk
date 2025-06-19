using Newtonsoft.Json;
using SphereKit.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    [Preserve]
    [DataContract]
    public class Player
    {
        /// <summary>
        /// The ID of the player.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "uid")]
        public readonly string Uid = "";

        /// <summary>
        /// The username of the player.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "name")]
        public readonly string UserName = "";

        /// <summary>
        /// The display name of the player.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "realName")]
        public readonly string DisplayName = "";

        /// <summary>
        /// The profile picture URL of the player. Expires in 7 days from time of generation.
        /// </summary>
        [Preserve] [DataMember(Name = "profilepicURL")]
        public readonly string? ProfilePicUrl;

        /// <summary>
        /// The email of the player. Only available if the player retrieved is the current signed in player.
        /// </summary>
        [Preserve] [DataMember(Name = "email")]
        public readonly string? Email;

        [Preserve]
        [DataMember(IsRequired = true, Name = "joinDate")]
        private string _joinDateStr
        {
            set => JoinDate = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The date the player first joined this game.
        /// </summary>
        public DateTime? JoinDate { get; private set; }

        /// <summary>
        /// The metadata of the player. This is visible in the Sphere app.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "metadata")]
        public readonly Dictionary<string, string> Metadata = new();

        /// <summary>
        /// Whether the player is banned.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "isBanned")]
        public readonly bool IsBanned;

        [Preserve]
        [DataMember(Name = "banStartTime")]
        private long _banStartTimeMillis
        {
            set => BanStartDate = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
        }

        /// <summary>
        /// The date and time the ban started.
        /// </summary>
        public DateTime? BanStartDate { get; private set; }

        [Preserve]
        [DataMember(Name = "banDuration")]
        private long _banDurationHours
        {
            set => BanDuration = TimeSpan.FromHours(value);
        }

        /// <summary>
        /// The duration of the ban.
        /// </summary>
        public TimeSpan? BanDuration { get; private set; }

        /// <summary>
        /// The reason for the ban.
        /// </summary>
        [Preserve] [DataMember(Name = "banReason")]
        public readonly string? BanReason;

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Gets achievements achieved by the player.
        /// Achievements will be ordered by the date and time they were achieved.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="pageSize">The number of achievements to retrieve at a time.</param>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <returns>A cursor where the next <see cref="pageSize"/> achievements can be retrieved.</returns>
        public PlayerAchievementsCursor GetPlayerAchievements(string? query = null, int pageSize = 30,
            string? groupName = null)
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            string? currentStartAfter = null;

            var cursor = new PlayerAchievementsCursor();
            cursor.Next = async () =>
            {
                if (!cursor.HasNext) return Array.Empty<PlayerAchievement>();

                var achievements = await GetPlayerAchievementsPage(query, pageSize, currentStartAfter, groupName);
                if (achievements.Length < pageSize || achievements.Length == 0) cursor.HasNext = false;
                currentStartAfter = achievements.LastOrDefault()?.Id;
                return achievements;
            };
            return cursor;
        }

        /// <summary>
        /// Gets a list of achievements achieved by the player. Used by the cursor to get the next page of achievements.
        /// Achievements will be ordered by the date and time they were achieved.
        /// </summary>
        /// <param name="query">A full-text search query, either by the display name, short description or detailed description.</param>
        /// <param name="limit">The number of achievements to retrieve at a time.</param>
        /// <param name="startAfter">The achievement ID to start retrieving achievements after.</param>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <returns>A list of achievements matching the parameters.</returns>
        private async Task<PlayerAchievement[]> GetPlayerAchievementsPage(string? query, int limit, string? startAfter,
            string? groupName = null)
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            var baseUrl = $"{CoreServices.ServerUrl}/auth/players/{Uid}/achievements";
            var parameters = new Dictionary<string, string>
            {
                { "limit", limit.ToString() }
            };
            if (!string.IsNullOrEmpty(query)) parameters["query"] = query;
            if (!string.IsNullOrEmpty(startAfter)) parameters["startAfter"] = startAfter;
            if (!string.IsNullOrEmpty(groupName)) parameters["groupName"] = groupName;
            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var achievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData =
                    JsonConvert.DeserializeObject<GetPlayerAchievementsResponse>(
                        await achievementsResponse.Content.ReadAsStringAsync());
                foreach (var achievement in achievementsResponseData.Achievements) achievement.Player = this;
                return achievementsResponseData.Achievements;
            }
            else
            {
                await CoreServices.HandleErrorResponse(achievementsResponse);
                return Array.Empty<PlayerAchievement>();
            }
        }

        /// <summary>
        /// Gets an array of achievement IDs achieved by the player.
        /// </summary>
        /// <param name="groupName">The group ID to get achievements from.</param>
        /// <returns>An array of achievement IDs achieved.</returns>
        public async Task<ListedPlayerAchievement[]> ListAllPlayerAchievements(string? groupName = null)
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            var baseUrl = $"{CoreServices.ServerUrl}/auth/players/{Uid}/achievements:list";
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(groupName)) parameters["groupName"] = groupName;

            var url = UrlBuilder.New(baseUrl).SetQueryParameters(parameters).ToString();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var listAchievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementsResponse.IsSuccessStatusCode)
            {
                var listAchievementsResponseData =
                    JsonConvert.DeserializeObject<ListPlayerAchievementsResponse>(
                        await listAchievementsResponse.Content.ReadAsStringAsync());
                return listAchievementsResponseData.Achievements;
            }
            else
            {
                await CoreServices.HandleErrorResponse(listAchievementsResponse);
                return Array.Empty<ListedPlayerAchievement>();
            }
        }
    }
}