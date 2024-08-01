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
        TimeSpan pauseTime = TimeSpan.FromMilliseconds(Client.APP_CONFIG.HEARTBEAT_RECEIVED_TOLERANCE_MS);
        Console.WriteLine($"Waiting for {pauseTime.Seconds} seconds, then checking for services heard...");
        Thread.Sleep(pauseTime);

        List<MessageFormats.Common.HeartBeatPulse> heartBeats = Microsoft.Azure.SpaceFx.SDK.Client.ServicesOnline();

        heartBeats.ForEach((_heartBeat) => {
            Console.WriteLine($"Service Online: {_heartBeat.AppId}");
        });

        Console.WriteLine($"Total Services Online: {heartBeats.Count}");

        Assert.True(heartBeats.Count > 0);
    }

    [Fact]
    public void HealthCheckTest() {
        TimeSpan waitTimeSpan = TimeSpan.FromMilliseconds(Client.APP_CONFIG.HEARTBEAT_RECEIVED_CRITICAL_TOLERANCE_MS);
        DateTime maxTimeToWait = DateTime.Now.Add(waitTimeSpan);

        Console.WriteLine($"Starting HealthCheckTest. Waiting maximum of {waitTimeSpan} for IsAppHealthy to be called.");

        while (TestSharedContext.HEALTH_CHECK_RECEIVED == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Console.WriteLine($"IS_APP_HEALTHY received: {TestSharedContext.HEALTH_CHECK_RECEIVED}");

        if (!TestSharedContext.HEALTH_CHECK_RECEIVED) throw new TimeoutException($"Failed to hear IsAppHealthy heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.");

        Assert.True(TestSharedContext.HEALTH_CHECK_RECEIVED);
    }
}
