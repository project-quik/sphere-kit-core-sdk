using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal class ErrorResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "code")]
        public string ErrorCode { get; set; }

        [Preserve]
        [DataMember(IsRequired = true, Name = "message")]
        public string ErrorMessage { get; set; }

        [Preserve]
        [DataMember(IsRequired = true, Name = "statusCode")]
        public int StatusCode { get; set; }
    }
}