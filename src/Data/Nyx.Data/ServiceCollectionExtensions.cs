using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Nyx.Data;
using Nyx.Data.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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
        return serviceCollection.RegisterDbContext(
            configuration.GetConnectionString(connectionStringName) ?? throw new InvalidOperationException($"Cannot find a connection string with name {connectionStringName}"),
            builderAction);
    }

    public static IServiceCollection RegisterDbContext(this IServiceCollection serviceCollection,
        string connectionString,
        Assembly migrationsAssembly,
        Action<NpgsqlDbContextOptionsBuilder>? builderAction = null
    )
    {

        NpgsqlDbContextOptionsBuilder ConfigureMigrations(NpgsqlDbContextOptionsBuilder c)
        {
            return c.MigrationsAssembly(migrationsAssembly.FullName ?? 
                                        throw new InvalidOperationException("Cannot identify migrations assembly (using entry)"));    
                
        }

        Action<NpgsqlDbContextOptionsBuilder> a = builderAction != null
            ? builder => builderAction(ConfigureMigrations(builder))
            : builder => ConfigureMigrations(builder);

        return serviceCollection.RegisterDbContext(connectionString, a);

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
            new ServiceDescriptor(typeof(IEntityRepository<>), typeof(TypedEntityRepository<>),
                ServiceLifetime.Scoped)
        );

        DbContextOptionsBuilder Defaults(DbContextOptionsBuilder builder) => builder
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        NpgsqlDbContextOptionsBuilder CommonOptionsBuilderConfiguration(NpgsqlDbContextOptionsBuilder c) => c;

        Action<NpgsqlDbContextOptionsBuilder> act = builderAction != null
            ? builder => builderAction(CommonOptionsBuilderConfiguration(builder))
            : builder => CommonOptionsBuilderConfiguration(builder);

        return serviceCollection.AddDbContext<RootDbContext>(
            builder => Defaults(builder).UseNpgsql(connectionString, act)
            );
    }

    public static IServiceCollection RegisterDataMigrationStartupService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<DbMigrationHostedService>();
        return serviceCollection;
    }
}