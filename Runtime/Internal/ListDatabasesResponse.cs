using System.Runtime.Serialization;
using UnityEngine.Scripting;

#nullable enable
namespace SphereKit
{
    [Preserve]
    [DataContract]
    internal readonly struct ListDatabasesResponse
    {
        [Preserve]
        [DataMember(IsRequired = true, Name = "databases")]
        public readonly string[] Databases;
    }
}