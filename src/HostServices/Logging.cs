using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx.SDK;

public class Logging {
    private static readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Logging}".ToLower();
    private static ILogger? _logger = null;
    private static ILogger Logger {
        get {
            if (Client._grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            if (_logger is null) {
                _logger = Client._grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Logging));
            }
            return _logger;
        }
    }

    public static Task<MessageFormats.Common.LogMessageResponse> SendLogMessage(string logMessage, MessageFormats.Common.LogMessage.Types.LOG_LEVEL logLevel = MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info, int? responseTimeoutSecs = null, bool? waitForResponse = false) {
        MessageFormats.Common.LogMessage logMessageRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString(),
            },
            Message = logMessage,
            LogLevel = logLevel
        };


        return SendLogMessage(logMessageRequest, responseTimeoutSecs, waitForResponse);
    }

    public static Task<MessageFormats.Common.LogMessageResponse> SendLogMessage(MessageFormats.Common.LogMessage logMessage, int? responseTimeoutSecs = null, bool? waitForResponse = false) => Task.Run(() => {
        bool targetServiceOnline = false;

        if (logMessage.RequestHeader is null) logMessage.RequestHeader = new();
        if (string.IsNullOrWhiteSpace(logMessage.RequestHeader.TrackingId)) logMessage.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(logMessage.RequestHeader.CorrelationId)) logMessage.RequestHeader.CorrelationId = logMessage.RequestHeader.TrackingId;

        MessageFormats.Common.LogMessageResponse response = SpaceFx.Core.Utils.ResponseFromRequest(logMessage, new MessageFormats.Common.LogMessageResponse());

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

        Logger.LogDebug("WaitForResponse = '{wait_for_response}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", waitForResponse, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);
        // Only wire up a callback if we're waiting for a response
        if (waitForResponse == true) {
            void LogMessageResponseEventHandler(object? _, MessageFormats.Common.LogMessageResponse eventHandlerResponse) {
                if (eventHandlerResponse.ResponseHeader.TrackingId == logMessage.RequestHeader.TrackingId) {
                    Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId);
                    response = eventHandlerResponse;
                    Client.LogMessageResponseEvent -= LogMessageResponseEventHandler; // Remove the callback so it's not called for future iterations
                }
            }

            Client.LogMessageResponseEvent += LogMessageResponseEventHandler;
        }

        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", logMessage.GetType().Name, TARGET_SERVICE_APP_ID, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

        Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: logMessage).Wait();

        // Only wait for a response if we're expecting one
        if (waitForResponse == true) {
            TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
            DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

            Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LogMessageResponse), maxWait, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

            // Start loop until we hear a response
            while (response.ResponseHeader.Status == MessageFormats.Common.StatusCodes.Unknown && DateTime.UtcNow <= responseDeadline) {
                Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds)).Wait();
            }

            if (response == null) {
                Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LogMessageResponse), maxWait, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);
                throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
            }
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LogMessageResponse), response.ResponseHeader.Status, logMessage.RequestHeader.TrackingId, logMessage.RequestHeader.CorrelationId);

        return response;
    });

    public static Task<MessageFormats.Common.TelemetryMetricResponse> SendTelemetry(MessageFormats.Common.TelemetryMetric telemetryMessage, int? responseTimeoutSecs = null, bool? waitForResponse = false) => Task.Run(async () => {
        bool targetServiceOnline = false;

        if (telemetryMessage.RequestHeader is null) telemetryMessage.RequestHeader = new();
        if (string.IsNullOrWhiteSpace(telemetryMessage.RequestHeader.TrackingId)) telemetryMessage.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(telemetryMessage.RequestHeader.CorrelationId)) telemetryMessage.RequestHeader.CorrelationId = telemetryMessage.RequestHeader.TrackingId;

        MessageFormats.Common.TelemetryMetricResponse response = SpaceFx.Core.Utils.ResponseFromRequest(telemetryMessage, new MessageFormats.Common.TelemetryMetricResponse());

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

        Logger.LogDebug("WaitForResponse = '{wait_for_response}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", waitForResponse, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);


        // Only wire up a callback if we're waiting for a response
        if (waitForResponse == true) {
            void TelemetryResponseEventHandler(object? _, MessageFormats.Common.TelemetryMetricResponse eventHandlerResponse) {
                if (eventHandlerResponse.ResponseHeader.TrackingId == telemetryMessage.RequestHeader.TrackingId) {
                    Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId);

                    response = eventHandlerResponse;
                    Client.TelemetryMetricResponseEvent -= TelemetryResponseEventHandler; // Remove the callback so it's not called for future iterations
                }
            }

            Client.TelemetryMetricResponseEvent += TelemetryResponseEventHandler;
        }

        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", telemetryMessage.GetType().Name, TARGET_SERVICE_APP_ID, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

        await Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: telemetryMessage);

        // Only wait for a response if we're expecting one
        if (waitForResponse == true) {

            TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
            DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

            Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TelemetryMetricResponse), maxWait, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

            // Loop until we've received a response or we've timed out
            for (; (response == null || response.ResponseHeader.Status == MessageFormats.Common.StatusCodes.Unknown) && DateTime.UtcNow <= responseDeadline; await Task.Delay((int) Client.DefaultPollingTime.TotalMilliseconds)) ;

            if (response == null) {
                Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TelemetryMetricResponse), maxWait, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);
                throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
            }
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TelemetryMetricResponse), response.ResponseHeader.Status, telemetryMessage.RequestHeader.TrackingId, telemetryMessage.RequestHeader.CorrelationId);

        return response;
    });

    public static Task<MessageFormats.Common.TelemetryMetricResponse> SendTelemetry(string metricName, int metricValue, int? responseTimeoutSecs = null, bool? waitForResponse = false) {
        MessageFormats.Common.TelemetryMetric telemetryMessage = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString(),
            },
            MetricName = metricName,
            MetricValue = metricValue
        };

        return SendTelemetry(telemetryMessage: telemetryMessage, responseTimeoutSecs: responseTimeoutSecs, waitForResponse: waitForResponse);
    }
}
