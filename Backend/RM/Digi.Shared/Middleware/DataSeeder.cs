using System.Data;
using Dapper;
using Digi.Shared.Helper;

namespace Digi.Shared.Middleware
{
    public class DataSeeder
    {
        private readonly IDbConnection _dbConnection;

        public DataSeeder(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task SeedSuperAdminAsync(string hashedPasswordFromApi, string email, string userName)
        {
            // Ecrypt UserName
            string encryptedUserName = EncryptionHelper.EncryptText(userName!.ToLower());
            //Encrypt UserEmail
            string encryptedUserEmail = EncryptionHelper.EncryptText(email!.ToLower());
            var saltRandom = Guid.NewGuid().ToString();
            var saltEncode = HelperFun.EncodeText(saltRandom);
            var conPassword = hashedPasswordFromApi + saltRandom;
            var bHashPassword = BCrypt.Net.BCrypt.HashPassword(conPassword);

            var parameters = new DynamicParameters();
            parameters.Add("@HashedPassword", bHashPassword, DbType.String);
            parameters.Add("@SecurityStamp", saltEncode, DbType.String);
            parameters.Add("@AdminEmail", encryptedUserEmail, DbType.String);
            parameters.Add("@Username", encryptedUserName, DbType.String);

           // parameters.Add("@Message",);
            await _dbConnection.ExecuteAsync("sp_DataSeed_Adm_System", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
