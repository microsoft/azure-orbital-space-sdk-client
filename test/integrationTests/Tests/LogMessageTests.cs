namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class LogMessageTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    private readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Logging}".ToLower();

    public LogMessageTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void SendLogMessage() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        MessageFormats.Common.LogMessageResponse? response = null;

        Task.Run(async () => {
            response = await Logging.SendLogMessage(logMessage: "Hello space world!", waitForResponse: true);
        });

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);
    }

    [Fact]
    public void SendTelemetryMessage() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        MessageFormats.Common.TelemetryMetricResponse? response = null;

        Task.Run(async () => {
            response = await Logging.SendTelemetry(metricName: "IntegrationTests", metricValue: 1, waitForResponse: true);
        });

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);
    }

    [Fact]
    public void SendTelemetryMultiMessage() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        MessageFormats.Common.TelemetryMultiMetricResponse? response = null;

        Task.Run(async () => {
            MessageFormats.Common.TelemetryMultiMetric message = new MessageFormats.Common.TelemetryMultiMetric();
            message.RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
            };
            message.RequestHeader.CorrelationId = message.RequestHeader.TrackingId;

            message.TelemetryMetrics.Add(new MessageFormats.Common.TelemetryMetric() {
                RequestHeader = message.RequestHeader,
                MetricName = "IntegrationTests",
                MetricValue = 1,
            });

            response = await Logging.SendMultiTelemetry(telemetryMessage: message, waitForResponse: true);
        });

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);
    }
}
