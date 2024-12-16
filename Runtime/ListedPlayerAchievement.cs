using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a summary of the player achievement.
    /// </summary>
    [Preserve]
    [DataContract]
    public class ListedPlayerAchievement
    {
        /// <summary>
        /// The ID of the achievement.
        /// </summary>
        [Preserve] [DataMember(IsRequired = true, Name = "name")]
        public readonly string Id = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "timestamp")]
        private long _achievedTimeMillis
        {
            set => AchievedDate = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
        }

        /// <summary>
        /// The date and time the achievement was achieved.
        /// </summary>
        public DateTime AchievedDate { get; private set; }
    }
}