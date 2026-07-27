using Microsoft.Extensions.Configuration;

namespace Digi.Shared.Services
{
    public class DatabaseMigrationSettings
    {
        public bool AutoRunMigrations { get; set; } = true;
        public bool CreateDatabaseIfNotExists { get; set; } = true;
        public string MigrationsPath { get; set; } = "Migrations";
        public int CommandTimeout { get; set; } = 300; // 5 minutes
        public bool LogScriptContent { get; set; } = false;
        public bool StopOnError { get; set; } = true;
        public bool AutoRunStoredProcedureMigrations { get; set; } = true;
    }

    public static class DatabaseMigrationConfiguration
    {
        public static DatabaseMigrationSettings GetMigrationSettings(IConfiguration configuration)
        {
            var settings = new DatabaseMigrationSettings();
            
            configuration.GetSection("DatabaseMigration").Bind(settings);
            
            return settings;
        }
    }
}
