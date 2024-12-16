using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    /// <summary>
    /// Represents a detailed player achievement.
    /// </summary>
    [Preserve]
    [DataContract]
    public class DetailedPlayerAchievement : DetailedAchievement
    {
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