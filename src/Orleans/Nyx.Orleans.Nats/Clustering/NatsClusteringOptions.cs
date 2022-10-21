namespace Nyx.Orleans.Nats.Clustering;

public class NatsClusteringOptions
{
    public string NatsUrl { get; set; }
    public string BucketName { get; set; }

    public NatsClusteringOptions()
    {
        BucketName = "orleans-cluster-membership";
        NatsUrl = "nats://localhost:4222";
    }
}