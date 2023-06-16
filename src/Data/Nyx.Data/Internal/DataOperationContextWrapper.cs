using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Nyx.Data.Internal;

/// <summary>
///     The DataOperationContextWrapper handles activations and de-activations of data operation contexts and also their
///     scope.  This is helpful in situations where the scope of the service provider that activated the user's code is
///     long lived (such as in .NET Orleans).
/// </summary>
/// <typeparam name="T"></typeparam>
public class DataOperationContextWrapper<T> : IDataOperationContext
    where T: IDataOperationContext
{
    private readonly IServiceScope _scope;
    private readonly T _context;

    public DataOperationContextWrapper(IServiceProvider rootServiceProvider)
    {
        _scope = rootServiceProvider.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _scope.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _scope.Dispose();
    }

    public DbSet<T1> GetDbSet<T1>() where T1 : class
    {
        return _context.GetDbSet<T1>();
    }

    public Task Commit()
    {
        return _context.Commit();
    }

    public Task Rollback()
    {
        return _context.Rollback();
    }

    public IDataChangeOperation BeginChangeOperation()
    {
        return _context.BeginChangeOperation();
    }

    public IDataFetchOperation BeginFetchOperation()
    {
        return _context.BeginFetchOperation();
    }

    public IEntityRepository Entities => _context.Entities;
    public IEntityRepository GetEntityRepository() => _context.GetEntityRepository();
}