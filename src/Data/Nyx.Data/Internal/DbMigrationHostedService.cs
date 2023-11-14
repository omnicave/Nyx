using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nyx.Data.Internal
{
    public class DbMigrationHostedService : IHostedService
    {
        private readonly ILogger<DbMigrationHostedService> _log;
        private readonly IServiceProvider _serviceProvider;

        public DbMigrationHostedService(
            ILogger<DbMigrationHostedService> log,
            IServiceProvider serviceProvider
            )
        {
            _log = log;
            _serviceProvider = serviceProvider;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogTrace("DbMigrationHostedService::StartAsync() >>");
            using var scope = _serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<RootDbContext>();

            _log.LogInformation("Checking database ...");
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).ToList();
            _log.LogInformation("Checking database ... done");

            if (pendingMigrations?.Any() ?? false)
            {
                _log.LogInformation("Applying pending database migrations");
                await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                _log.LogInformation("Completed database migration");
            }
            
            _log.LogTrace("DbMigrationHostedService::StartAsync() <<");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}