using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class AchievementGroup
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id;

        [Preserve]
        [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName;

        [Preserve]
        [DataMember(IsRequired = true, Name = "shortDescription")]
        public readonly string ShortDescription;

        [Preserve]
        [DataMember(IsRequired = true, Name = "progress")]
        public readonly float Progress;

        static readonly HttpClient _httpClient = new();

        public async Task<DetailedAchievementGroup> GetDetailedAchievementGroup()
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/achievements:groups/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementGroupResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementGroupResponse.IsSuccessStatusCode)
            {
                var detailedAchievementGroup = JsonConvert.DeserializeObject<DetailedAchievementGroup>(await detailedAchievementGroupResponse.Content.ReadAsStringAsync());
                return detailedAchievementGroup;
            }
            else
            {
                await CoreServices.HandleErrorResponse(detailedAchievementGroupResponse);
                return null;
            }
        }
    }
}
