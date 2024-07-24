using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Position;

namespace Microsoft.Azure.SpaceFx.SDK;

public class Position {
    private static readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Position}".ToLower();
    private static ILogger? _logger = null;
    private static ILogger Logger {
        get {
            if (Client._grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            if (_logger is null) {
                _logger = Client._grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Position));
            }
            return _logger;
        }
    }
    public static Task<MessageFormats.HostServices.Position.PositionResponse> LastKnownPosition(int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Position.PositionRequest positionRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            }
        };
        return LastKnownPosition(positionRequest, responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Position.PositionResponse> LastKnownPosition(MessageFormats.HostServices.Position.PositionRequest positionRequest, int? responseTimeoutSecs = null) => Task.Run(async () => {
        MessageFormats.HostServices.Position.PositionResponse? response = null;
        bool targetServiceOnline = false;

        if (string.IsNullOrWhiteSpace(positionRequest.RequestHeader.TrackingId)) positionRequest.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(positionRequest.RequestHeader.CorrelationId)) positionRequest.RequestHeader.CorrelationId = positionRequest.RequestHeader.TrackingId;

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);


        // Create an in-line callback function to get the response for this message
        void PositionResponseEventHandler(object? _, MessageFormats.HostServices.Position.PositionResponse eventHandlerResponse) {
            if (eventHandlerResponse.ResponseHeader.TrackingId == positionRequest.RequestHeader.TrackingId) {
                Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId, eventHandlerResponse.ResponseHeader.Status);

                response = eventHandlerResponse;
                Client.PositionResponseEvent -= PositionResponseEventHandler; // Remove myself for next time
            }
        }

        // Register the temporary in-line function
        Client.PositionResponseEvent += PositionResponseEventHandler;

        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", positionRequest.GetType().Name, TARGET_SERVICE_APP_ID, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);


        await Client.DirectToApp(TARGET_SERVICE_APP_ID, positionRequest);


        TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
        DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

        Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(PositionResponse), maxWait, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);

        // Start loop until we hear a response
        while (response is null && DateTime.UtcNow <= responseDeadline) {
            await Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds));
        }

        if (response == null) {
            Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(PositionResponse), maxWait, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId);
            throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", nameof(PositionResponse), response.ResponseHeader.Status, positionRequest.RequestHeader.TrackingId, positionRequest.RequestHeader.CorrelationId, response.ResponseHeader.Status);

        return response;
    });
}
