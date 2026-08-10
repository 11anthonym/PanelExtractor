namespace PanelExtractor.Tests;

[TestClass]
public class PanelTransportSelectorTests
{
    private const string RequiredPath = "/display";

    [TestMethod]
    public async Task ConnectAsync_UsesSecureTransportWithoutCreatingFtpClient()
    {
        var sftp = new FakePanelFileClient("SFTP");
        bool ftpCreated = false;
        var selector = CreateSelector(
            SecureCandidate(() => sftp),
            LegacyCandidate(() =>
            {
                ftpCreated = true;
                return new FakePanelFileClient("FTP");
            })
        );

        using IReadOnlyPanelFileClient selected = await selector.ConnectAsync(
            RequiredPath,
            allowLegacyFtp: true,
            _ => { },
            CancellationToken.None
        );

        Assert.AreSame(sftp, selected);
        Assert.IsFalse(ftpCreated);
        Assert.AreEqual(1, sftp.DirectoryProbeCount);
    }

    [TestMethod]
    public async Task ConnectAsync_FallsBackToFtpWhenSftpIsUnavailable()
    {
        var sftp = new FakePanelFileClient("SFTP")
        {
            ConnectException = Unavailable("SFTP unavailable")
        };
        var ftp = new FakePanelFileClient("FTP");
        var selector = CreateSelector(SecureCandidate(() => sftp), LegacyCandidate(() => ftp));

        using IReadOnlyPanelFileClient selected = await selector.ConnectAsync(
            RequiredPath,
            allowLegacyFtp: true,
            _ => { },
            CancellationToken.None
        );

        Assert.AreSame(ftp, selected);
        Assert.IsTrue(sftp.WasDisposed);
    }

    [TestMethod]
    public async Task ConnectAsync_FallsBackWhenSftpCannotSeeDisplayDirectory()
    {
        var sftp = new FakePanelFileClient("SFTP") { CanReadRequiredPath = false };
        var ftp = new FakePanelFileClient("FTP");
        var selector = CreateSelector(SecureCandidate(() => sftp), LegacyCandidate(() => ftp));

        using IReadOnlyPanelFileClient selected = await selector.ConnectAsync(
            RequiredPath,
            allowLegacyFtp: true,
            _ => { },
            CancellationToken.None
        );

        Assert.AreSame(ftp, selected);
        Assert.IsTrue(sftp.WasDisposed);
    }

    [TestMethod]
    public async Task ConnectAsync_DoesNotCreateFtpClientWithoutPermission()
    {
        var sftp = new FakePanelFileClient("SFTP")
        {
            ConnectException = Unavailable("SFTP unavailable")
        };
        bool ftpCreated = false;
        var selector = CreateSelector(
            SecureCandidate(() => sftp),
            LegacyCandidate(() =>
            {
                ftpCreated = true;
                return new FakePanelFileClient("FTP");
            })
        );

        PanelConnectionException exception = await Assert.ThrowsExactlyAsync<PanelConnectionException>(
            () => selector.ConnectAsync(
                RequiredPath,
                allowLegacyFtp: false,
                _ => { },
                CancellationToken.None
            )
        );

        StringAssert.Contains(exception.Message, "Enable legacy FTP fallback");
        Assert.IsFalse(ftpCreated);
    }

    [TestMethod]
    public async Task ConnectAsync_DoesNotDowngradeAfterAuthenticationFailure()
    {
        var sftp = new FakePanelFileClient("SFTP")
        {
            ConnectException = new PanelAuthenticationException(
                "SFTP authentication failed.",
                new InvalidOperationException()
            )
        };
        bool ftpCreated = false;
        var selector = CreateSelector(
            SecureCandidate(() => sftp),
            LegacyCandidate(() =>
            {
                ftpCreated = true;
                return new FakePanelFileClient("FTP");
            })
        );

        await Assert.ThrowsExactlyAsync<PanelAuthenticationException>(
            () => selector.ConnectAsync(
                RequiredPath,
                allowLegacyFtp: true,
                _ => { },
                CancellationToken.None
            )
        );

        Assert.IsFalse(ftpCreated);
        Assert.IsTrue(sftp.WasDisposed);
    }

    [TestMethod]
    public async Task ConnectAsync_DoesNotDowngradeAfterHostKeyFailure()
    {
        var sftp = new FakePanelFileClient("SFTP")
        {
            ConnectException = new PanelHostKeyException("The SSH identity changed.")
        };
        bool ftpCreated = false;
        var selector = CreateSelector(
            SecureCandidate(() => sftp),
            LegacyCandidate(() =>
            {
                ftpCreated = true;
                return new FakePanelFileClient("FTP");
            })
        );

        await Assert.ThrowsExactlyAsync<PanelHostKeyException>(
            () => selector.ConnectAsync(
                RequiredPath,
                allowLegacyFtp: true,
                _ => { },
                CancellationToken.None
            )
        );

        Assert.IsFalse(ftpCreated);
        Assert.IsTrue(sftp.WasDisposed);
    }

    private static PanelTransportSelector CreateSelector(params PanelTransportCandidate[] candidates) =>
        new(candidates);

    private static PanelTransportCandidate SecureCandidate(Func<IReadOnlyPanelFileClient> factory) =>
        new(RequiresLegacyFtpPermission: false, factory);

    private static PanelTransportCandidate LegacyCandidate(Func<IReadOnlyPanelFileClient> factory) =>
        new(RequiresLegacyFtpPermission: true, factory);

    private static PanelTransportUnavailableException Unavailable(string message) =>
        new(message, new IOException(message));

    private sealed class FakePanelFileClient(string protocolName) : IReadOnlyPanelFileClient
    {
        public string ProtocolName { get; } = protocolName;

        public Exception? ConnectException { get; init; }

        public bool CanReadRequiredPath { get; init; } = true;

        public int DirectoryProbeCount { get; private set; }

        public bool WasDisposed { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ConnectException is null
                ? Task.CompletedTask
                : Task.FromException(ConnectException);
        }

        public Task<bool> CanReadDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            DirectoryProbeCount++;
            return Task.FromResult(CanReadRequiredPath);
        }

        public async IAsyncEnumerable<PanelFileEntry> ListDirectoryAsync(
            string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new PanelFileEntry("project.vtx", 1, PanelFileKind.File);
            await Task.CompletedTask;
        }

        public Task DownloadFileAsync(
            string path,
            Stream output,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public void Dispose() => WasDisposed = true;
    }
}
