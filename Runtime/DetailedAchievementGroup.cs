using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class DetailedAchievementGroup
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "detailedDescription")]
        public readonly string DetailedDescription = "";
    }
}
