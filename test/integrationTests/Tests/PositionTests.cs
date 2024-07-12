namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class PositionTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    private readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Position}".ToLower();

    public PositionTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void GetPosition() {
        MessageFormats.HostServices.Position.PositionResponse? response = null;
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        Console.WriteLine($"Querying for last known position...");

        Task.Run(async () => {
            response = await Position.LastKnownPosition();
        });

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Heard response from '{TARGET_SERVICE_APP_ID}'.");

        // The Position Service doesn't return Success unless it has a position to return
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.NotFound, response.ResponseHeader.Status);

    }
}
