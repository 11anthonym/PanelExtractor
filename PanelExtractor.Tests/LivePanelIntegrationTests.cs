namespace PanelExtractor.Tests;

[TestClass]
public class LivePanelIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task Selector_UsesSftpToReadDisplayDirectory()
    {
        string? host = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_PANEL_TEST_HOST");
        string? username = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_PANEL_TEST_USERNAME");
        string? password = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_PANEL_TEST_PASSWORD");

        if (host is null || username is null || password is null)
        {
            Assert.Inconclusive(
                "Set the PANEL_EXTRACTOR_PANEL_TEST_* variables to run the live panel test.");
        }

        var log = new List<string>();
        var selector = PanelTransportSelector.CreateDefault(host, username, password);

        using IReadOnlyPanelFileClient client = await selector.ConnectAsync(
            "/display",
            allowLegacyFtp: false,
            log.Add,
            CancellationToken.None);

        Assert.AreEqual("SFTP", client.ProtocolName);
        CollectionAssert.Contains(
            log,
            "Selected SFTP; read access to /display was verified.");
    }
}
