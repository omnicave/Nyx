using System;
using Nyx.Data.Internal;

namespace Nyx.Data;

class DataOperationContextFactory(IServiceProvider rootServiceProvider) : IDataOperationContextFactory
{
    public IDataOperationContext GetTransactionalOperationContext() =>
        new DataOperationContextWrapper<TransactionalDataOperationContext>(rootServiceProvider);

    public IDataOperationContext GetBatchingOperationContext() =>
        new DataOperationContextWrapper<BatchingDataOperationContext>(rootServiceProvider);

    public IDataOperationContext GetSimpleOperationContext() =>
        new DataOperationContextWrapper<SimpleDataOperationContext>(rootServiceProvider);
}
