namespace Microsoft.Azure.SpaceFx.SDK.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class LinkTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    private readonly string TEST_SUB_DIR = "test_directory";
    private readonly string TEST_FILE = "/workspace/spacesdk-client/test/sampleData/astronaut.jpg";
    private readonly string TARGET_SERVICE_APP_ID = $"hostsvc-{MessageFormats.Common.HostServices.Link}".ToLower();
    private string INBOX_DIRECTORY = "";
    private string OUTBOX_DIRECTORY = "";
    public LinkTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void SendFile() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Link.LinkResponse? linkResponse = null;

        PrepInboxAndOutboxDirectories();

        Console.WriteLine($"Sending file '{TEST_FILE}'...");

        Task.Run(async () => {
            linkResponse = await Link.SendFileToApp(destinationAppId: Client.APP_ID, file: TEST_FILE, overwriteDestinationFile: true);
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (linkResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (linkResponse == null) throw new TimeoutException($"Failed to hear {nameof(linkResponse)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(linkResponse)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, linkResponse.ResponseHeader.Status);

        Console.WriteLine($"Waiting for file '{Path.GetFileName(TEST_FILE)}' to appear in inbox...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (!File.Exists(Path.Join(INBOX_DIRECTORY, Path.GetFileName(TEST_FILE))) && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (!File.Exists(Path.Join(INBOX_DIRECTORY, Path.GetFileName(TEST_FILE)))) throw new TimeoutException($"Failed to receive file '{Path.Join(INBOX_DIRECTORY, Path.GetFileName(TEST_FILE))}' after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Assert.True(File.Exists(Path.Join(INBOX_DIRECTORY, Path.GetFileName(TEST_FILE))));

        Console.WriteLine($"File '{Path.GetFileName(TEST_FILE)}' found in inbox.");

        SendFileToSubdirectory();
    }

    private void SendFileToSubdirectory() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Link.LinkResponse? linkResponse = null;

        PrepInboxAndOutboxDirectories();

        Console.WriteLine($"Sending file '{Path.Join(TEST_SUB_DIR, Path.GetFileName(TEST_FILE))}'...");

        Task.Run(async () => {
            MessageFormats.HostServices.Link.LinkRequest linkRequest = new() {
                RequestHeader = new() { },
                LinkType = MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App,
                DestinationAppId = Client.APP_ID,
                FileName = Path.GetFileName(TEST_FILE),
                Subdirectory = TEST_SUB_DIR
            };
            linkResponse = await Link.SendLinkRequest(linkRequest, TEST_FILE);
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (linkResponse == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (linkResponse == null) throw new TimeoutException($"Failed to hear {nameof(linkResponse)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Console.WriteLine($"Received {nameof(linkResponse)}  from '{TARGET_SERVICE_APP_ID}'");
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful, linkResponse.ResponseHeader.Status);

        Console.WriteLine($"Waiting for file '{Path.Join(INBOX_DIRECTORY, TEST_SUB_DIR, Path.GetFileName(TEST_FILE))}' to appear in inbox...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (!File.Exists(Path.Join(INBOX_DIRECTORY, TEST_SUB_DIR, Path.GetFileName(TEST_FILE))) && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (!File.Exists(Path.Join(INBOX_DIRECTORY, TEST_SUB_DIR, Path.GetFileName(TEST_FILE)))) throw new TimeoutException($"Failed to receive file '{Path.Join(INBOX_DIRECTORY, Path.GetFileName(TEST_FILE))}' after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TARGET_SERVICE_APP_ID} is deployed");

        Assert.True(File.Exists(Path.Join(INBOX_DIRECTORY, TEST_SUB_DIR, Path.GetFileName(TEST_FILE))));

        Console.WriteLine($"File '{Path.Join(INBOX_DIRECTORY, TEST_SUB_DIR, Path.GetFileName(TEST_FILE))}' found in inbox.");
    }

    private void PrepInboxAndOutboxDirectories() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        Console.WriteLine($"Querying inbox and outbox directory locations...");
        Task.Run(async () => {
            var xfer_directories = await Core.GetXFerDirectories();
            INBOX_DIRECTORY = xfer_directories.inbox_directory;
            OUTBOX_DIRECTORY = xfer_directories.outbox_directory;
        });

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        while (string.IsNullOrEmpty(INBOX_DIRECTORY) && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (string.IsNullOrEmpty(INBOX_DIRECTORY)) throw new TimeoutException($"Failed to calculate INBOX_DIRECTORY after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please retry");
        if (string.IsNullOrEmpty(OUTBOX_DIRECTORY)) throw new TimeoutException($"Failed to calculate outbox_directory after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please retry");

        Console.WriteLine($"Cleaning inbox directory '{INBOX_DIRECTORY}'...");
        Directory.GetFiles(INBOX_DIRECTORY).ToList().ForEach(file => File.Delete(file)); ;
        Directory.GetDirectories(INBOX_DIRECTORY).ToList().ForEach(dir => Directory.Delete(dir, true));

        Console.WriteLine($"Cleaning outbox directory '{OUTBOX_DIRECTORY}'...");
        Directory.GetFiles(OUTBOX_DIRECTORY).ToList().ForEach(file => File.Delete(file)); ;
        Directory.GetDirectories(OUTBOX_DIRECTORY).ToList().ForEach(dir => Directory.Delete(dir, true));

        Console.WriteLine("Prepped inbox and outbox directories.");
    }
}
