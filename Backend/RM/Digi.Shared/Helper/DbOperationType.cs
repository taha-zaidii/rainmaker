using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public enum DbOperationType
    {
        Insert = 1,
        Update = 2,
        Delete = 3,
        HardDelete = 4,
        Select = 5,
        SelectById = 6,
        SetStatus = 7,
        BulkInsert = 8,
        BulkUpdate = 9,
        BulkDelete = 10,
        Restore = 11,
        Activate = 12,
        Deactivate = 13,
        Approve = 14,
        Reject = 15,
        Archive = 16,
        UnArchive = 17,
        Export = 18,
        Import = 19,
        Login = 20,
        Logout = 21,
        AuditTrail = 22,
        LogError = 23,
        Sync = 24,
        Validate = 25,
        DuplicateCheck = 26,
        Count = 27
    }

}
