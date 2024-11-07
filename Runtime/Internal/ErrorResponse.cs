using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal class ErrorResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "code")]
        public readonly string ErrorCode = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "message")]
        public readonly string ErrorMessage = "";

        [Preserve]
        [DataMember(IsRequired = true, Name = "statusCode")]
        public readonly int StatusCode = 400;
    }
}