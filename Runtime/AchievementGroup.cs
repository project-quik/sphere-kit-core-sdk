using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class AchievementGroup
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id;

        [Preserve]
        [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName;

        [Preserve]
        [DataMember(IsRequired = true, Name = "shortDescription")]
        public readonly string ShortDescription;

        [Preserve]
        [DataMember(IsRequired = true, Name = "progress")]
        public readonly float Progress;
    }
}
