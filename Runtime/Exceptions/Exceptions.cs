using System;

namespace SphereKit
{
    public class InternalServerException : Exception
    {
        public InternalServerException(string message) : base(message) { }
    }

    public class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundExceptionCode Code { get; private set; }

        public NotFoundException(string code, string message) : base($"({code}) {message}")
        {
            Code = NotFoundExceptionCodeExtensions.GetExceptionCode(code);
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenExceptionCode Code { get; private set; }

        public ForbiddenException(string code, string message) : base($"({code}) {message}")
        {
            Code = ForbiddenExceptionCodeExtensions.GetExceptionCode(code);
        }
    }

    public class AuthenticationException : Exception
    {
        public AuthenticationExceptionCode Code { get; private set; }

        public AuthenticationException(string code, string message) : base($"({code}) {message}") {
            Code = AuthenticationExceptionCodeExtensions.GetExceptionCode(code);
        }
    }

    public class BadRequestException : Exception
    {
        public string Code { get; private set; }

        public BadRequestException(string code, string message) : base($"({code}) {message}")
        {
            Code = code;
        }
    }
}
