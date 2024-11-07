using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct ListPlayerAchievementsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievements")]
        public readonly ListedPlayerAchievement[] Achievements;
    }
}
