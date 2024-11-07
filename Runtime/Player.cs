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
    [Preserve]
    [DataContract]
    public class Player
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "uid")]
        public readonly string Uid = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string UserName = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "realName")]
        public readonly string DisplayName = "";

        [Preserve]
        [DataMember(Name = "profilepicURL")]
        public readonly string? ProfilePicUrl;

        [Preserve]
        [DataMember(Name = "email")]
        public readonly string? Email;

        [Preserve]
        [DataMember(IsRequired = true, Name = "joinDate")]
        string _joinDateStr
        {
            set
            {
                JoinDate = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }
        public DateTime? JoinDate { get; private set; }

        [Preserve]
        [DataMember(Name = "level")]
        public readonly int? Level;

        [Preserve]
        [DataMember(Name = "score")]
        public readonly long? Score;

        [Preserve]
        [DataMember(IsRequired = true, Name = "metadata")]
        public readonly Dictionary<string, string> Metadata = new();

        [Preserve]
        [DataMember(IsRequired = true, Name = "isBanned")]
        public readonly bool IsBanned = false;

        [Preserve]
        [DataMember(Name = "banStartTime")]
        long _banStartTimeMillis
        {
            set
            {
                BanStartTime = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
            }
        }
        public DateTime? BanStartTime { get; private set; }

        [Preserve]
        [DataMember(Name = "banDuration")]
        long _banDurationHours
        {
            set
            {
                BanDuration = TimeSpan.FromHours(value);
            }
        }
        public TimeSpan? BanDuration { get; private set; }

        [Preserve]
        [DataMember(Name = "banReason")]
        public readonly string? BanReason;

        static readonly HttpClient _httpClient = new();

        public PlayerAchievementsCursor GetPlayerAchievements(string? query = null, int pageSize = 30)
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            string? currentStartAfter = null;
            bool reachedEnd = false;

            return new PlayerAchievementsCursor(async () =>
            {
                if (reachedEnd) return new PlayerAchievement[0];

                var achievements = await GetPlayerAchievementsPage(query, pageSize, currentStartAfter);
                if (achievements.Length < pageSize || achievements.Length == 0)
                {
                    reachedEnd = true;
                }
                currentStartAfter = achievements.LastOrDefault()?.Id;
                return achievements;
            });
        }

        async Task<PlayerAchievement[]> GetPlayerAchievementsPage(string? query, int limit, string? startAfter)
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            var baseUrl = $"{CoreServices.ServerUrl}/auth/players/{Uid}/achievements";
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
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var achievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (achievementsResponse.IsSuccessStatusCode)
            {
                var achievementsResponseData = JsonConvert.DeserializeObject<GetPlayerAchievementsResponse>(await achievementsResponse.Content.ReadAsStringAsync())!;
                foreach (var achievement in achievementsResponseData.Achievements)
                {
                    achievement.Player = this;   
                }
                return achievementsResponseData.Achievements;
            }
            else
            {
                await CoreServices.HandleErrorResponse(achievementsResponse);
                return new PlayerAchievement[0];
            }
        }

        public async Task<ListedPlayerAchievement[]> ListPlayerAchievements()
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/auth/players/{Uid}/achievements:list");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var listAchievementsResponse = await _httpClient.SendAsync(requestMessage);
            if (listAchievementsResponse.IsSuccessStatusCode)
            {
                var listAchievementsResponseData = JsonConvert.DeserializeObject<ListPlayerAchievementsResponse>(await listAchievementsResponse.Content.ReadAsStringAsync())!;
                return listAchievementsResponseData.Achievements;
            }
            else
            {
                await CoreServices.HandleErrorResponse(listAchievementsResponse);
                return new ListedPlayerAchievement[0];
            }
        }
    }
}
