using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor;

namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class SensorTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    private readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Sensor}".ToLower();
    private readonly string TEST_SENSOR_ID = "DemoTemperatureSensor";

    public SensorTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void TestSensorService() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        SensorsAvailableResponse? sensorsAvailableResponse = null;
        SensorData? sensorData = null;
        TaskingPreCheckResponse? taskingPreCheckResponse = null;
        TaskingResponse? taskingResponse = null;


        Console.WriteLine($"Registering callback for '{typeof(SensorData)}'...");
        void SensorDataEventHandler(object? _, SensorData _sensorData) {
            sensorData = _sensorData;
        }

        Client.SensorDataEvent += SensorDataEventHandler;

        #region GetAvailableSensors


        Console.WriteLine($"Querying '{TARGET_SERVICE_APP_ID}' for available sensors...");

        Task.Run(async () => {
            sensorsAvailableResponse = await Sensor.GetAvailableSensors();
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (sensorsAvailableResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (sensorsAvailableResponse == null) throw new TimeoutException($"Failed to hear {nameof(sensorsAvailableResponse)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(sensorsAvailableResponse)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, sensorsAvailableResponse.ResponseHeader.Status);
        Assert.NotEmpty(sensorsAvailableResponse.Sensors);
        Assert.Contains(sensorsAvailableResponse.Sensors, s => s.SensorID == TEST_SENSOR_ID);

        Console.WriteLine($"Test Sensor '{TEST_SENSOR_ID}' found in available sensors");

        #endregion

        #region TaskingPrecheck

        Console.WriteLine($"Tasking Precheck for Sensor '{TEST_SENSOR_ID}'...");

        Task.Run(async () => {
            taskingPreCheckResponse = await Sensor.SensorTaskingPreCheck(sensorId: TEST_SENSOR_ID);
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (taskingPreCheckResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (taskingPreCheckResponse == null) throw new TimeoutException($"Failed to hear {nameof(taskingPreCheckResponse)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(taskingPreCheckResponse)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, taskingPreCheckResponse.ResponseHeader.Status);
        #endregion


        #region Tasking

        Console.WriteLine($"Tasking Sensor '{TEST_SENSOR_ID}'...");

        Task.Run(async () => {
            sensorData = null;
            taskingResponse = await Sensor.SensorTasking(sensorId: TEST_SENSOR_ID);
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (taskingResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (taskingResponse == null) throw new TimeoutException($"Failed to hear {nameof(taskingResponse)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(taskingResponse)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, taskingResponse.ResponseHeader.Status);
        #endregion


        #region SensorData

        Console.WriteLine($"Waiting for Sensor Data from Sensor '{TEST_SENSOR_ID}'...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (sensorData == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (sensorData == null) throw new TimeoutException($"Failed to hear {nameof(sensorData)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(sensorData)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, sensorData.ResponseHeader.Status);
        #endregion

    }
}
