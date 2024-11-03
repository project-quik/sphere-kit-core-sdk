using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal class GetAchievementsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievements")]
        public readonly Achievement[] Achievements;
    }
}