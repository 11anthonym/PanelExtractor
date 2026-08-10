namespace PanelExtractor
{
    internal enum PanelFileKind
    {
        Other,
        File,
        Directory
    }

    internal readonly record struct PanelFileEntry(string Name, long Length, PanelFileKind Kind);

    internal interface IReadOnlyPanelFileClient : IDisposable
    {
        string ProtocolName { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task<bool> CanReadDirectoryAsync(string path, CancellationToken cancellationToken);

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

    internal sealed class PanelHostKeyException(string message, Exception? innerException = null)
        : PanelConnectionException(message, innerException)
    {
    }

    internal sealed class PanelTransportUnavailableException(string message, Exception innerException)
        : PanelConnectionException(message, innerException)
    {
    }
}
