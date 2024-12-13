using System;

#nullable enable
namespace SphereKit
{
    public enum NotFoundExceptionCode
    {
        PlayerNotFound = 1,
        AchievementGroupNotFound = 2,
        StartNotFound = 3,
        AchievementNotFound = 4,
        AchievementNotAdded = 5,
        PlayerAchievementNotFound = 6,
        KeyNotFound = 7,
        DatabaseNotFound = 8,
        DocumentNotFound = 9
    }

    internal static class NotFoundExceptionCodeExtensions
    {
        internal static NotFoundExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/player-not-found" => NotFoundExceptionCode.PlayerNotFound,
                "auth/keys-not-found" => NotFoundExceptionCode.KeyNotFound,
                "achievements/achievement-group-not-found" => NotFoundExceptionCode.AchievementGroupNotFound,
                "achievements/start-not-found" => NotFoundExceptionCode.StartNotFound,
                "achievements/achievement-not-found" => NotFoundExceptionCode.AchievementNotFound,
                "achievements/achievement-not-added" => NotFoundExceptionCode.AchievementNotAdded,
                "achievements/player-achievement-not-found" => NotFoundExceptionCode.PlayerAchievementNotFound,
                "databases/database-not-found" => NotFoundExceptionCode.DatabaseNotFound,
                "databases/document-not-found" => NotFoundExceptionCode.DocumentNotFound,
                "databases/no-such-key" => NotFoundExceptionCode.KeyNotFound,
                "databases/invalid-dbref" => NotFoundExceptionCode.DatabaseNotFound,
                _ => throw new Exception($"Unknown exception code: {code}")
            };
        }
    }
}