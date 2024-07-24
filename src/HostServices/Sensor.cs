using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor;

namespace Microsoft.Azure.SpaceFx.SDK;

public class Sensor {
    private static readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Sensor}".ToLower();
    private static ILogger? _logger = null;
    private static ILogger Logger {
        get {
            if (Client._grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            if (_logger is null) {
                _logger = Client._grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Sensor));
            }
            return _logger;
        }
    }
    public static Task<MessageFormats.HostServices.Sensor.SensorsAvailableResponse> GetAvailableSensors(int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Sensor.SensorsAvailableRequest sensorsAvailableRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            }
        };

        return GetAvailableSensors(sensorsAvailableRequest, responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Sensor.SensorsAvailableResponse> GetAvailableSensors(MessageFormats.HostServices.Sensor.SensorsAvailableRequest sensorsAvailableRequest, int? responseTimeoutSecs = null) => Task.Run(async () => {
        MessageFormats.HostServices.Sensor.SensorsAvailableResponse? response = null;
        bool targetServiceOnline = false;

        if (string.IsNullOrWhiteSpace(sensorsAvailableRequest.RequestHeader.TrackingId)) sensorsAvailableRequest.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(sensorsAvailableRequest.RequestHeader.CorrelationId)) sensorsAvailableRequest.RequestHeader.CorrelationId = sensorsAvailableRequest.RequestHeader.TrackingId;

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);


        void SensorsAvailableResponseEventHandler(object? _, MessageFormats.HostServices.Sensor.SensorsAvailableResponse eventHandlerResponse) {
            if (eventHandlerResponse.ResponseHeader.TrackingId == sensorsAvailableRequest.RequestHeader.TrackingId) {
                Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId, eventHandlerResponse.ResponseHeader.Status);
                response = eventHandlerResponse;
                Client.SensorsAvailableResponseEvent -= SensorsAvailableResponseEventHandler;
            }
        }

        Client.SensorsAvailableResponseEvent += SensorsAvailableResponseEventHandler;

#pragma warning disable CS4014
        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", sensorsAvailableRequest.GetType().Name, TARGET_SERVICE_APP_ID, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);
        Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: sensorsAvailableRequest);
#pragma warning restore CS4014

        TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
        DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

        Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(SensorsAvailableResponse), maxWait, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);

        // Start loop until we hear a response
        while (response is null && DateTime.UtcNow <= responseDeadline) {
            await Task.Delay((int) Client.DefaultPollingTime.TotalMilliseconds);
        }

        // Response didn't come back in time.  Return with a failure
        if (response == null) {
            Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(SensorsAvailableResponse), maxWait, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId);
            throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", nameof(SensorsAvailableResponse), response.ResponseHeader.Status, sensorsAvailableRequest.RequestHeader.TrackingId, sensorsAvailableRequest.RequestHeader.CorrelationId, response.ResponseHeader.Status);

        return response;
    });

    public static Task<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse> SensorTaskingPreCheck(string sensorId, Any? requestData = null, Dictionary<string, string>? metaData = null, int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Sensor.TaskingPreCheckRequest sensorTaskingPreCheckRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            },
            SensorID = sensorId
        };

        sensorTaskingPreCheckRequest.RequestHeader.CorrelationId = sensorTaskingPreCheckRequest.RequestHeader.TrackingId;

        if (requestData != null) sensorTaskingPreCheckRequest.RequestData = requestData;
        if (metaData != null) {
            metaData.ToList().ForEach(x => sensorTaskingPreCheckRequest.RequestHeader.Metadata.Add(x.Key, x.Value));
        }

        return SensorTaskingPreCheck(sensorTaskingPreCheckRequest, responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse> SensorTaskingPreCheck(MessageFormats.HostServices.Sensor.TaskingPreCheckRequest taskingPreCheckRequest, int? responseTimeoutSecs = null) => Task.Run(async () => {
        MessageFormats.HostServices.Sensor.TaskingPreCheckResponse? response = null;
        bool targetServiceOnline = false;

        if (string.IsNullOrWhiteSpace(taskingPreCheckRequest.RequestHeader.TrackingId)) taskingPreCheckRequest.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(taskingPreCheckRequest.RequestHeader.CorrelationId)) taskingPreCheckRequest.RequestHeader.CorrelationId = taskingPreCheckRequest.RequestHeader.TrackingId;

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);


        void TaskingPreCheckResponseEventHandler(object? _, MessageFormats.HostServices.Sensor.TaskingPreCheckResponse eventHandlerResponse) {
            if (eventHandlerResponse.ResponseHeader.TrackingId == taskingPreCheckRequest.RequestHeader.TrackingId) {
                Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId, eventHandlerResponse.ResponseHeader.Status);

                response = eventHandlerResponse;
                Client.SensorsTaskingPreCheckResponseEvent -= TaskingPreCheckResponseEventHandler;
            }
        }

        Client.SensorsTaskingPreCheckResponseEvent += TaskingPreCheckResponseEventHandler;

#pragma warning disable CS4014
        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", taskingPreCheckRequest.GetType().Name, TARGET_SERVICE_APP_ID, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);
        Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: taskingPreCheckRequest);
#pragma warning restore CS4014

        TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
        DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

        Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TaskingPreCheckResponse), maxWait, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);

        // Start loop until we hear a response
        while (response is null && DateTime.UtcNow <= responseDeadline) {
            await Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds));
        }

        if (response == null) {
            Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TaskingPreCheckResponse), maxWait, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId);
            throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", nameof(TaskingPreCheckResponse), response.ResponseHeader.Status, taskingPreCheckRequest.RequestHeader.TrackingId, taskingPreCheckRequest.RequestHeader.CorrelationId, response.ResponseHeader.Status);


        return response;
    });

    public static Task<MessageFormats.HostServices.Sensor.TaskingResponse> SensorTasking(string sensorId, Any? requestData = null, Dictionary<string, string>? metaData = null, int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Sensor.TaskingRequest sensorTaskingRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString(),
            },
            SensorID = sensorId
        };

        if (requestData != null) sensorTaskingRequest.RequestData = requestData;
        if (metaData != null) {
            metaData.ToList().ForEach(x => sensorTaskingRequest.RequestHeader.Metadata.Add(x.Key, x.Value));
        }

        return SensorTasking(sensorTaskingRequest, responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Sensor.TaskingResponse> SensorTasking(MessageFormats.HostServices.Sensor.TaskingRequest taskingRequest, int? responseTimeoutSecs = null) => Task.Run(async () => {
        MessageFormats.HostServices.Sensor.TaskingResponse? response = null;
        bool targetServiceOnline = false;

        if (string.IsNullOrWhiteSpace(taskingRequest.RequestHeader.TrackingId)) taskingRequest.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(taskingRequest.RequestHeader.CorrelationId)) taskingRequest.RequestHeader.CorrelationId = taskingRequest.RequestHeader.TrackingId;

        Logger.LogDebug("Waiting for service '{service_app_id}' to come online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);


        void TaskingResponseEventHandler(object? _, MessageFormats.HostServices.Sensor.TaskingResponse eventHandlerResponse) {
            if (eventHandlerResponse.ResponseHeader.TrackingId == taskingRequest.RequestHeader.TrackingId) {
                Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId, eventHandlerResponse.ResponseHeader.Status);

                response = eventHandlerResponse;
                Client.SensorsTaskingResponseEvent -= TaskingResponseEventHandler;
            }
        }

        Client.SensorsTaskingResponseEvent += TaskingResponseEventHandler;

        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", taskingRequest.GetType().Name, TARGET_SERVICE_APP_ID, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

#pragma warning disable CS4014
        Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: taskingRequest);
#pragma warning restore CS4014

        TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
        DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

        Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TaskingResponse), maxWait, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

        // Start loop until we hear a response
        while (response is null && DateTime.UtcNow <= responseDeadline) {
            await Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds));
        }

        if (response == null) {
            Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(TaskingResponse), maxWait, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);
            throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}' / status: '{status}')", nameof(TaskingResponse), response.ResponseHeader.Status, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId, response.ResponseHeader.Status);


        return response;
    });
}
