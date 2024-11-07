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
    [Preserve]
    [DataContract]
    public class PlayerAchievement: Achievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "timestamp")]
        long _achievedTimeMillis
        {
            set
            {
                AchievedDate = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
            }
        }
        public DateTime AchievedDate { get; private set; } = new DateTime();

        internal Player Player { get; set; } = new Player();

        static readonly HttpClient _httpClient = new();

        public new async Task<DetailedPlayerAchievement> GetDetailedAchievement()
        {
            Assert.IsNotNull(Player, "Player is not set for this player achievement.");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/auth/players/{Player.Uid}/achievements/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementResponse.IsSuccessStatusCode)
            {
                var detailedAchievement = JsonConvert.DeserializeObject<DetailedPlayerAchievement>(await detailedAchievementResponse.Content.ReadAsStringAsync())!;
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
