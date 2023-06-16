using Microsoft.EntityFrameworkCore;
using Nyx.Data;

namespace orleansdata.models.dao;

public class DbContextConfigurator : IDbContextConfigurator
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>();
        modelBuilder.Entity<Order>();
        modelBuilder.Entity<OrderEvents>();
    }
}