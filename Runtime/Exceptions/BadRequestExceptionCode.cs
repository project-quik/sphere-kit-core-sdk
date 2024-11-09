using System;

#nullable enable
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
        InvalidQuery = 12,
        AchievementAlreadyAdded = 13,
        SameKeyMultipleOperations = 14,
        InvalidModifyDataType = 15,
        InvalidKeys = 16,
        InvalidUpdateDataType = 17,
        PlayerMetadataLimitReached = 18,
        DivideByZero = 19,
        ValidationFailed = 20,
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
                "auth/same-key-multiple-operations" => BadRequestExceptionCode.SameKeyMultipleOperations,
                "auth/invalid-modify-data-type" => BadRequestExceptionCode.InvalidModifyDataType,
                "auth/invalid-keys" => BadRequestExceptionCode.InvalidKeys,
                "auth/invalid-data-type" => BadRequestExceptionCode.InvalidUpdateDataType,
                "auth/metadata-limit-reached" => BadRequestExceptionCode.PlayerMetadataLimitReached,
                "auth/divide-by-zero" => BadRequestExceptionCode.DivideByZero,
                "achievements/invalid-query" => BadRequestExceptionCode.InvalidQuery,
                "achievements/achievement-already-added" => BadRequestExceptionCode.AchievementAlreadyAdded,
                "FST_VALIDATION_ERROR" => BadRequestExceptionCode.ValidationFailed,
                _ => throw new Exception($"Unknown exception code: {code}"),
            };
        }
    }
}