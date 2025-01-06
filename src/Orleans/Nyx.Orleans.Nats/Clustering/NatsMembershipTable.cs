using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Orleans.Configuration;

namespace Nyx.Orleans.Nats.Clustering;

public class NatsMembershipTable(
    IOptions<NatsClusteringOptions> natsClusteringOptions,
    IOptions<ClusterOptions> clusterOptions,
    ILocalSiloDetails localSiloDetails,
    ILogger<NatsMembershipTable> log)
    : BaseNatsClusteringBucket(natsClusteringOptions, clusterOptions), IMembershipTable
{
    private readonly ILogger<NatsMembershipTable> _log = log;
    private static readonly TableVersion DefaultTableVersion = new(0, "0");
    private Task? _keepAlive = null;
    private CancellationTokenSource? _cts;

    public async Task InitializeMembershipTable(bool tryInitTableVersion)
    {
        await Init();

        _cts = new CancellationTokenSource();
        var t = Task.Factory.StartNew(async () =>
            {
                _cts.Token.ThrowIfCancellationRequested();

                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    await RefreshTtlForSiloEntry(localSiloDetails.SiloAddress);
                    
                    if (_cts.Token.IsCancellationRequested)
                        break;
                }
            },
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Current
        );
        
        _keepAlive = t.Unwrap();
    }

    public async Task DeleteMembershipTableEntries(string clusterId)
    {
        var kv = await GetBucket();
        var keys = kv.GetKeysAsync();
        await foreach (var key in keys) 
            await kv.DeleteAsync(key);
    }

    public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        return Task.CompletedTask;
    }

    public async Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        var w = await Get(key);
        return new MembershipTableData(Tuple.Create(w.Entry, string.Empty), w.TableVersion);
    }

    public async Task<MembershipTableData> ReadAll()
    {
        var w = ( await GetAll()).ToArray();
        var result = w
            .Select(p => Tuple.Create(p.Entry, string.Empty))
            .ToList();

        return new MembershipTableData(result, w.FirstOrDefault()?.TableVersion ?? DefaultTableVersion);
    }

    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        await Upsert(entry, tableVersion);
        return true;
    }

    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        await Upsert(entry, tableVersion, etag);
        return true;
    }

    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        var (currentEntry, tableVersion, natsRevision) = await Get(entry.SiloAddress);
        currentEntry.IAmAliveTime = entry.IAmAliveTime;
        await Upsert(currentEntry, tableVersion, 
            natsRevision: natsRevision);
    }

    public override ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        _keepAlive?.Wait(TimeSpan.FromSeconds(30));
        _keepAlive?.Dispose();

        return base.DisposeAsync();
    }
}

