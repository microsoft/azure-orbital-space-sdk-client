namespace Microsoft.Azure.SpaceFx.SDK;

public class Utils {
    public static Task<bool> WaitForService(string appId, int? responseTimeoutSecs = null) => Task.Run(async () => {
        bool heardService = false;
        List<MessageFormats.Common.HeartBeatPulse> allServices;

        // Set a deadline of when we should hear back from the logging service
        DateTime responseDeadline = DateTime.UtcNow.AddSeconds(responseTimeoutSecs ?? Client.DefaultMessageResponseTimeout.TotalSeconds);

        // Keeping this here for debugging / break points
        allServices = Core.ServicesOnline();

        heardService = allServices.Any(service => string.Equals(service.AppId, appId, StringComparison.InvariantCultureIgnoreCase));

        // Loop until we hear the service or timeout
        while (DateTime.UtcNow <= responseDeadline && !heardService) {
            await Task.Delay(((int) Client.DefaultPollingTime.TotalMilliseconds));
            allServices = Core.ServicesOnline();
            heardService = allServices.Any(service => string.Equals(service.AppId, appId, StringComparison.InvariantCultureIgnoreCase));
        }

        return heardService;
    });

    public static byte[] ConvertProtoToBytes<T>(T protoObject) where T : IMessage {
        return protoObject.ToByteArray();
    }
}
