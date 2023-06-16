using Bogus;
using Nyx.Data;
using orleansdata.models.dao;
using orleansdata.shared;
using Product = orleansdata.models.dao.Product;

namespace orleansdata.server.Grains;

public class PrepareDataGrain : Grain, IPrepareDataGrain
{
    private readonly IDataOperationContextFactory _dataOperationContextFactory;

    public PrepareDataGrain(IDataOperationContextFactory dataOperationContextFactory)
    {
        _dataOperationContextFactory = dataOperationContextFactory;
    }
    
    public async Task PopulateDb()
    {
        await using var context = _dataOperationContextFactory.GetBatchingOperationContext();
        var repository = context.GetEntityRepository();
        
        var products = new Faker<Product>()
            .RuleFor(p => p.Description, f => f.Rant.Review())
            .RuleFor(p => p.Name, f => f.Name.FindName())
            .Generate(5);

        foreach (var p in products)
        {
            await repository.Add(p);
        }


        await context.Commit();
    }
}