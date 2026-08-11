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

        private readonly SftpClient client;
        private readonly SshHostKeyVerifier hostKeyVerifier;
        private PanelHostKeyException? hostKeyFailure;

        public string ProtocolName => "SFTP";

        public ReadOnlySftpClient(string host, string username, string password)
            : this(host, username, password, SshHostKeyStore.Default)
        {
        }

        internal ReadOnlySftpClient(
            string host,
            string username,
            string password,
            SshHostKeyStore hostKeys)
        {
            var connectionInfo = new ConnectionInfo(
                host,
                Port,
                username,
                new PasswordAuthenticationMethod(username, password)
            )
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

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
                throw new PanelAuthenticationException("SFTP authentication failed.", ex);
            }
            catch (Exception ex) when (ex is SshException or SocketException or IOException or TimeoutException)
            {
                throw new PanelTransportUnavailableException("The SSH/SFTP service could not be used.", ex);
            }
        }

        private void VerifyHostKey(object? sender, HostKeyEventArgs e)
        {
            try
            {
                if (!hostKeyVerifier.CanTrust(e.FingerPrintSHA256))
                {
                    hostKeyFailure = new PanelHostKeyException(
                        "The SSH identity for this panel has changed. SFTP was stopped. " +
                        "If the panel was replaced or reset, use File > Forget Saved SSH Host Keys and try again.");
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

        public async Task<bool> CanReadDirectoryAsync(
            string path,
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (PanelFileEntry _ in ListDirectoryAsync(path, cancellationToken))
                {
                    break;
                }

                return true;
            }
            catch (SftpPathNotFoundException)
            {
                return false;
            }
            catch (SftpPermissionDeniedException)
            {
                return false;
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
