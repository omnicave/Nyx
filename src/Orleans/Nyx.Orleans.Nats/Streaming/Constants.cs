namespace Nyx.Orleans.Nats.Streaming;

internal static class Constants
{
    public static class NatsHeaders
    {
        public const string PayloadTypeHeader = "OrleansStreamPayloadType";
        public const string StreamIdHeader = "OrleansStreamId";
        public const string StreamNamespaceHeader = "OrleansStreamNamespace";
    }
}