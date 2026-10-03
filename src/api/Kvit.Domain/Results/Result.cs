using System.Net;

namespace Kvit.Domain.Results
{
    public class Result
    {
        private protected Result(bool isSuccess, HttpStatusCode statusCode, string error, string errorCode)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Error = error;
            ErrorCode = errorCode;
        }

        public bool IsSuccess { get; }

        public HttpStatusCode StatusCode { get; }

        public string Error { get; }

        public string ErrorCode { get; }

        public static Result Ok() => new(true, HttpStatusCode.OK, string.Empty, string.Empty);

        public static Result<T> Ok<T>(T value) => new(value);

        public static Result Failure(string error, string errorCode) => new(false, HttpStatusCode.BadRequest, error, errorCode);

        public static Result<T> Failure<T>(string error, string errorCode) => new(HttpStatusCode.BadRequest, error, errorCode);

        public static Result Unauthorized(string error, string errorCode) => new(false, HttpStatusCode.Unauthorized, error, errorCode);

        public static Result<T> Unauthorized<T>(string error, string errorCode) => new(HttpStatusCode.Unauthorized, error, errorCode);

        public static Result Forbid(string error, string errorCode) => new(false, HttpStatusCode.Forbidden, error, errorCode);

        public static Result<T> Forbid<T>(string error, string errorCode) => new(HttpStatusCode.Forbidden, error, errorCode);

        public static Result NotFound(string error, string errorCode) => new(false, HttpStatusCode.NotFound, error, errorCode);

        public static Result<T> NotFound<T>(string error, string errorCode) => new(HttpStatusCode.NotFound, error, errorCode);

        public Result<T> ToFailure<T>()
        {
            if (IsSuccess)
            {
                throw new InvalidOperationException($"A successful Result cannot be turned into a failed Result<{typeof(T).Name}>. Check IsSuccess first.");
            }

            return new Result<T>(StatusCode, Error, ErrorCode);
        }
    }
}
