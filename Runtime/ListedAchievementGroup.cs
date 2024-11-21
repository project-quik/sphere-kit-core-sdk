using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class ListedAchievementGroup
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "progress")]
        public readonly float Progress;
    }
}
