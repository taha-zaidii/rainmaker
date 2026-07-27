using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public static class DbOperationTypeExtensions
    {
        public static string ToProcedureName(this DbOperationType type)
        {
            return type switch
            {
                DbOperationType.Insert => "Insert",
                DbOperationType.Update => "Update",
                DbOperationType.Delete => "Delete",
                DbOperationType.HardDelete => "HardDelete",
                DbOperationType.Select => "Select",
                DbOperationType.SelectById => "SelectById",
                DbOperationType.SetStatus => "SetStatus",
                DbOperationType.BulkInsert => "BulkInsert",
                DbOperationType.BulkUpdate => "BulkUpdate",
                DbOperationType.BulkDelete => "BulkDelete",
                DbOperationType.Restore => "Restore",
                DbOperationType.Activate => "Activate",
                DbOperationType.Deactivate => "Deactivate",
                DbOperationType.Approve => "Approve",
                DbOperationType.Reject => "Reject",
                DbOperationType.Archive => "Archive",
                DbOperationType.UnArchive => "UnArchive",
                DbOperationType.Export => "Export",
                DbOperationType.Import => "Import",
                DbOperationType.Login => "Login",
                DbOperationType.Logout => "Logout",
                DbOperationType.AuditTrail => "GetAuditTrail",
                DbOperationType.LogError => "LogError",
                DbOperationType.Sync => "SyncData",
                DbOperationType.Validate => "ValidateData",
                DbOperationType.DuplicateCheck => "CheckDuplicate",
                DbOperationType.Count => "CountRecords",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

}
