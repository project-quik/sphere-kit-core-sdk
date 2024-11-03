using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal struct GetPlayerCountResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "playerCount")]
        public readonly long PlayerCount;
    }
}
