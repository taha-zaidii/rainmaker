using Digi.Shared.DTOs;
using System.Net;

namespace Digi.Shared.Helper
{

    // Updated DbOperationResultHelpers.cs
    public static class DbOperationResultHelpers
    {
        public static DbOperationResult<T> Success<T>(T data, string message = "")
        {
            return new DbOperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public static DbOperationResult<T> Failure<T>(string message, int? returnCode = null, Exception exception = null)
        {
            return new DbOperationResult<T>
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = returnCode.HasValue ? returnCode.Value : -1,
                Exception = exception
            };
        }

        public static DbOperationResult Success(string message = "")
        {
            return new DbOperationResult
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static DbOperationResult Failure(string message, int? returnCode = null, Exception exception = null)
        {
            return new DbOperationResult
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = (int)returnCode,  // No .Value access here
                Exception = exception
            };
        }
    }

    public class OperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string Message { get; }
        public HttpStatusCode StatusCode { get; }

        private OperationResult(bool isSuccess, T data, string message, HttpStatusCode statusCode)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            StatusCode = statusCode;
        }

        public static OperationResult<T> Success(T data, string message = "")
            => new OperationResult<T>(true, data, message, HttpStatusCode.OK);

        public static OperationResult<T> Failure(string message, HttpStatusCode statusCode)
            => new OperationResult<T>(false, default, message, statusCode);
    }

    /// <summary>
    /// Helpers for DbResults (new name requested)
    /// </summary>
    public static class DbResultsHelpers
    {
        public static DbResults<T> Success<T>(T data, string message = "")
        {
            return new DbResults<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public static DbResults<T> Failure<T>(string message, int? returnCode = null, Exception exception = null)
        {
            return new DbResults<T>
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = returnCode ?? -1,
                Exception = exception
            };
        }

        public static DbResults Success(string message = "")
        {
            return new DbResults
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static DbResults Failure(string message, int? returnCode = null, Exception exception = null)
        {
            return new DbResults
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = returnCode ?? -1,
                Exception = exception
            };
        }
    }
}
