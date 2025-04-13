using System.Collections;
using NATS.Client;
using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public record NatsBatchContainerEntry(Guid InternalId, object Event, long Sequence);

[method: JsonConstructor]
public class NatsBatchContainer(
    StreamId streamId,
    StreamSequenceToken sequenceToken
    ) : IBatchContainer
{
    [JsonProperty] 
    internal List<NatsBatchContainerEntry> Entries { get; } = new();

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

    public StreamId StreamId { get; } = streamId;

    public StreamSequenceToken SequenceToken { get; } = sequenceToken;
}