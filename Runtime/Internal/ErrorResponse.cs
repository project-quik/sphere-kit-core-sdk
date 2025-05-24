using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct ErrorResponse
    {
        [Preserve] [DataMember(IsRequired = true, Name = "code")]
        public readonly string ErrorCode;

        [Preserve] [DataMember(Name = "message")]
        public readonly string? ErrorMessage;

        [Preserve] [DataMember(IsRequired = true, Name = "statusCode")]
        public readonly int StatusCode;
    }
}