using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a summary of the achievement group.
    /// </summary>
    [Preserve]
    [DataContract]
    public class ListedAchievementGroup
    {
        /// <summary>
        /// The ID of the achievement group.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id = "";

        /// <summary>
        /// The display name of the achievement group.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName = "";

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Returns the detailed achievement group with group description.
        /// </summary>
        public async Task<DetailedAchievementGroup> GetDetailedAchievementGroup()
        {
            CoreServices.CheckInitialized();
            CoreServices.CheckSignedIn();

            using var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"{CoreServices.ServerUrl}/achievements:groups/{Id}");
            requestMessage.Headers.Add("Authorization", $"Bearer {CoreServices.AccessToken}");
            var detailedAchievementGroupResponse = await _httpClient.SendAsync(requestMessage);
            if (detailedAchievementGroupResponse.IsSuccessStatusCode)
            {
                var detailedAchievementGroup =
                    JsonConvert.DeserializeObject<DetailedAchievementGroup>(await detailedAchievementGroupResponse
                        .Content.ReadAsStringAsync())!;
                return detailedAchievementGroup;
            }
            else
            {
                await CoreServices.HandleErrorResponse(detailedAchievementGroupResponse);
                return new DetailedAchievementGroup();
            }
        }
    }
}