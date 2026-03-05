using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Nyx.Data;

public class RootDbContext(
    DbContextOptions<RootDbContext> options,
    IEnumerable<IDbContextConfigurator> configurators
    )
    : DbContext(options)
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        configurators.ToList().ForEach( x=>x.OnModelCreating(modelBuilder));
    }
}
