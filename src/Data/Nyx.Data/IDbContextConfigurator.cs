using Microsoft.EntityFrameworkCore;

namespace Nyx.Data
{
    public interface IDbContextConfigurator
    {
        void OnModelCreating(ModelBuilder modelBuilder);
    }
}