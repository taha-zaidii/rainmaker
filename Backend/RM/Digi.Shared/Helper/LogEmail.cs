using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public class LogEmail
    {
        private readonly IDbConnection _db;
        public async Task LogEmailAsync(string title, string sender, string receiver, string status, string body, string exception, string createdBy)
        {
            // using var connection = new SqlConnection(_db);
            await _db.ExecuteAsync(
                "sp_Glob_InsertEmailLog",
                new
                {
                    EmailTitle = title,
                    EmailSender = sender,
                    EmailReceiver = receiver,
                    EmailStatus = status,
                    EmailBody = body,
                    Exception = exception,
                    CreatedBy = createdBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

    }
}
