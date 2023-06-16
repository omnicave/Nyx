namespace Nyx.Data;

/// <summary>
///     Factory contract to make it easy to retrieve a data operation context.
/// </summary>
public interface IDataOperationContextFactory
{
    /// <summary>
    ///     Builds a data operation context that allows multiple data change and data fetch operations within a
    ///     transactional session.
    /// </summary>
    /// <returns></returns>
    IDataOperationContext GetTransactionalOperationContext();
    
    /// <summary>
    ///     Builds a data operation context that allows multiple data change or data fetch operations to be executed
    ///     and any changes committed at the end of all operations.
    /// </summary>
    /// <returns></returns>
    IDataOperationContext GetBatchingOperationContext();
    
    /// <summary>
    ///     Builds a simple data operation context that commits data immeditately after a data change operation completes.
    /// </summary>
    /// <returns></returns>
    IDataOperationContext GetSimpleOperationContext();
}