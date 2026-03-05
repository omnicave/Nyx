using Orleans.Concurrency;

namespace Nyx.Orleans.Management;

[StatelessWorker]
public abstract class ManagementGrain : Grain, IManagementGrain
{
    
}