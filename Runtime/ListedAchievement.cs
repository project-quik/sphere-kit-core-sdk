using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a summary of the player achievement.
    /// </summary>
    [Preserve]
    [DataContract]
    public class ListedAchievement
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
        /// The ID of the group that this achievement belongs to.
        /// </summary>
        [Preserve] [DataMember(Name = "groupName")]
        public readonly string? GroupId;

        /// <summary>
        /// The relative order of this achievement within its group, if the achievement is in a group.
        /// </summary>
        [Preserve] [DataMember(Name = "groupOrder")]
        public readonly int? GroupOrder;
    }
}