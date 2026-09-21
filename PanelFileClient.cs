namespace PanelExtractor
{
    internal enum PanelFileKind
    {
        Other,
        File,
        Directory
    }

    // Separates "this panel does not expose the folder" from "this account may not read it",
    // because only the first is a reason to try another transport.
    internal enum PanelDirectoryAccess
    {
        Readable,
        NotFound,
        PermissionDenied
    }

    internal readonly record struct PanelFileEntry(string Name, long Length, PanelFileKind Kind);

    internal interface IReadOnlyPanelFileClient : IDisposable
    {
        string ProtocolName { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task<PanelDirectoryAccess> CheckDirectoryAccessAsync(
            string path,
            CancellationToken cancellationToken
        );

        IAsyncEnumerable<PanelFileEntry> ListDirectoryAsync(
            string path,
            CancellationToken cancellationToken
        );

        Task DownloadFileAsync(
            string path,
            Stream output,
            CancellationToken cancellationToken
        );
    }

    internal class PanelConnectionException(string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
    }

    internal sealed class PanelAuthenticationException(string message, Exception innerException)
        : PanelConnectionException(message, innerException)
    {
    }

    // The credentials were accepted but the account cannot read the project folder.
    // Retrying on another transport would only spend a second login attempt.
    internal sealed class PanelAuthorizationException(string message)
        : PanelConnectionException(message)
    {
    }

    internal sealed class PanelHostKeyException : PanelConnectionException
    {
        public string? Host { get; }

        public int Port { get; }

        public string? SavedFingerprint { get; }

        public string? PresentedFingerprint { get; }

        public bool IsIdentityChange =>
            Host is not null && SavedFingerprint is not null && PresentedFingerprint is not null;

        public PanelHostKeyException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public PanelHostKeyException(
            string host,
            int port,
            string savedFingerprint,
            string presentedFingerprint)
            : base($"The SSH identity for {host} has changed. SFTP was stopped.")
        {
            Host = host;
            Port = port;
            SavedFingerprint = savedFingerprint;
            PresentedFingerprint = presentedFingerprint;
        }
    }

    internal sealed class PanelTransportUnavailableException(string message, Exception innerException)
        : PanelConnectionException(message, innerException)
    {
    }
}
