using System;

#nullable enable
namespace SphereKit
{
    public enum ForbiddenExceptionCode
    {
        InsufficientScope = 1,
    }

    internal static class ForbiddenExceptionCodeExtensions
    {
        internal static ForbiddenExceptionCode GetExceptionCode(string code)
        {
            return code switch
            {
                "auth/insufficient-scope" => ForbiddenExceptionCode.InsufficientScope,
                _ => throw new Exception($"Unknown exception code: {code}"),
            };
        }
    }
}