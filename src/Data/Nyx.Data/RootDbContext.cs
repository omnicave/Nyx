using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Nyx.Data
{
    public class RootDbContext : DbContext
    {
        private readonly IEnumerable<IDbContextConfigurator> _configurators;


        public RootDbContext(
            DbContextOptions<RootDbContext> options, 
            IEnumerable<IDbContextConfigurator> configurators
            ) : base(options)
        {
            _configurators = configurators;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _configurators.ToList().ForEach( x=>x.OnModelCreating(modelBuilder));
        }
    }
}