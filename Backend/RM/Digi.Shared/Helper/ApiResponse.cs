using System.Net;

namespace Digi.Shared.Helper
{
    public class ApiResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ApiResponse(HttpStatusCode statusCode, string message, object data, List<string> errors = null)
        {
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Errors = errors ?? new List<string>();
        }

        // ✅ Success Response
        public static ApiResponse<T> Success(object data, string message = "Request successful")
        {
            return new ApiResponse<T>(HttpStatusCode.OK, message, data);
        }

        // ✅ Error Response
        public static ApiResponse<T> Fail(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string> errors = null)
        {
            return new ApiResponse<T>(statusCode, message, default, errors ?? new List<string>());
        }
    }


    public class PagedResultGetAttendance<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
