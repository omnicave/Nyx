using System.Collections;
using NATS.Client;
using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public record NatsBatchContainerEntry(Guid InternalId, object Event, long Sequence);

public class NatsBatchContainer : IBatchContainer
{
    [JsonProperty] 
    internal List<NatsBatchContainerEntry> Entries { get; } = new();
    
    [JsonConstructor]
    public NatsBatchContainer(
        StreamId streamId,
        StreamSequenceToken sequenceToken)
    {
        StreamId = streamId;
        SequenceToken = sequenceToken;
    }

    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Entries
            .Select(x => Tuple.Create((T)x.Event, (StreamSequenceToken)new EventSequenceTokenV2(x.Sequence)))
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

    public StreamId StreamId { get; }
    
    public StreamSequenceToken SequenceToken { get; }
}