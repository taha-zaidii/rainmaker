using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public static class GlobalResponseMessages
    {
        public static string Requried(object id) => $"{id} is required.";
        public static string Status(string actionName, bool active) => $"{actionName} status updated to {(active ? "active" : "inActive")}.";
        public static string Delete(string actionName, bool isDelete) => $"{actionName} status updated to {(isDelete ? "true" : "false")}.";

        public const string RecordInserted = "Record has been inserted successfully.";
        public const string InsertedFailure = "Failed to insert the record. Please try again.";
        public const string UpdateFailure = "Failed to Updated the record. Please try again.";

        public const string RecordUpdated = "Record has been updated successfully.";
        public const string RecordNotUpdated = "Failed to update the record. Please try again.";

        public const string RecordDeleted = "Record has been deleted successfully.";
        public const string RecordNotDeleted = "Failed to delete the record. Please try again.";

        public const string RecordRetrieved = "Record retrieved successfully.";
        public const string RecordNotFound = "Record not found.";

        public const string BulkInsertSuccess = "Bulk insert operation completed successfully.";
        public const string BulkInsertFailure = "Bulk insert operation failed. Please check the input data.";

        public const string BulkUpdateSuccess = "Bulk update operation completed successfully.";
        public const string BulkUpdateFailure = "Bulk update operation failed. Please check the input data.";

        public const string InvalidData = "Invalid data provided. Please verify the input and try again.";

        public const string RecordDeletedSuccess = "Record deleted successfully";
        public const string RecordUpdatedSuccess = "Record updated successfully";
    }
}
