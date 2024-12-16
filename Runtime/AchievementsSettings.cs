using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// The settings for the Achievements module.
    /// </summary>
    [Preserve]
    [DataContract]
    public class AchievementsSettings
    {
        /// <summary>
        /// The total number of achievements in the game.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "numAchievements")]
        public readonly int AchievementCount;

        /// <summary>
        /// The URL to the icon for the score unit.
        /// </summary>
        [Preserve] [DataMember(Name = "scoreLogoURL")]
        public readonly string? ScoreLogoUrl;

        /// <summary>
        /// The name of the score unit (e.g. XP, points).
        /// </summary>
        [Preserve] [DataMember(Name = "scoreText")]
        public readonly string? ScoreUnit;

        /// <summary>
        /// Whether the game uses skill-based achievements (true) or story-based achievements (false).
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "isSkillBased")]
        public readonly bool IsSkillBased = true;
    }
}