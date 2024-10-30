using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace Cdm.Authentication.OAuth2
{
    [Preserve]
    [DataContract]
    public class AccessTokenUserInfo
    {
        /// <summary>
        /// Gets or sets the uid issued by the authorization server.
        /// </summary>
        [Preserve]
        [DataMember(IsRequired = true, Name = "uid")]
        public string uid { get; set; }
    }
}