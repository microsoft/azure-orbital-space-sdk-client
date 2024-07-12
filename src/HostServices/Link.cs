using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link;

namespace Microsoft.Azure.SpaceFx.SDK;

public class Link {

    private static ILogger? _logger = null;
    private static ILogger Logger {
        get {
            if (Client._grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            if (_logger is null) {
                _logger = Client._grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Link));
            }
            return _logger;
        }
    }

    private static readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Link}".ToLower();

    public static Task<MessageFormats.HostServices.Link.LinkResponse> SendFileToApp(string destinationAppId, string file, bool overwriteDestinationFile = false, int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Link.LinkRequest linkRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            },
            LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App,
            DestinationAppId = destinationAppId,
            FileName = System.IO.Path.GetFileName(file),
            Overwrite = overwriteDestinationFile
        };

        return SendLinkRequest(linkRequest, file: file, responseTimeoutSecs: responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Link.LinkResponse> DownlinkFile(string destinationAppId, string file, bool overwriteDestinationFile = false, int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Link.LinkRequest linkRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            },
            LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.Downlink,
            DestinationAppId = destinationAppId,
            FileName = System.IO.Path.GetFileName(file),
            Overwrite = overwriteDestinationFile
        };

        return SendLinkRequest(linkRequest, file: file, responseTimeoutSecs: responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Link.LinkResponse> CrosslinkFile(string destinationAppId, string file, bool overwriteDestinationFile = false, int? responseTimeoutSecs = null) {
        MessageFormats.HostServices.Link.LinkRequest linkRequest = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString()
            },
            LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.Crosslink,
            DestinationAppId = destinationAppId,
            FileName = System.IO.Path.GetFileName(file),
            Overwrite = overwriteDestinationFile
        };

        return SendLinkRequest(linkRequest, file: file, responseTimeoutSecs: responseTimeoutSecs);
    }

    public static Task<MessageFormats.HostServices.Link.LinkResponse> SendLinkRequest(MessageFormats.HostServices.Link.LinkRequest linkRequest, string file, int? responseTimeoutSecs = null) => Task.Run(async () => {
        MessageFormats.HostServices.Link.LinkResponse? response = null;

        bool targetServiceOnline = false;

        if (!File.Exists(file)) {
            Logger.LogError("File '{file}' not found.  Check path", file);
            throw new FileNotFoundException($"File '{file}' not found.  Check path");
        }

        if (string.IsNullOrWhiteSpace(linkRequest.RequestHeader.TrackingId)) linkRequest.RequestHeader.TrackingId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(linkRequest.RequestHeader.CorrelationId)) linkRequest.RequestHeader.CorrelationId = linkRequest.RequestHeader.TrackingId;

        var (inbox_directory, outbox_directory, root_directory) = Core.GetXFerDirectories().Result;

        if (!file.StartsWith(outbox_directory)) {
            Logger.LogDebug("Moving '{file}' to outbox directory '{outbox}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", file, outbox_directory, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);
            File.Copy(file, Path.Combine(outbox_directory, System.IO.Path.GetFileName(file)), overwrite: true);
        } else {
            linkRequest.Subdirectory = System.IO.Path.GetDirectoryName(file) ?? "";
            linkRequest.Subdirectory = linkRequest.Subdirectory.Replace(outbox_directory, ""); // Calculate the subdirectory name by removing the outbox directory name
            Logger.LogDebug("File '{file}' is in a subdirectory within outbox directory '{outbox}' of '{subdir}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", file, outbox_directory, linkRequest.Subdirectory, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);
        }

        linkRequest.FileName = System.IO.Path.GetFileName(file);


        Logger.LogDebug("Waiting for service '{service_app_id}' to come online", TARGET_SERVICE_APP_ID);
        // Wait for the service to come online
        targetServiceOnline = Utils.WaitForService(appId: TARGET_SERVICE_APP_ID, responseTimeoutSecs: responseTimeoutSecs).Result;

        if (!targetServiceOnline) {
            Logger.LogError("Service '{service_app_id}' is not online and not available to handle the message request.  No heartbeat was received within {responseTimeoutSecs} (trackingId: '{trackingId}' / correlationId: '{correlationId}')", TARGET_SERVICE_APP_ID, responseTimeoutSecs, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);

            // Response didn't come back in time.  Return with a failure
            throw new InvalidOperationException($"Service '{TARGET_SERVICE_APP_ID}' is not online and not available to handle the message request.");
        }

        Logger.LogDebug("Service '{service_app_id}' is online", TARGET_SERVICE_APP_ID);

        void LinkResponseEventHandler(object? _, MessageFormats.HostServices.Link.LinkResponse eventHandlerResponse) {
            if (eventHandlerResponse.ResponseHeader.TrackingId == linkRequest.RequestHeader.TrackingId) {
                Logger.LogDebug("Message response received for '{messageType}'.  Status: '{status}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", eventHandlerResponse.GetType().Name, eventHandlerResponse.ResponseHeader.Status, eventHandlerResponse.ResponseHeader.TrackingId, eventHandlerResponse.ResponseHeader.CorrelationId);

                if (eventHandlerResponse.ResponseHeader.Status != MessageFormats.Common.StatusCodes.Pending) {
                    response = eventHandlerResponse;
                    Client.LinkResponseEvent -= LinkResponseEventHandler; // Remove myself for next time
                }
            }
        }

        Client.LinkResponseEvent += LinkResponseEventHandler;

#pragma warning disable CS4014
        Logger.LogDebug("Sending '{messageType}' to '{appId}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkRequest.GetType().Name, TARGET_SERVICE_APP_ID, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);
        Client.DirectToApp(appId: TARGET_SERVICE_APP_ID, message: linkRequest);
#pragma warning restore CS4014

        TimeSpan maxWait = TimeSpan.FromSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);
        DateTime responseDeadline = DateTime.UtcNow.Add(maxWait);

        Logger.LogDebug("Waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LinkResponse), maxWait, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);

        // Start loop until we hear a response
        while (response is null && DateTime.UtcNow <= responseDeadline) {
            await Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds));
        }

        if (response == null) {
            Logger.LogError("Timed out waiting for '{messageType}'.  Deadline: '{timeout}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LinkResponse), maxWait, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);
            throw new TimeoutException($"Timed out waiting for a response from {TARGET_SERVICE_APP_ID}");
        }

        Logger.LogDebug("Returning '{messageType}' with status '{status}' to payload app (trackingId: '{trackingId}' / correlationId: '{correlationId}')", nameof(LinkResponse), response.ResponseHeader.Status, linkRequest.RequestHeader.TrackingId, linkRequest.RequestHeader.CorrelationId);

        return response;
    });
}
