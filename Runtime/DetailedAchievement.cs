using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class DetailedAchievement : Achievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "detailedDescription")]
        public readonly string DetailedDescription;
    }
}
