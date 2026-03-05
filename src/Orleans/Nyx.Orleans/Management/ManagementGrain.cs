using Orleans;
using Orleans.Concurrency;

namespace Shipbot.Common.Management;

[StatelessWorker]
public abstract class ManagementGrain : Grain, IManagementGrain
{
    
}