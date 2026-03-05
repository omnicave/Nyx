namespace Nyx.Orleans.Host.Configuration.Models;

public class OrleansConfiguration
{
    public OrleansClusteringConfiguration Clustering { get; set; } = new();
}