namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests;

/// <summary>
/// We only get one opportunity to build our client per deployment
/// This class allows us to instantiate and share the build context across
/// multiple test runs
/// </summary>
public class TestSharedContext : IDisposable {
    private static TestSharedContext TextContext { get; set; } = null!;
    private static bool IS_PROVISIONED = false;
    public static bool HEALTH_CHECK_RECEIVED = false;
    internal static TimeSpan MAX_TIMESPAN_TO_WAIT_FOR_MSG = TimeSpan.FromSeconds(90);
    public readonly string GenericGuid = Guid.NewGuid().ToString();
    public readonly int GenericInt = 12345;
    public readonly string GenericString = "Where's the kaboom?";
    public readonly MapField<string, string> GenericMetaData = new() { { "Marvin", "Martian" } };
    public readonly RepeatedField<string> GenericRepeatedString = new() { "Marvin", "Martian" };
    public readonly MessageFormats.Common.StatusCodes GenericStatus = MessageFormats.Common.StatusCodes.Ready;
    public readonly Timestamp GenericTimeStamp = Timestamp.FromDateTime(DateTime.MaxValue.ToUniversalTime());
    public readonly StringValue GenericStringProto = new() { Value = "Looney Tunes" };
    public readonly Any GenericAny = Any.Pack(new StringValue() { Value = "Looney Tunes" });

    /// <summary>
    /// Setup the SpaceFx Core to be shared across tests
    /// </summary>
    public TestSharedContext() {
        if (IS_PROVISIONED == true) return;

        Client.Build();
        Client.WaitForOnline(MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        Client.IsAppHealthy = () => {
            TestSharedContext.HEALTH_CHECK_RECEIVED = true;
            return true; // Assume the app is healthy
        };



        IS_PROVISIONED = true;

    }


    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition(nameof(TestSharedContext))]
public class TestSharedContextCollection : ICollectionFixture<TestSharedContext> {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}