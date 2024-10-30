using System;

namespace SphereKit
{
    public enum BadRequestExceptionCode
    {
        Banned = 1,
        MissingClientID = 2,
        MissingAuthorizationCode = 3,
        MissingCodeVerifier = 4,
        InvalidGrantType = 5,
        MissingRefreshToken = 6,
        InvalidClientID = 7,
        InvalidAuthorizationCode = 8,
        InvalidCodeVerifier = 9,
        MismatchClientIDAuthorizationCode = 10,
        MismatchClientIDRefreshToken = 11,
        ValidationFailed = 12,
    }

    internal static class BadRequestExceptionCodeExtensions
    {
        internal static BadRequestExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/banned" => BadRequestExceptionCode.Banned,
                "auth/missing-client-id" => BadRequestExceptionCode.MissingClientID,
                "auth/missing-code" => BadRequestExceptionCode.MissingAuthorizationCode,
                "auth/missing-code-verifier" => BadRequestExceptionCode.MissingCodeVerifier,
                "auth/invalid-grant-type" => BadRequestExceptionCode.InvalidGrantType,
                "auth/missing-refresh-token" => BadRequestExceptionCode.MissingRefreshToken,
                "auth/invalid-client-id" => BadRequestExceptionCode.InvalidClientID,
                "auth/invalid-code" => BadRequestExceptionCode.InvalidAuthorizationCode,
                "auth/invalid-code-verifier" => BadRequestExceptionCode.InvalidCodeVerifier,
                "auth/mismatch-client-id-authcode" => BadRequestExceptionCode.MismatchClientIDAuthorizationCode,
                "auth/mismatch-client-id-refresh" => BadRequestExceptionCode.MismatchClientIDRefreshToken,
                "FST_VALIDATION_ERROR" => BadRequestExceptionCode.ValidationFailed,
                _ => throw new Exception($"Unknown exception code: {code}"),
            };
        }
    }
}