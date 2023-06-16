using Nyx.Orleans.Data;

namespace orleansdata.shared;

public interface IPrepareDataGrain : IGrainWithGuidKey
{
    Task PopulateDb();
}

[GenerateSerializer]
public record Product(
    Guid Id, string Name, string Description);