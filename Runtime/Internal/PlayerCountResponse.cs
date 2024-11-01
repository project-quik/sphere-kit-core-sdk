using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal struct PlayerCountResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "playerCount")]
        public readonly long PlayerCount;
    }
}
