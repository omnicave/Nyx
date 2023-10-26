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
                    RefreshTtlForSiloEntry(_localSiloDetails.SiloAddress);
                    
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
        var w = Get(key);
        return Task.FromResult(
            new MembershipTableData(Tuple.Create(w.Entry, string.Empty), w.TableVersion)
        );
    }

    public Task<MembershipTableData> ReadAll()
    {
        var w = GetAll().ToArray();
        var result = w
            .Select(p => Tuple.Create(p.Entry, string.Empty))
            .ToList();

        return Task.FromResult(new MembershipTableData(result, w.FirstOrDefault()?.TableVersion ?? DefaultTableVersion));
    }

    public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        Upsert(entry, tableVersion);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        Upsert(entry, tableVersion, etag);
        return Task.FromResult(true);
    }

    public Task UpdateIAmAlive(MembershipEntry entry)
    {
        var (currentEntry, tableVersion, natsRevision) = Get(entry.SiloAddress);
        currentEntry.IAmAliveTime = entry.IAmAliveTime;
        Upsert(currentEntry, tableVersion, 
            natsRevision: natsRevision);
        
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

