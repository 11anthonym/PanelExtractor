namespace PanelExtractor.Tests;

[TestClass]
public class ReadOnlyFtpClientIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task Client_ListsAndDownloadsFromLoopbackFtpServer()
    {
        string? host = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_FTP_TEST_HOST");
        string? portText = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_FTP_TEST_PORT");
        string? username = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_FTP_TEST_USERNAME");
        string? password = Environment.GetEnvironmentVariable("PANEL_EXTRACTOR_FTP_TEST_PASSWORD");

        if (host is null || portText is null || username is null || password is null)
        {
            Assert.Inconclusive(
                "Set the PANEL_EXTRACTOR_FTP_TEST_* variables to run the loopback FTP integration test.");
        }

        using var client = new ReadOnlyFtpClient(host, username, password, int.Parse(portText));
        await client.ConnectAsync(CancellationToken.None);

        Assert.AreEqual(
            PanelDirectoryAccess.Readable,
            await client.CheckDirectoryAccessAsync("/display", CancellationToken.None));
        Assert.AreEqual(
            PanelDirectoryAccess.NotFound,
            await client.CheckDirectoryAccessAsync("/missing", CancellationToken.None));

        var entries = new List<PanelFileEntry>();
        await foreach (PanelFileEntry entry in client.ListDirectoryAsync("/display", CancellationToken.None))
        {
            entries.Add(entry);
        }

        PanelFileEntry file = entries.Single(entry => entry.Name == "sample.vtx");
        Assert.AreEqual(PanelFileKind.File, file.Kind);

        using var output = new MemoryStream();
        await client.DownloadFileAsync("/display/sample.vtx", output, CancellationToken.None);

        Assert.AreEqual(
            "loopback ftp qa",
            System.Text.Encoding.UTF8.GetString(output.ToArray()).TrimEnd('\r', '\n')
        );
    }
}
