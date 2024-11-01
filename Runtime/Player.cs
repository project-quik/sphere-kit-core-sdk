using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    public struct Player
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "uid")]
        public readonly string Uid;

        [Preserve]
        [DataMember(IsRequired = true, Name = "name")]
        public readonly string UserName;

        [Preserve]
        [DataMember(IsRequired = true, Name = "realName")]
        public readonly string DisplayName;

        [Preserve]
        [DataMember(Name = "profilepicURL")]
        public readonly string ProfilePicUrl;

        [Preserve]
        [DataMember(Name = "email")]
        public readonly string Email;

        [Preserve]
        [DataMember(IsRequired = true, Name = "joinDate")]
        string _joinDateStr
        {
            set
            {
                JoinDate = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }
        public DateTime JoinDate { get; private set; }

        [Preserve]
        [DataMember(Name = "level")]
        public readonly int Level;

        [Preserve]
        [DataMember(Name = "score")]
        public readonly long Score;

        [Preserve]
        [DataMember(IsRequired = true, Name = "metadata")]
        public readonly Dictionary<string, string> Metadata;

        [Preserve]
        [DataMember(IsRequired = true, Name = "isBanned")]
        public readonly bool IsBanned;

        [Preserve]
        [DataMember(Name = "banStartTime")]
        long _banStartTimeMillis
        {
            set
            {
                BanStartTime = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
            }
        }
        public DateTime? BanStartTime { get; private set; }

        [Preserve]
        [DataMember(Name = "banDuration")]
        long _banDurationHours
        {
            set
            {
                BanDuration = TimeSpan.FromHours(value);
            }
        }
        public TimeSpan? BanDuration { get; private set; }

        [Preserve]
        [DataMember(Name = "banReason")]
        public readonly string BanReason;
    }
}
