using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int ErrorCode { get; set; } // Add this property
        public int AffectedRows { get; set; } // Add this if you need row count
    }
    public class DataAccessResult
    {
        public bool Success { get; set; }
        public int AffectedRows { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
    public class DbOperationResult
    {
        public bool IsSuccess { get; set; }
        public int AffectedRows { get; set; }
        public int ReturnCode { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public dynamic Data { get; set; }

        public static DbOperationResult Success(string message = "", int affectedRows = 0, int returnCode = 0, dynamic data = null)
        {
            return new DbOperationResult
            {
                IsSuccess = true,
                Message = message,
                AffectedRows = affectedRows,
                ReturnCode = returnCode,
                Data = data
            };
        }

        public static DbOperationResult Fail(string message, int returnCode = -1, Exception exception = null)
        {
            return new DbOperationResult
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = returnCode,
                Exception = exception
            };
        }
    }

    public class DbOperationResult<T> : DbOperationResult
    {
        public new T Data { get; set; }

        public static DbOperationResult<T> Success(T data, string message = "", int affectedRows = 0, int returnCode = 0)
        {
            return new DbOperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                AffectedRows = affectedRows,
                ReturnCode = returnCode
            };
        }

        public static DbOperationResult<T> Fail(string message, int returnCode = -1, Exception exception = null)
        {
            return new DbOperationResult<T>
            {
                IsSuccess = false,
                Message = message,
                ReturnCode = returnCode,
                Exception = exception
            };
        }

    }
   public class EmailAttachment
    {
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
