using System.Net;

namespace Kvit.Domain.Results
{
    public sealed class Result<T> : Result
    {
        private readonly T _value;

        internal Result(T value)
            : base(true, HttpStatusCode.OK, string.Empty, string.Empty)
        {
            _value = value;
        }

        internal Result(HttpStatusCode statusCode, string error, string errorCode)
            : base(false, statusCode, error, errorCode)
        {
            _value = default!;
        }

        public T Value => IsSuccess
            ? _value
            : throw new InvalidOperationException($"Value was read from a failed Result<{typeof(T).Name}> (status {(int)StatusCode}, error code '{ErrorCode}'). Check IsSuccess first.");
    }
}
