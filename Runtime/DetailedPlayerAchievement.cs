using System;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public class DetailedPlayerAchievement : DetailedAchievement
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "timestamp")]
        long _achievedTimeMillis
        {
            set
            {
                AchievedDate = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
            }
        }
        public DateTime AchievedDate { get; private set; }
    }
}
