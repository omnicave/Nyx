using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Nyx.Data;
using Nyx.Data.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers a db context configurator.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IServiceCollection AddDbContextConfigurator<T>(this IServiceCollection serviceCollection)
            where T: class, IDbContextConfigurator
        {
            return serviceCollection.AddTransient<IDbContextConfigurator, T>();
        }

        public static IServiceCollection RegisterDbContext(this IServiceCollection serviceCollection,
            IConfiguration configuration,
            string connectionStringName,
            Action<NpgsqlDbContextOptionsBuilder>? builderAction = null
        )
        {
            return serviceCollection.RegisterDbContext(configuration.GetConnectionString(connectionStringName),
                builderAction);
        }
        
        public static IServiceCollection RegisterDbContext(this IServiceCollection serviceCollection, 
            string connectionString,
            Action<NpgsqlDbContextOptionsBuilder>? builderAction = null
        )
        {
            //serviceCollection.AddScoped<IUnitOfWork, SimpleUnitOfWork>();
            serviceCollection.AddScoped<SimpleDataOperationContext>();
            serviceCollection.AddScoped<TransactionalDataOperationContext>();
            serviceCollection.AddScoped<BatchingDataOperationContext>();

            serviceCollection.AddSingleton<IDataOperationContextFactory, DataOperationContextFactory>();
            serviceCollection.Add(
                new ServiceDescriptor(typeof(IEntityRepository<>), typeof(EntityRepository<>), ServiceLifetime.Scoped)
                );

            DbContextOptionsBuilder Defaults(DbContextOptionsBuilder builder) => builder
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();

            Action<DbContextOptionsBuilder> a = builderAction == null
                ? builder => Defaults(builder).UseNpgsql(connectionString)
                : builder => Defaults(builder).UseNpgsql(connectionString, builderAction);    
            
            return serviceCollection.AddDbContext<RootDbContext>(a);
        }

        public static IServiceCollection RegisterDataMigrationStartupService(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddHostedService<DbMigrationHostedService>();
            return serviceCollection;
        }
    }
}