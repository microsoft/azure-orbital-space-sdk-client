using Dapr.Client.Autogen.Grpc.v1;

namespace Microsoft.Azure.SpaceFx.SDK;
public class Client {
    private static Client _client { get; set; } = null!;
    internal static CancellationTokenSource _globalCancellationTokenSource { get; set; } = new CancellationTokenSource();
    internal static WebApplication _grpcHost { get; set; } = null!;
    private static bool IS_ONLINE {
        get {
            return !string.IsNullOrWhiteSpace(APP_ID);
        }
    }
    public static Core.APP_CONFIG APP_CONFIG {
        get {
            if (_grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            return new Core.APP_CONFIG();
        }
    }
    internal static TimeSpan DefaultMessageResponseTimeout;
    internal static TimeSpan DefaultPollingTime;
    internal static Core.Client? SPACEFX_CLIENT = null;
    internal static string? _appId = null;
    public static string APP_ID {
        get {
            if (_appId is null) return "";
            return _appId;
        }
    }
    public static ILogger Logger {
        get {
            if (_grpcHost is null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
            if (string.IsNullOrWhiteSpace(APP_ID)) return _grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.Azure.SpaceFx.PayloadApp.Uninitialized");
            return _grpcHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Microsoft.Azure.SpaceFx.PayloadApp." + APP_ID);
        }
    }
    public static EventHandler<MessageFormats.Common.LogMessageResponse>? LogMessageResponseEvent;
    public static EventHandler<MessageFormats.Common.TelemetryMetricResponse>? TelemetryMetricResponseEvent;
    public static EventHandler<MessageFormats.Common.TelemetryMultiMetricResponse>? TelemetryMultiMetricResponseEvent;
    public static EventHandler<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>? SensorsAvailableResponseEvent;
    public static EventHandler<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse>? SensorsTaskingPreCheckResponseEvent;
    public static EventHandler<MessageFormats.HostServices.Sensor.TaskingResponse>? SensorsTaskingResponseEvent;
    public static EventHandler<MessageFormats.HostServices.Sensor.SensorData>? SensorDataEvent;
    public static EventHandler<MessageFormats.HostServices.Position.PositionResponse>? PositionResponseEvent;
    public static EventHandler<MessageFormats.HostServices.Link.LinkResponse>? LinkResponseEvent;
    public delegate void SensorDataEventPythonHandler(byte[] sensorData);
    public static event SensorDataEventPythonHandler? SensorDataEventPython;

    /// <summary>(Optional) Provide a boolean response for the integrated app healthcheck.  If used, any value other than "true" will signify the app is in a failed state and should be terminated.</summary>
    public delegate bool IsAppHealthyDelegate();
    public static IsAppHealthyDelegate? IsAppHealthy;


    /// <summary>
    /// Instantiates the SDK Client and allows for messages to be sent and received
    /// </summary>
    /// <param name="MessageResponseTimeout">The amount of time to wait for a response to a message that has been sent.  Defaults to 30 seconds</param>
    /// <param name="PollingTime">The amount of time to wait inbetween checks for new messages.  Lower polling time makes responses faster, but higher load on the PC.  Higher polling time reduce processing impact, but slows responses to messages received.  Defaults to 250 milliseconds.</param>
    public static void Build(TimeSpan? MessageResponseTimeout = null, TimeSpan? PollingTime = null) {
        DefaultMessageResponseTimeout = MessageResponseTimeout ?? TimeSpan.FromSeconds(30);
        DefaultPollingTime = PollingTime ?? TimeSpan.FromMilliseconds(250);

        _client = new Client();
    }

    public static async Task KeepAppOpen() {
        while (!_globalCancellationTokenSource.IsCancellationRequested) {
            await Task.Delay(2000, _globalCancellationTokenSource.Token);
        }
    }


    /// <summary>
    /// Stops the SDK Client and disposes of all resources
    /// </summary>
    public static void Shutdown() {
        _globalCancellationTokenSource.Cancel();
        if (_client is not null && _grpcHost is not null)
            _grpcHost.StopAsync().Wait();
    }


    /// <summary>
    /// Enables apps to check if the client has been provisioned yet
    /// </summary>
    public static List<MessageFormats.Common.HeartBeatPulse> ServicesOnline() {
        if (Client.SPACEFX_CLIENT == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client.SPACEFX_CLIENT.ServicesOnline();
    }

    /// <summary>
    /// Provide a method to wait for the sidecar to start reporting healthy.  Default timespan of 30 seconds.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Task<Core.Enums.SIDECAR_STATUS> WaitForOnline(TimeSpan? timeOut = null) {
        TimeSpan waitTimeOut = timeOut ?? TimeSpan.FromSeconds(30);
        DateTime responseDeadline = DateTime.UtcNow.AddSeconds(waitTimeOut.TotalSeconds);

        // Start loop until we hear a response
        while (Client._client is null && DateTime.UtcNow <= responseDeadline) {
            Thread.Sleep(150);
        }

        if (Client._client == null) throw new TimeoutException("Timed out waiting for Client to provision.  Please run Client.Build before running this.");

        return Core.WaitForOnline(waitTimeOut);
    }

    /// <summary>
    /// Send a message directly to an App within the SDK
    /// </summary>
    /// <param name="appId">Name of the target app to receive the message</param>
    /// <param name="message">IMessage (protobuf object) to send</param>
    /// <returns></returns>
    public static Task DirectToApp(string appId, IMessage message) {
        if (Client.SPACEFX_CLIENT == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client.SPACEFX_CLIENT.DirectToApp(appId, message);
    }

    public Client() {
        if (_grpcHost != null || _client != null) return;


        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: false);

        if(! string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPACEFX_CONFIG_DIR")) && Directory.Exists(Environment.GetEnvironmentVariable("SPACEFX_CONFIG_DIR"))) {
            builder.Configuration.AddJsonFile(Path.Combine(Environment.GetEnvironmentVariable("SPACEFX_CONFIG_DIR"), "appsettings.json"), optional: true, reloadOnChange: false);
        }

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Sensor.SensorData>, MessageHandler<MessageFormats.HostServices.Sensor.SensorData>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.LogMessageResponse>, MessageHandler<MessageFormats.Common.LogMessageResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.TelemetryMetricResponse>, MessageHandler<MessageFormats.Common.TelemetryMetricResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.TelemetryMultiMetricResponse>, MessageHandler<MessageFormats.Common.TelemetryMultiMetricResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>, MessageHandler<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse>, MessageHandler<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Sensor.TaskingResponse>, MessageHandler<MessageFormats.HostServices.Sensor.TaskingResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Position.PositionResponse>, MessageHandler<MessageFormats.HostServices.Position.PositionResponse>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Link.LinkResponse>, MessageHandler<MessageFormats.HostServices.Link.LinkResponse>>();
            services.AddHostedService<ServiceCallback>();
        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
            logging.AddSimpleConsole(options => {
                options.ColorBehavior = Extensions.Logging.Console.LoggerColorBehavior.Disabled;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
            logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        });

        _globalCancellationTokenSource.Token.Register(() => {
            Console.WriteLine("Cancellation requested.");
        });

        _grpcHost = builder.Build();

        _grpcHost.UseRouting();
        _grpcHost.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Core.Services.MessageReceiver>();
            endpoints.MapGrpcHealthChecksService();
            endpoints.MapGet("/", async context => {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            });
        });

        _grpcHost.Use(async (context, next) => {
            try {
                await next.Invoke();
            } catch (Exception ex) {
                Console.WriteLine($"Exception caught in middleware: {ex.Message}");

                // Stop the host gracefully
                var lifetime = context.RequestServices.GetService<IHostApplicationLifetime>();
                lifetime?.StopApplication();
            }
        });

        Logger.LogDebug("Starting Microsoft Azure Orbital Client");

        _grpcHost.StartAsync().Wait();

        Logger.LogDebug("Waiting for Microsoft Azure Orbital Client to come online");

        // Waiting for the _grpcHost to spin up
        while (APP_ID is null || SPACEFX_CLIENT is null) {
            Thread.Sleep(250);
        }

        Logger.LogDebug("Microsoft Azure Orbital Client is online.  App ID: " + APP_ID);

    }

    public class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull, Google.Protobuf.IMessage {
        private readonly ILogger<MessageHandler<T>> _logger;
        private readonly IServiceProvider _serviceProvider;
        public MessageHandler(ILogger<MessageHandler<T>> logger, IServiceProvider serviceProvider) {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Route the message to the applicable Event Handler
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fullMessage"></param>
        public void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage) {
            using (var scope = _serviceProvider.CreateScope()) {
                _logger.LogDebug($"Receieved message type '{typeof(T).Name}'");

                if (message == null || EqualityComparer<T>.Default.Equals(message, default)) {
                    _logger.LogDebug("Received empty message '{messageType}' from '{appId}'.  Discarding message.", typeof(T).Name, fullMessage.SourceAppId);
                    return;
                }

                switch (typeof(T).Name) {
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Link.LinkResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Link.LinkResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: LinkResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Position.PositionResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Position.PositionResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: PositionResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.Common.LogMessageResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.Common.LogMessageResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: LogMessageResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.Common.TelemetryMetricResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.Common.TelemetryMetricResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: TelemetryMetricResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.Common.TelemetryMultiMetricResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.Common.TelemetryMultiMetricResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: TelemetryMultiMetricResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Sensor.SensorData).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Sensor.SensorData, sourceAppId: fullMessage.SourceAppId, eventHandler: SensorDataEvent);
                        if (message != null && message is MessageFormats.HostServices.Sensor.SensorData) {
                            _logger.LogDebug($"Routing message type '{typeof(T).Name}' to Python event handler");
                            SensorDataEventPython?.Invoke(message.ToByteArray());
                        }
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Sensor.SensorsAvailableResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Sensor.SensorsAvailableResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: SensorsAvailableResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Sensor.TaskingPreCheckResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Sensor.TaskingPreCheckResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: SensorsTaskingPreCheckResponseEvent);
                        break;
                    case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Sensor.TaskingResponse).Name, StringComparison.CurrentCultureIgnoreCase):
                        MessageEventRouter(message: message as MessageFormats.HostServices.Sensor.TaskingResponse, sourceAppId: fullMessage.SourceAppId, eventHandler: SensorsTaskingResponseEvent);
                        break;
                }
            }
        }

        private void MessageEventRouter<V>(V? message, string sourceAppId, EventHandler<V>? eventHandler) where V : Google.Protobuf.IMessage, new() {
            if (eventHandler == null || message == null) return;
            using (var scope = _serviceProvider.CreateScope()) {

                foreach (Delegate handler in eventHandler.GetInvocationList()) {
                    Task.Factory.StartNew(
                        () => handler.DynamicInvoke(sourceAppId, message));
                }
            }
        }
    }

    public class ServiceCallback : BackgroundService, Core.IMonitorableService {
        private readonly ILogger<ServiceCallback> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Azure.SpaceFx.Core.Client _client;

        public ServiceCallback(ILogger<ServiceCallback> logger, IServiceProvider serviceProvider, Core.Client client) {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _client = client;
        }

        public bool IsHealthy() {
            if (IsAppHealthy == null) {
                _logger.LogTrace("No AppHealthCheck event handler registered. Returning default value of 'true'.");
                return true;
            }

            _logger.LogTrace("Received Health Check request from cluster. Triggering IsAppHealthy event handler.");
            try {
                bool isHealthy = IsAppHealthy();
                _logger.LogDebug($"IsAppHealthy returned '{isHealthy}'.");
                if (!isHealthy) {
                    _logger.LogCritical("IsAppHealthy returned 'false' and is unhealthy. Check logs for more details.");
                }
                return isHealthy;
            } catch (Exception ex) {
                _logger.LogError(ex, "Exception calling IsAppHealthy. Setting response to false.");
                return false;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Task.Run(() => {
                using (var scope = _serviceProvider.CreateScope()) {
                    _appId = _client.GetAppID().Result;
                    SPACEFX_CLIENT = _client;
                }
            });

        }
    }
}
