using System.Text;
using System.Text.Unicode;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using NATS.Client.KeyValue;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Nats;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public void CreateBucket()
    {
        using var c = new ConnectionFactory().CreateConnection("nats://localhost:4222");
        
        var kvm = c.CreateKeyValueManagementContext();
        
        var kvc = KeyValueConfiguration.Builder()
            .WithName("test-bucket")
            .WithMaxHistoryPerKey(5)
            .WithStorageType(StorageType.Memory)
            .WithTtl(Duration.OfSeconds(20))
            .Build();

        var kvs = kvm.Create(kvc);
        _testOutputHelper.WriteLine($"Status: {kvs}");

        var kv = c.CreateKeyValueContext("test-bucket");

        var rev1 = kv.Put("x", JsonConvert.SerializeObject(new Model("george", "vella")));
        var rev2 = kv.Put("x", JsonConvert.SerializeObject(new Model("george", "vella2")));

        
        // apierrorcode = JsWrongLastSequence / 10071 : key exists but cannot update because of mismatch in revision
        var rev3 = kv.Update(
            "z",
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Model("xyz", "vella"))),
            0
        );
    }

    [Fact]
    public void CreateStream()
    {
        var connectionFactory = new ConnectionFactory();
        
        
        
        var providerName = "test-provider";

        using var managementConnection = connectionFactory.CreateConnection("nats://localhost:4222");

        var jsm = managementConnection.CreateJetStreamManagementContext();
        var b = new StreamConfiguration.StreamConfigurationBuilder();
        var sc = b.WithName("test-stream")
            .AddSubjects($"{providerName}.*.*")
            .Build();

        jsm.AddStream(sc);

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // var t1 = Task.Run(() =>
        // {
        //     using var c = connectionFactory.CreateConnection("nats://localhost:4222");
        //     var js = c.CreateJetStreamContext();
        //     
        //     var rnd = new Random();
        //
        //     int i = 0;
        //
        //     while (!cts.Token.IsCancellationRequested)
        //     {
        //         _testOutputHelper.WriteLine($"Publishing [{i++}]...");
        //         var ns = $"namespace-{rnd.Next() % 100}";
        //         var streamId = $"{Guid.NewGuid():N}";
        //
        //         js.Publish(
        //             new Msg(
        //                 $"{providerName}.{ns}.{streamId}",
        //                 Encoding.UTF8.GetBytes($"hello world [Rnd: {rnd.Next()}]")
        //             )
        //         );
        //
        //         Thread.Sleep(100);
        //     }
        // }, cts.Token);

        var taskAction = () =>
        {
            using var c = connectionFactory.CreateConnection("nats://localhost:4222");
            var js = c.CreateJetStreamContext();
            var taskId = Guid.NewGuid();
            var sub = js.PullSubscribe($"{providerName}.*.*", PullSubscribeOptions.Builder()
                .WithStream("test-stream")
                .WithConfiguration(
                    ConsumerConfiguration.Builder()
                        .WithDurable("test-stream-consumer")
                        .WithAckPolicy(AckPolicy.Explicit)
                        .WithAckWait(2000)
                        .Build()
                )
                .Build()
            );
            
            
            
            _testOutputHelper.WriteLine($"Task: {taskId}");

            // var sub = js.PushSubscribeSync(
            //     $"{providerName}.*.*",
            //     PushSubscribeOptions.Builder()
            //         .WithStream("test-stream")
            //         .WithConfiguration(
            //             ConsumerConfiguration.Builder()
            //                 .WithDurable("test-stream-consumer")
            //                 .WithAckPolicy(AckPolicy.Explicit)
            //                 .WithAckWait(2000)
            //                 .Build()
            //         )
            //         .Build()
            // );
            while (!cts.Token.IsCancellationRequested)
            {
                // _testOutputHelper.WriteLine("Fetching ...");
                var batch = sub.Fetch(10, 1000);
                // _testOutputHelper.WriteLine($"Fetching ... ok [{batch.Count}]");

                foreach (var item in batch)
                {
                    item.InProgress();
                    _testOutputHelper.WriteLine($"[{taskId}][{item.Subject}]: {Encoding.UTF8.GetString(item.Data)}");
                    item.Ack();
                }
            }
        };

        _testOutputHelper.WriteLine("Waiting ...");
        Task.WaitAll(
            Task.Run(taskAction, cts.Token),
            Task.Run(taskAction, cts.Token)
        );
        _testOutputHelper.WriteLine("Exiting ...");
    }
}

public record Model(string Name, string Surname);

public class X : IKeyValueWatcher
{
    public void Watch(KeyValueEntry kve)
    {
        throw new NotImplementedException();
    }

    public void EndOfData()
    {
        throw new NotImplementedException();
    }
}