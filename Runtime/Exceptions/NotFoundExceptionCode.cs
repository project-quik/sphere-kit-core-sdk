using System;

namespace SphereKit
{
    public enum NotFoundExceptionCode
    {
        PlayerNotFound = 1,
    }

    internal static class NotFoundExceptionCodeExtensions
    {
        internal static NotFoundExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/player-not-found" => NotFoundExceptionCode.PlayerNotFound,
                _ => throw new Exception($"Unknown exception code: {code}"),
            };
        }
    }
}