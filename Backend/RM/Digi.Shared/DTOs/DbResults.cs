using System;

namespace Digi.Shared.DTOs
{
    /// <summary>
    /// Replacement for DbOperationResult to avoid naming conflicts.
    /// </summary>
    public class DbResults
    {
        public bool IsSuccess { get; set; }
        public int AffectedRows { get; set; }
        public int ReturnCode { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public object Data { get; set; }
    }

    public class DbResults<T> : DbResults
    {
        public new T Data { get; set; }
    }
}

