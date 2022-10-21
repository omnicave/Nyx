using System.Collections;
using NATS.Client;
using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public record NatsBatchContainerEntry(Guid InternalId, object Event, long sequence);

public class NatsBatchContainer : IBatchContainer
{
    [JsonProperty] 
    internal List<NatsBatchContainerEntry> Entries { get; } = new();
    
    [JsonConstructor]
    public NatsBatchContainer(
        Guid streamGuid, 
        string streamNamespace, 
        StreamSequenceToken sequenceToken)
    {
        StreamGuid = streamGuid;
        StreamNamespace = streamNamespace;
        SequenceToken = sequenceToken;
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Entries
            .Select(x => Tuple.Create((T)x.Event, (StreamSequenceToken)new EventSequenceTokenV2(x.sequence)))
            .ToList();
    }

    public void AddEvent(Guid internalId, object e, long sequence)
    {
        Entries.Add(new NatsBatchContainerEntry(internalId, e, sequence));
    }

    public bool ImportRequestContext()
    {
        return false;
    }

    public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
    {
        return Entries.Any(e => shouldReceiveFunc(stream, filterData, e.Event));
    }

    public Guid StreamGuid { get; }
    public string StreamNamespace { get; }
    
    public StreamSequenceToken SequenceToken { get; }
}