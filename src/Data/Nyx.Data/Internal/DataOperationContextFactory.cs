using System;
using Nyx.Data.Internal;

namespace Nyx.Data;

class DataOperationContextFactory : IDataOperationContextFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataOperationContextFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDataOperationContext GetTransactionalOperationContext() =>
        new DataOperationContextWrapper<TransactionalDataOperationContext>(_serviceProvider);

    public IDataOperationContext GetBatchingOperationContext() =>
        new DataOperationContextWrapper<BatchingDataOperationContext>(_serviceProvider);

    public IDataOperationContext GetSimpleOperationContext() =>
        new DataOperationContextWrapper<SimpleDataOperationContext>(_serviceProvider);
}