namespace Nyx.Orleans.Nats.Clustering.Storage.Models;

public record ClusteringEntry(
    MembershipEntry Entry,
    TableVersion TableVersion,
    ulong? NatsRevision
) : ClusteringEntryStorage(Entry, TableVersion);

public record ClusteringEntryStorage(
    MembershipEntry Entry,
    TableVersion TableVersion
);