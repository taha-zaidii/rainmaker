using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Digi.Shared.Services;

namespace Digi.Shared.Middleware
{
    public static class DatabaseMigrationMiddleware
    {
        public static async Task<IHost> RunDatabaseMigrationsAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationService>>();
            var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

            try
            {
                logger.LogInformation("Starting database migration process...");

                var success = await migrationService.RunMigrationsAsync();
                
                if (success)
                {
                    logger.LogInformation("Database migration completed successfully");
                }
                else
                {
                    logger.LogError("Database migration failed");
                    throw new InvalidOperationException("Database migration failed");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database migration");
                throw;
            }

            return host;
        }

        public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
            services.AddScoped<IStoredProcedureMigrationService, StoredProcedureMigrationService>();
            services.AddScoped<IAdvancedStoredProcedureMigrationService, AdvancedStoredProcedureMigrationService>();
            services.AddScoped<IDirectSQLExecutionService, DirectSQLExecutionService>();
            return services;
        }
    }

    public class DatabaseMigrationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrationHostedService> _logger;

        public DatabaseMigrationHostedService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Database migration hosted service starting...");

                using var scope = _serviceProvider.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
                var spMigrationService = scope.ServiceProvider.GetRequiredService<IStoredProcedureMigrationService>();
                var advancedSPMigrationService = scope.ServiceProvider.GetRequiredService<IAdvancedStoredProcedureMigrationService>();
                var directSQLService = scope.ServiceProvider.GetRequiredService<IDirectSQLExecutionService>();

                // Run regular migrations first
                var migrationSuccess = await migrationService.RunMigrationsAsync();
                
                if (migrationSuccess)
                {
                    _logger.LogInformation("Database migration completed successfully");
                    
                    // Try basic stored procedure migrations first (more reliable for complex scripts)
                    try
                    {
                        _logger.LogInformation("Trying basic stored procedure migration first");
                        var spSuccess = await spMigrationService.RunStoredProcedureMigrationsAsync();
                        
                        if (spSuccess)
                        {
                            _logger.LogInformation("Basic stored procedure migration completed successfully");
                        }
                        else
                        {
                            _logger.LogWarning("Basic stored procedure migration failed, trying direct SQL execution");
                            
                            // Try direct SQL execution for complex scripts
                            try
                            {
                                var spPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations", "011_StoredProcedures");
                                var sqlFiles = Directory.GetFiles(spPath, "*.sql", SearchOption.AllDirectories);
                                
                                foreach (var file in sqlFiles)
                                {
                                    _logger.LogInformation("Executing SQL file directly: {FileName}", Path.GetFileName(file));
                                    var directSuccess = await directSQLService.ExecuteSQLFileAsync(file);
                                    
                                    if (directSuccess)
                                    {
                                        _logger.LogInformation("Direct SQL execution successful for: {FileName}", Path.GetFileName(file));
                                    }
                                    else
                                    {
                                        _logger.LogError("Direct SQL execution failed for: {FileName}", Path.GetFileName(file));
                                    }
                                }
                            }
                            catch (Exception directEx)
                            {
                                _logger.LogError(directEx, "Direct SQL execution failed, trying advanced approach");
                                
                                // Fallback to advanced SP migration
                                var advancedSPSuccess = await advancedSPMigrationService.RunAdvancedStoredProcedureMigrationsAsync();
                                
                                if (advancedSPSuccess)
                                {
                                    _logger.LogInformation("Advanced stored procedure migration completed successfully");
                                }
                                else
                                {
                                    _logger.LogError("Advanced stored procedure migration also failed");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in basic SP migration, trying direct SQL execution");
                        
                        // Try direct SQL execution
                        try
                        {
                            var spPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations", "011_StoredProcedures");
                            var sqlFiles = Directory.GetFiles(spPath, "*.sql", SearchOption.AllDirectories);
                            
                            foreach (var file in sqlFiles)
                            {
                                _logger.LogInformation("Executing SQL file directly: {FileName}", Path.GetFileName(file));
                                var directSuccess = await directSQLService.ExecuteSQLFileAsync(file);
                                
                                if (directSuccess)
                                {
                                    _logger.LogInformation("Direct SQL execution successful for: {FileName}", Path.GetFileName(file));
                                }
                                else
                                {
                                    _logger.LogError("Direct SQL execution failed for: {FileName}", Path.GetFileName(file));
                                }
                            }
                        }
                        catch (Exception directEx)
                        {
                            _logger.LogError(directEx, "Direct SQL execution also failed");
                        }
                    }
                }
                else
                {
                    _logger.LogError("Database migration failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database migration in hosted service");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database migration hosted service stopping...");
            return Task.CompletedTask;
        }
    }
}
