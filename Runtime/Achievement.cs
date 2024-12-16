using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a short achievement.
    /// Use <see cref="GetDetailedAchievement"/> to get a detailed achievement with achievement description.
    /// </summary>
    [Preserve]
    [DataContract]
    public class Achievement
    {
        /// <summary>
        /// The ID of the achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id = "";

        /// <summary>
        /// The display name of the achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName = "";

        /// <summary>
        /// The URL to the cover image of the achievement. Expires in 15 min from time of response.
        /// </summary>
        [Preserve] [DataMember(Name = "coverURL")]
        public readonly string? CoverUrl;

        /// <summary>
        /// The short description of the achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "shortDescription")]
        public readonly string ShortDescription = "";

        /// <summary>
        /// The story progress that this achievement represents. Only available for story-based games.
        /// </summary>
        [Preserve] [DataMember(Name = "progress")]
        public readonly float? Progress;

        /// <summary>
        /// The ID of the group that this achievement belongs to.
        /// </summary>
        [Preserve] [DataMember(Name = "groupName")]
        public readonly string? GroupId;

        /// <summary>
        /// The relative order of this achievement within its group, if the achievement is in a group.
        /// </summary>
        [Preserve] [DataMember(Name = "groupOrder")]
        public readonly int? GroupOrder;

        /// <summary>
        /// The percentage of players who have achieved this achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "percentageAchieved")]
        public readonly float PercentageAchieved;

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Returns the detailed achievement with achievement description.
        /// </summary>
        /// <returns>The detailed achievement.</returns>
        public async Task<DetailedAchievement> GetDetailedAchievement()
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            using var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/achievements/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementResponse.IsSuccessStatusCode)
            {
                var detailedAchievement =
                    JsonConvert.DeserializeObject<DetailedAchievement>(await detailedAchievementResponse.Content
                        .ReadAsStringAsync())!;
                return detailedAchievement;
            }
            else
            {
                await CoreServices.HandleErrorResponse(detailedAchievementResponse);
                return new DetailedAchievement();
            }
        }
    }
}