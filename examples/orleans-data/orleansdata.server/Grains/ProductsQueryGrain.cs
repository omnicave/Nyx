using System.Linq.Expressions;
using Nyx.Data;
using Nyx.Orleans.Data;
using orleansdata.shared;

namespace orleansdata.server.Grains;

public class ProductsQueryGrain : BaseEntityRepositoryQueryGrain<Product, models.dao.Product>
{
    public ProductsQueryGrain(IEntityRepository<models.dao.Product> cloudResourceRepository) : base(cloudResourceRepository)
    {
    }

    protected override Task<Product> ConvertDaoToModel(models.dao.Product dao)
    {
        var m = new Product(dao.Id, dao.Name, dao.Description);
        return Task.FromResult(m);
    }

    protected override Expression<Func<models.dao.Product, bool>> BuildGenericSearchStringFilter(string searchTerm)
    {
        return product => product.Description.Contains(searchTerm) || product.Name.Contains(searchTerm);
    }

    protected override Expression<Func<models.dao.Product, bool>> BuildFilterBasedOnModelField(string field, string[] values)
    {
        throw new NotImplementedException();
    }
}