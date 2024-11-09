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
        KeysNotFound = 7,
    }

    internal static class NotFoundExceptionCodeExtensions
    {
        internal static NotFoundExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/player-not-found" => NotFoundExceptionCode.PlayerNotFound,
                "auth/keys-not-found" => NotFoundExceptionCode.KeysNotFound,
                "achievements/achievement-group-not-found" => NotFoundExceptionCode.AchievementGroupNotFound,
                "achievements/start-not-found" => NotFoundExceptionCode.StartNotFound,
                "achievements/achievement-not-found" => NotFoundExceptionCode.AchievementNotFound,
                "achievements/achievement-not-added" => NotFoundExceptionCode.AchievementNotAdded,
                "achievements/player-achievement-not-found" => NotFoundExceptionCode.PlayerAchievementNotFound,
                _ => throw new Exception($"Unknown exception code: {code}"),
            };
        }
    }
}