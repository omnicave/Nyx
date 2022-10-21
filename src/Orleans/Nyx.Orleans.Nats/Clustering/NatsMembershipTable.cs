using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.KeyValue;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Nyx.Orleans.Nats.Clustering;

public class NatsMembershipTable : BaseNatsClusteringBucket, IMembershipTable
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ILogger<NatsMembershipTable> _log;
    private static readonly TableVersion DefaultTableVersion = new(0, "0");
    private Task? _keepAlive = null;
    private CancellationTokenSource? _cts;

    public NatsMembershipTable(
        IOptions<NatsClusteringOptions> natsClusteringOptions, 
        IOptions<ClusterOptions> clusterOptions, 
        ILocalSiloDetails localSiloDetails,
        ILogger<NatsMembershipTable> log)
        : base(natsClusteringOptions, clusterOptions)
    {
        _localSiloDetails = localSiloDetails;
        _log = log;
    }

    public Task InitializeMembershipTable(bool tryInitTableVersion)
    {
        Init();

        _cts = new CancellationTokenSource();
        _keepAlive = Task.Factory.StartNew(() =>
            {
                _cts.Token.ThrowIfCancellationRequested();

                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    var kv = GetBucket();
                    MembershipEntryMap.AddOrUpdate(
                        _localSiloDetails.SiloAddress,
                        address => throw new InvalidOperationException(),
                        (address, pair) => (pair.entry,
                            kv.Update(GetKey(address), Serialize(pair.entry), pair.revision))
                    );

                    if (_cts.Token.IsCancellationRequested)
                        break;
                }
            },
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Current
        );
        return Task.CompletedTask;
    }
    
    private void Upsert(IKeyValue kv, MembershipEntry entry, TableVersion tableVersion)
    {
        var revision = MembershipEntryMap.AddOrUpdate(
            entry.SiloAddress,
            address => (entry, kv.Put(GetKey(address), Serialize(entry))),
            (address, pair) => (entry, kv.Update(GetKey(address), Serialize(entry), pair.revision))
        );
    }

    public Task DeleteMembershipTableEntries(string clusterId)
    {
        var kv = GetBucket();
        foreach (var key in kv.Keys()) 
            kv.Delete(key);
        
        return Task.CompletedTask;
    }

    public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        return Task.CompletedTask;
    }

    public Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        var p = MembershipEntryMap[key];
        return Task.FromResult(
            new MembershipTableData(Tuple.Create(p.entry, p.revision.ToString()), DefaultTableVersion)
        );
    }

    public Task<MembershipTableData> ReadAll()
    {
        var result = MembershipEntryMap.Values
            .Select(p => Tuple.Create(p.entry, p.revision.ToString()))
            .ToList();

        return Task.FromResult(new MembershipTableData(result, DefaultTableVersion));
    }

    public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        var kv = GetBucket();
        Upsert(kv, entry, tableVersion);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        var kv = GetBucket();
        Upsert(kv, entry, tableVersion);
        return Task.FromResult(true);
    }

    public Task UpdateIAmAlive(MembershipEntry entry)
    {
        var currentEntry = MembershipEntryMap[entry.SiloAddress].entry;
        currentEntry.IAmAliveTime = entry.IAmAliveTime;
        
        var kv = GetBucket();
        Upsert(kv, currentEntry, DefaultTableVersion);
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _cts?.Cancel();

        _keepAlive?.Wait(TimeSpan.FromSeconds(30));
        _keepAlive?.Dispose();
        
        base.Dispose();
    }
}

