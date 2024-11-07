using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal class GetPlayerAchievementsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievements")]
        public readonly PlayerAchievement[] Achievements = {};
    }
}