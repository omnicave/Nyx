namespace Nyx.Orleans.Management;

public static class GrainFactoryExtensions
{
    public static TGrainInterface GetManagementGrain<TGrainInterface>(this IGrainFactory grainFactory)
        where TGrainInterface : IManagementGrain
    {
        return grainFactory.GetGrain<TGrainInterface>(Guid.Empty);
    }
}