using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class SphereKitSettings
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "numAchievements")]
        public readonly int AchievementCount;

        [Preserve]
        [DataMember(Name = "scoreLogoURL")]
        public readonly string? ScoreLogoUrl;

        [Preserve]
        [DataMember(Name = "scoreText")]
        public readonly string? ScoreUnit;

        [Preserve]
        [DataMember(IsRequired = true, Name = "isSkillBased")]
        public readonly bool IsSkillBased = true;
    }
}
