using System.Runtime.Serialization;
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
    }
}