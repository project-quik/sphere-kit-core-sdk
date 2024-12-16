using System;

#nullable enable
namespace SphereKit
{
    public enum AuthenticationExceptionCode
    {
        NotSignedIn = 0,
        ExpiredAccessToken = 1,
        InvalidAccessToken = 2,
        InvalidCodeVerifier = 3,
        InvalidRefreshToken = 4
    }

    internal static class AuthenticationExceptionCodeExtensions
    {
        internal static AuthenticationExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/not-signed-in" => AuthenticationExceptionCode.NotSignedIn,
                "auth/expired-access-token" => AuthenticationExceptionCode.ExpiredAccessToken,
                "auth/invalid-access-token" => AuthenticationExceptionCode.InvalidAccessToken,
                "auth/invalid-code-verifier" => AuthenticationExceptionCode.InvalidCodeVerifier,
                "auth/invalid-refresh-token" => AuthenticationExceptionCode.InvalidRefreshToken,
                _ => throw new Exception($"Unknown exception code: {code}")
            };
        }
    }
}