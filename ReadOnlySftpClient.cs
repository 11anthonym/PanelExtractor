using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PanelExtractor
{
    // Keeps SSH.NET's mutation-capable types behind a read-only application surface.
    internal sealed class ReadOnlySftpClient : IReadOnlyPanelFileClient
    {
        private const int Port = 22;

        // Panels running on older, slower hardware can take several seconds to finish a key
        // exchange. A short budget here reports a healthy panel as unreachable.
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(20);

        private const string LockoutWarning =
            "Panels can lock the account and block this computer's IP address after a few " +
            "failed attempts, so check the credentials before trying again.";

        private readonly string host;
        private readonly Action<string> log;
        private readonly SftpClient client;
        private readonly SshHostKeyVerifier hostKeyVerifier;
        private PanelHostKeyException? hostKeyFailure;

        public string ProtocolName => "SFTP";

        public ReadOnlySftpClient(string host, string username, string password, Action<string> log)
            : this(host, username, password, log, SshHostKeyStore.Default)
        {
        }

        internal ReadOnlySftpClient(
            string host,
            string username,
            string password,
            Action<string> log,
            SshHostKeyStore hostKeys)
        {
            this.host = host;
            this.log = log;

            // Password is the only method offered on purpose. Adding keyboard-interactive as a
            // second method would make every rejected password cost two failed attempts against
            // a panel lockout counter, and no supported panel requires it.
            var connectionInfo = new ConnectionInfo(
                host,
                Port,
                username,
                new PasswordAuthenticationMethod(username, password)
            )
            {
                Timeout = HandshakeTimeout
            };

            connectionInfo.AuthenticationBanner += ReportBanner;

            client = new SftpClient(connectionInfo);
            hostKeyVerifier = new SshHostKeyVerifier(host, Port, hostKeys);
            client.HostKeyReceived += VerifyHostKey;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await client.ConnectAsync(cancellationToken);

                if (hostKeyFailure is not null)
                {
                    throw hostKeyFailure;
                }

                try
                {
                    hostKeyVerifier.Remember();
                }
                catch (Exception ex)
                {
                    throw new PanelHostKeyException(
                        "The SSH identity for this panel could not be saved safely. SFTP was stopped.",
                        ex);
                }
            }
            catch (Exception) when (hostKeyFailure is not null)
            {
                throw hostKeyFailure;
            }
            catch (SshAuthenticationException ex)
            {
                throw new PanelAuthenticationException(DescribeAuthenticationFailure(ex), ex);
            }
            catch (Exception ex) when (ex is SshException or SocketException or IOException or TimeoutException)
            {
                throw new PanelTransportUnavailableException("The SSH/SFTP service could not be used.", ex);
            }
        }

        private static string DescribeAuthenticationFailure(SshAuthenticationException failure)
        {
            // SSH.NET names the methods the panel offered when none of ours applied. A panel
            // that will not take a password is a configuration this tool cannot satisfy, and
            // saying so beats reporting it as a rejected password.
            if (failure.Message.Contains("No suitable authentication method", StringComparison.Ordinal))
            {
                return "The panel would not accept a password sign-in, which is the only method " +
                    $"this application supports. The panel reported: {failure.Message}";
            }

            return $"SFTP authentication failed. {LockoutWarning}";
        }

        private void ReportBanner(object? sender, AuthenticationBannerEventArgs e)
        {
            // Enterprise and government deployments configure a login banner that is meant to be
            // shown to whoever signs in, so record it rather than discarding it.
            string[] lines = e.BannerMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                log($"Panel login banner: {line.Trim()}");
            }
        }

        private void VerifyHostKey(object? sender, HostKeyEventArgs e)
        {
            try
            {
                if (!hostKeyVerifier.CanTrust(e.FingerPrintSHA256))
                {
                    hostKeyFailure = new PanelHostKeyException(
                        host,
                        Port,
                        hostKeyVerifier.SavedFingerprint!,
                        hostKeyVerifier.PresentedFingerprint!);
                }
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                hostKeyFailure = new PanelHostKeyException(
                    "The SSH identity for this panel could not be verified. SFTP was stopped.",
                    ex);
            }

            e.CanTrust = hostKeyFailure is null;
        }

        public async Task<PanelDirectoryAccess> CheckDirectoryAccessAsync(
            string path,
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (PanelFileEntry _ in ListDirectoryAsync(path, cancellationToken))
                {
                    break;
                }

                return PanelDirectoryAccess.Readable;
            }
            catch (SftpPathNotFoundException)
            {
                return PanelDirectoryAccess.NotFound;
            }
            catch (SftpPermissionDeniedException)
            {
                return PanelDirectoryAccess.PermissionDenied;
            }
        }

        public async IAsyncEnumerable<PanelFileEntry> ListDirectoryAsync(
            string path,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in client.ListDirectoryAsync(path, cancellationToken))
            {
                yield return new PanelFileEntry(
                    item.Name,
                    item.Length,
                    item.IsDirectory
                        ? PanelFileKind.Directory
                        : item.IsRegularFile
                            ? PanelFileKind.File
                            : PanelFileKind.Other
                );
            }
        }

        public Task DownloadFileAsync(string path, Stream output, CancellationToken cancellationToken) =>
            client.DownloadFileAsync(path, output, cancellationToken);

        public void Dispose() => client.Dispose();
    }
}
