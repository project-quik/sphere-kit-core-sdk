using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct ListAchievementsResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "achievements")]
        public readonly string[] AchievementIDs;
    }
}