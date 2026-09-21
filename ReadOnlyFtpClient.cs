using System.Net.Sockets;
using System.Runtime.CompilerServices;
using FluentFTP;
using FluentFTP.Exceptions;

namespace PanelExtractor
{
    // Keeps FluentFTP's mutation-capable types behind a read-only application surface.
    internal sealed class ReadOnlyFtpClient : IReadOnlyPanelFileClient
    {
        private const int TimeoutMilliseconds = 15000;

        private readonly AsyncFtpClient client;

        public string ProtocolName => "FTP (unencrypted)";

        public ReadOnlyFtpClient(string host, string username, string password, int port = 21)
        {
            var config = new FtpConfig
            {
                ConnectTimeout = TimeoutMilliseconds,
                ReadTimeout = TimeoutMilliseconds,
                DataConnectionConnectTimeout = TimeoutMilliseconds,
                DataConnectionReadTimeout = TimeoutMilliseconds,
                DataConnectionType = FtpDataConnectionType.AutoPassive,
                EncryptionMode = FtpEncryptionMode.None
            };

            client = new AsyncFtpClient(host, username, password, port, config);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await client.Connect(cancellationToken);
            }
            catch (FtpAuthenticationException ex)
            {
                throw new PanelAuthenticationException(
                    "FTP authentication failed. Panels can lock the account and block this " +
                    "computer's IP address after a few failed attempts, so check the " +
                    "credentials before trying again.",
                    ex);
            }
            catch (Exception ex) when (ex is FtpException or SocketException or IOException or TimeoutException)
            {
                throw new PanelTransportUnavailableException("The legacy FTP service could not be used.", ex);
            }
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
            catch (FtpCommandException)
            {
                // FTP answers both "no such folder" and "not allowed" with the same 550 reply,
                // so this cannot be reported as a privilege problem the way SFTP can.
                return PanelDirectoryAccess.NotFound;
            }
        }

        public async IAsyncEnumerable<PanelFileEntry> ListDirectoryAsync(
            string path,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            FtpListItem[] items = await client.GetListing(path, cancellationToken);
            foreach (FtpListItem item in items)
            {
                yield return new PanelFileEntry(
                    item.Name,
                    item.Size,
                    item.Type switch
                    {
                        FtpObjectType.Directory => PanelFileKind.Directory,
                        FtpObjectType.File => PanelFileKind.File,
                        _ => PanelFileKind.Other
                    }
                );
            }
        }

        public async Task DownloadFileAsync(
            string path,
            Stream output,
            CancellationToken cancellationToken)
        {
            bool downloaded = await client.DownloadStream(
                output,
                path,
                token: cancellationToken
            );

            if (!downloaded)
            {
                throw new IOException($"FTP did not download the requested file: {path}");
            }
        }

        public void Dispose() => client.Dispose();
    }
}
