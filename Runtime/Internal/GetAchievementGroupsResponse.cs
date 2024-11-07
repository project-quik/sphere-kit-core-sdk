using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct GetAchievementGroupsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievementGroups")]
        public readonly AchievementGroup[] AchievementGroups;
    }
}