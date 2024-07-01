namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class ProtoTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    public ProtoTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void PositionRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader" };

        CheckProperties<MessageFormats.HostServices.Position.PositionRequest>(expectedProperties);
    }

    [Fact]
    public void PositionResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "Position" };

        CheckProperties<MessageFormats.HostServices.Position.PositionResponse>(expectedProperties);
    }

    [Fact]
    public void PositionUpdateRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "Position" };

        CheckProperties<MessageFormats.HostServices.Position.PositionUpdateRequest>(expectedProperties);
    }

    [Fact]
    public void PositionUpdateResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };

        CheckProperties<MessageFormats.HostServices.Position.PositionUpdateResponse>(expectedProperties);
    }

    [Fact]
    public void PositionAttitude() {
        // Arrange
        List<string> expectedProperties = new() { "X", "Y", "Z", "K" };

        CheckProperties<MessageFormats.HostServices.Position.Position.Types.Attitude>(expectedProperties);
    }

    [Fact]
    public void PositionPoint() {
        // Arrange
        List<string> expectedProperties = new() { "X", "Y", "Z" };

        CheckProperties<MessageFormats.HostServices.Position.Position.Types.Point>(expectedProperties);
    }

    [Fact]
    public void Position() {
        // Arrange
        List<string> expectedProperties = new() { "PositionTime", "Point", "Attitude" };

        var request = new Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Position.Position() {
            PositionTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
            Point = new MessageFormats.HostServices.Position.Position.Types.Point() {
                X = 1,
                Y = 2,
                Z = 3,
            },
            Attitude = new MessageFormats.HostServices.Position.Position.Types.Attitude() {
                X = 1,
                Y = 2,
                Z = 3,
                K = 4
            }
        };

        Assert.Equal(1, request.Point.X);
        Assert.Equal(2, request.Point.Y);
        Assert.Equal(3, request.Point.Z);
        Assert.Equal(1, request.Attitude.X);
        Assert.Equal(2, request.Attitude.Y);
        Assert.Equal(3, request.Attitude.Z);
        Assert.Equal(4, request.Attitude.K);

        CheckProperties<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Position.Position>(expectedProperties);
    }

    [Fact]
    public void LinkRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "LinkType", "Priority", "FileName", "Subdirectory", "DestinationAppId", "Overwrite", "LeaveSourceFile", "ExpirationTime" };

        var request = new Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest() {
            LinkType = Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App,
            FileName = _context.GenericString,
            Subdirectory = _context.GenericString,
            DestinationAppId = _context.GenericString,
            Overwrite = true,
            LeaveSourceFile = true,
            ExpirationTime = _context.GenericTimeStamp
        };

        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App, request.LinkType);
        Assert.Equal(_context.GenericString, request.FileName);
        Assert.Equal(_context.GenericString, request.Subdirectory);
        Assert.Equal(_context.GenericString, request.Subdirectory);
        Assert.Equal(_context.GenericString, request.DestinationAppId);
        Assert.Equal(_context.GenericTimeStamp, request.ExpirationTime);
        Assert.True(request.Overwrite);
        Assert.True(request.LeaveSourceFile);


        CheckProperties<MessageFormats.HostServices.Link.LinkRequest>(expectedProperties);
    }


    [Fact]
    public void LinkType_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Downlink", "Uplink", "Crosslink", "App2App", "Unknown" };
        CheckEnumerator<MessageFormats.HostServices.Link.LinkRequest.Types.LinkType>(possibleEnumValues);
    }

    [Fact]
    public void SensorsAvailableRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader" };

        CheckProperties<MessageFormats.HostServices.Sensor.SensorsAvailableRequest>(expectedProperties);
    }

    [Fact]
    public void SensorsAvailableResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "Sensors" };

        CheckProperties<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>(expectedProperties);
    }

    [Fact]
    public void TaskingPreCheckRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "SensorID", "RequestTime", "ExpirationTime", "RequestData" };

        CheckProperties<MessageFormats.HostServices.Sensor.TaskingPreCheckRequest>(expectedProperties);
    }

    [Fact]
    public void TaskingPreCheckResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "SensorID", "ResponseData" };

        CheckProperties<MessageFormats.HostServices.Sensor.TaskingPreCheckResponse>(expectedProperties);
    }

    [Fact]
    public void TaskingRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "SensorID", "RequestTime", "ExpirationTime", "RequestData" };

        CheckProperties<MessageFormats.HostServices.Sensor.TaskingRequest>(expectedProperties);
    }

    [Fact]
    public void TaskingResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "SensorID", "ResponseData" };
        CheckProperties<MessageFormats.HostServices.Sensor.TaskingResponse>(expectedProperties);
    }


    private static void CheckProperties<T>(List<string> expectedProperties) where T : IMessage, new() {
        T testMessage = new T();
        List<string> actualProperties = testMessage.Descriptor.Fields.InFieldNumberOrder().Select(field => field.PropertyName).ToList();

        Console.WriteLine($"......checking properties for {typeof(T)}");

        Console.WriteLine($".........expected properties: ({expectedProperties.Count}): {string.Join(",", expectedProperties)}");
        Console.WriteLine($".........actual properties: ({actualProperties.Count}): {string.Join(",", actualProperties)}");

        Assert.Equal(0, expectedProperties.Count(_prop => !actualProperties.Contains(_prop)));  // Check if there's any properties missing in the message
        Assert.Equal(0, actualProperties.Count(_prop => !expectedProperties.Contains(_prop)));  // Check if there's any properties we aren't expecting
    }

    private static void CheckEnumerator<T>(List<string> expectedEnumValues) where T : System.Enum {
        // Loop through and try to set all the enum values
        foreach (string enumValue in expectedEnumValues) {
            // This will throw a hard exception if we pass an item that doesn't work
            object? parsedEnum = System.Enum.Parse(typeof(T), enumValue);
            Assert.NotNull(parsedEnum);
        }

        // Make sure we don't have any extra values we didn't test
        int currentEnumCount = System.Enum.GetNames(typeof(T)).Length;

        Assert.Equal(expectedEnumValues.Count, currentEnumCount);
    }
}
