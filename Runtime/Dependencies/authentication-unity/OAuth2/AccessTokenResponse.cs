using System;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace Cdm.Authentication.OAuth2
{
    [Preserve]
    [DataContract]
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token issued by the authorization server.
        /// </summary>
        [Preserve]
        [DataMember(IsRequired = true, Name = "access_token")]
        public string accessToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token which can be used to obtain a new access token.
        /// </summary>
        [Preserve]
        [DataMember(Name = "refresh_token")]
        public string refreshToken { get; set; }

        /// <summary>
        /// Gets or sets the token type as specified in http://tools.ietf.org/html/rfc6749#section-7.1.
        /// </summary>
        [Preserve]
        [DataMember(IsRequired = true, Name = "token_type")]
        public string tokenType { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in seconds of the access token.
        /// </summary>
        [Preserve]
        [DataMember(Name = "expires_in")]
        public long? expiresIn { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in seconds of the refresh token.
        /// </summary>
        [Preserve]
        [DataMember(Name = "refresh_token_expires_in")]
        public long? refreshTokenExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the scope of the access token as specified in http://tools.ietf.org/html/rfc6749#section-3.3.
        /// </summary>
        [Preserve]
        [DataMember(Name = "scope")]
        public string scope { get; set; }

        /// <summary>
        /// Gets or sets the time at which the <see cref="accessToken"/> was issued.
        /// </summary>
        [Preserve]
        [DataMember(Name = "issued_at")]
        public long? issuedAt { get; set; }

        /// <summary>
        /// Gets or sets the time at which the <see cref="refreshToken"/> was issued.
        /// </summary>
        [Preserve]
        [DataMember(Name = "refresh_token_issued_at")]
        public long? refreshTokenIssuedAt { get; set; }

        /// <summary>
        /// Seconds till the <see cref="accessToken"/> expires returned by provider.
        /// </summary>
        public DateTime? expiresAt
        {
            get
            {
                if (issuedAt.HasValue && expiresIn.HasValue)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(issuedAt.Value).DateTime + TimeSpan.FromSeconds(expiresIn.Value);
                }

                return null;
            }
        }

        /// <summary>
        /// Seconds till the <see cref="refreshToken"/> expires returned by provider.
        /// </summary>
        public DateTime? refreshTokenExpiresAt
        {
            get
            {
                if (refreshTokenIssuedAt.HasValue && refreshTokenExpiresIn.HasValue)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(refreshTokenIssuedAt.Value).DateTime + TimeSpan.FromSeconds(refreshTokenExpiresIn.Value);
                }

                return null;
            }
        }

        public AuthenticationHeaderValue GetAuthenticationHeader()
        {
            return new AuthenticationHeaderValue(tokenType, accessToken);
        }

        /// <summary>
        /// Returns true if the token is expired or it's going to expire soon.
        /// </summary>
        /// <remarks>
        /// If a token response does not have <see cref="accessToken"/> then it's considered expired.
        /// If <see cref="expiresAt"/> is <c>null</c>, the token is also considered expired.
        /// </remarks>
        public bool IsExpired()
        {
            return string.IsNullOrEmpty(accessToken) || expiresAt == null || expiresAt < DateTime.UtcNow;
        }

        /// <summary>
        /// Returns true if the <see cref="refreshToken">refresh token</see> is exist.
        /// </summary>
        public bool HasRefreshToken()
        {
            return !string.IsNullOrEmpty(refreshToken);
        }
    }
}