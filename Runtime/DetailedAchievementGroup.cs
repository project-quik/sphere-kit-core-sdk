using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a detailed achievement group.
    /// </summary>
    [Preserve]
    [DataContract]
    public class DetailedAchievementGroup
    {
        [Preserve] [DataMember(IsRequired = true, Name = "detailedDescription")]
        public readonly string DetailedDescription = "";
    }
}