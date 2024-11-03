using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class Achievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id;

        [Preserve]
        [DataMember(IsRequired = true, Name = "displayName")]
        public readonly string DisplayName;

        [Preserve]
        [DataMember(Name = "cover")]
        public readonly string CoverUrl;

        [Preserve]
        [DataMember(IsRequired = true, Name = "shortDescription")]
        public readonly string ShortDescription;

        [Preserve]
        [DataMember(Name = "progress")]
        public readonly float Progress;

        [Preserve]
        [DataMember(Name = "groupName")]
        public readonly string GroupName;

        [Preserve]
        [DataMember(Name = "groupOrder")]
        public readonly int GroupOrder;

        [Preserve]
        [DataMember(IsRequired = true, Name = "percentageAchieved")]
        public readonly float PercentageAchieved;
    }
}
