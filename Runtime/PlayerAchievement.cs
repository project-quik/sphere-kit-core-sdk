using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents an achievement achieved by a player.
    /// </summary>
    [Preserve]
    [DataContract]
    public class PlayerAchievement : Achievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "timestamp")]
        private long _achievedTimeMillis
        {
            set => AchievedDate = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
        }

        /// <summary>
        /// The date and time the achievement was achieved.
        /// </summary>
        public DateTime AchievedDate { get; private set; }

        /// <summary>
        /// The player who achieved the achievement.
        /// </summary>
        internal Player Player { get; set; } = new();

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Returns the detailed achievement with achievement description.
        /// </summary>
        /// <returns>The detailed player achievement.</returns>
        public new async Task<DetailedPlayerAchievement> GetDetailedAchievement()
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            Assert.IsNotNull(Player, "Player is not set for this player achievement.");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{CoreServices.ServerUrl}/auth/players/{Player.Uid}/achievements/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementResponse.IsSuccessStatusCode)
            {
                var detailedAchievement =
                    JsonConvert.DeserializeObject<DetailedPlayerAchievement>(await detailedAchievementResponse.Content
                        .ReadAsStringAsync())!;
                return detailedAchievement;
            }
            else
            {
                await CoreServices.HandleErrorResponse(detailedAchievementResponse);
                return new DetailedPlayerAchievement();
            }
        }
    }
}