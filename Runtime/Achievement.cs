using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class Achievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName = "";

        [Preserve]
        [DataMember(Name = "coverURL")]
        public readonly string? CoverUrl;

        [Preserve]
        [DataMember(IsRequired = true, Name = "shortDescription")]
        public readonly string ShortDescription = "";

        [Preserve]
        [DataMember(Name = "progress")]
        public readonly float? Progress;

        [Preserve]
        [DataMember(Name = "groupName")]
        public readonly string? GroupId;

        [Preserve]
        [DataMember(Name = "groupOrder")]
        public readonly int? GroupOrder;

        [Preserve]
        [DataMember(IsRequired = true, Name = "percentageAchieved")]
        public readonly float PercentageAchieved = 0f;

        static readonly HttpClient _httpClient = new();

        public async Task<DetailedAchievement> GetDetailedAchievement()
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/achievements/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementResponse.IsSuccessStatusCode)
            {
                var detailedAchievement = JsonConvert.DeserializeObject<DetailedAchievement>(await detailedAchievementResponse.Content.ReadAsStringAsync())!;
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
