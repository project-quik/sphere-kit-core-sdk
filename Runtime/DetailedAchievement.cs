using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a detailed achievement.
    /// </summary>
    [Preserve]
    [DataContract]
    public class DetailedAchievement : Achievement
    {
        /// <summary>
        /// The detailed description of the achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "detailedDescription")]
        public readonly string DetailedDescription = "";
    }
}