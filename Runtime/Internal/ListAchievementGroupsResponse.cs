using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct ListAchievementGroupsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievementGroups")]
        public readonly ListedAchievementGroup[] AchievementGroups;
    }
}
