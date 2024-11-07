using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct GetAchievementsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievements")]
        public readonly Achievement[] Achievements;
    }
}