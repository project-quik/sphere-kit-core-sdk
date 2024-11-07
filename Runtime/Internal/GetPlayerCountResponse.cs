using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct GetPlayerCountResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "playerCount")]
        public readonly long PlayerCount;
    }
}
