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
        /// The total number of achievement groups in the game.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "numAchievementGroups")]
        public readonly int AchievementGroupCount;
    }
}