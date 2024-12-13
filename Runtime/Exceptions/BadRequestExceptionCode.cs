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
        InvalidUpdateDataType = 17,
        PlayerMetadataLimitReached = 18,
        InvalidPath = 20,
        QueryNotAllowed = 21,
        InvalidProjectionKey = 22,
        InvalidProjectionValue = 23,
        MixedProjectionValues = 24,
        InvalidSort = 25,
        InvalidDocumentData = 26,
        InvalidField = 27,
        MissingSort = 29,
        SortKeysMismatch = 30,
        GraphContainsCycle = 31,
        TypeMismatch = 32,
        IllegalOperation = 33,
        EmptyArrayOperation = 34,
        CannotBackfillArray = 35,
        RemoteChangeDetected = 36,

        ValidationFailed = 1000
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
                "auth/invalid-modify-data-type" => BadRequestExceptionCode.InvalidUpdateDataType,
                "auth/invalid-keys" => BadRequestExceptionCode.InvalidField,
                "auth/invalid-data-type" => BadRequestExceptionCode.InvalidUpdateDataType,
                "auth/metadata-limit-reached" => BadRequestExceptionCode.PlayerMetadataLimitReached,
                "achievements/invalid-query" => BadRequestExceptionCode.InvalidQuery,
                "achievements/achievement-already-added" => BadRequestExceptionCode.AchievementAlreadyAdded,
                "databases/invalid-path" => BadRequestExceptionCode.InvalidPath,
                "databases/query-not-allowed" => BadRequestExceptionCode.QueryNotAllowed,
                "databases/invalid-projection-key" => BadRequestExceptionCode.InvalidProjectionKey,
                "databases/invalid-projection-value" => BadRequestExceptionCode.InvalidProjectionValue,
                "databases/mixed-projection-values" => BadRequestExceptionCode.MixedProjectionValues,
                "databases/same-key-multiple-operations" => BadRequestExceptionCode.SameKeyMultipleOperations,
                "databases/invalid-update-value-type" => BadRequestExceptionCode.InvalidUpdateDataType,
                "databases/invalid-sort" => BadRequestExceptionCode.InvalidSort,
                "databases/invalid-data" => BadRequestExceptionCode.InvalidDocumentData,
                "databases/invalid-update-key" => BadRequestExceptionCode.InvalidField,
                "databases/invalid-rename-key" => BadRequestExceptionCode.InvalidField,
                "databases/missing-sort" => BadRequestExceptionCode.MissingSort,
                "databases/sort-keys-mismatch" => BadRequestExceptionCode.SortKeysMismatch,
                "databases/graph-contains-cycle" => BadRequestExceptionCode.GraphContainsCycle,
                "databases/type-mismatch" => BadRequestExceptionCode.TypeMismatch,
                "databases/illegal-operation" => BadRequestExceptionCode.IllegalOperation,
                "databases/empty-array-operation" => BadRequestExceptionCode.EmptyArrayOperation,
                "databases/path-not-viable" => BadRequestExceptionCode.InvalidField,
                "databases/non-existent-path" => BadRequestExceptionCode.InvalidField,
                "databases/cannot-backfill-array" => BadRequestExceptionCode.CannotBackfillArray,
                "databases/remote-change-detected" => BadRequestExceptionCode.RemoteChangeDetected,
                "databases/conflicting-update-operators" => BadRequestExceptionCode.SameKeyMultipleOperations,
                "databases/empty-field-name" => BadRequestExceptionCode.InvalidField,
                "databases/dotted-field-name" => BadRequestExceptionCode.InvalidField,
                "FST_VALIDATION_ERROR" => BadRequestExceptionCode.ValidationFailed,
                _ => throw new Exception($"Unknown exception code: {code}")
            };
        }
    }
}