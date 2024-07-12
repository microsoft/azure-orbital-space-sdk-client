namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class ServicesHeardTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public ServicesHeardTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void CheckServicesAreHeard() {
        // Services send out HeartBeats to let other apps know they are online.
        // We have to give enough time for heartbeats to come in before we check
        TimeSpan pauseTime = TimeSpan.FromMilliseconds(Client.APP_CONFIG.HEARTBEAT_PULSE_TIMING_MS * 2);
        Console.WriteLine($"Waiting for {pauseTime.Seconds} seconds, then checking for services heard...");
        Thread.Sleep(pauseTime);

        List<MessageFormats.Common.HeartBeatPulse> heartBeats = Microsoft.Azure.SpaceFx.SDK.Client.ServicesOnline();

        heartBeats.ForEach((_heartBeat) => {
            Console.WriteLine($"Service Online: {_heartBeat.AppId}");
        });

        Console.WriteLine($"Total Services Online: {heartBeats.Count}");

        Assert.True(heartBeats.Count > 0);
    }
}
