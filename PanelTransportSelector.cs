namespace PanelExtractor
{
    internal readonly record struct PanelTransportCandidate(
        bool RequiresLegacyFtpPermission,
        Func<IReadOnlyPanelFileClient> CreateClient
    );

    internal sealed class PanelTransportSelector
    {
        private readonly IReadOnlyList<PanelTransportCandidate> candidates;

        internal PanelTransportSelector(IEnumerable<PanelTransportCandidate> candidates)
        {
            this.candidates = candidates.ToArray();

            if (this.candidates.Count == 0)
            {
                throw new ArgumentException("At least one transport candidate is required.", nameof(candidates));
            }
        }

        public static PanelTransportSelector CreateDefault(
            string host,
            string username,
            string password)
        {
            return new PanelTransportSelector(
            [
                new PanelTransportCandidate(
                    RequiresLegacyFtpPermission: false,
                    () => new ReadOnlySftpClient(host, username, password)
                ),
                new PanelTransportCandidate(
                    RequiresLegacyFtpPermission: true,
                    () => new ReadOnlyFtpClient(host, username, password)
                )
            ]);
        }

        public async Task<IReadOnlyPanelFileClient> ConnectAsync(
            string requiredPath,
            bool allowLegacyFtp,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            PanelTransportUnavailableException? lastUnavailableException = null;

            foreach (PanelTransportCandidate candidate in candidates)
            {
                if (candidate.RequiresLegacyFtpPermission && !allowLegacyFtp)
                {
                    log("Legacy FTP fallback is disabled; FTP was not attempted.");
                    continue;
                }

                IReadOnlyPanelFileClient client = candidate.CreateClient();
                bool selected = false;

                try
                {
                    log($"Trying {client.ProtocolName}.");
                    await client.ConnectAsync(cancellationToken);

                    if (!await client.CanReadDirectoryAsync(requiredPath, cancellationToken))
                    {
                        log($"{client.ProtocolName} connected, but {requiredPath} could not be read.");
                        continue;
                    }

                    selected = true;
                    log($"Selected {client.ProtocolName}; read access to {requiredPath} was verified.");
                    return client;
                }
                catch (PanelTransportUnavailableException ex)
                {
                    lastUnavailableException = ex;
                    log($"{client.ProtocolName} is unavailable: {ex.Message}");
                }
                finally
                {
                    if (!selected)
                    {
                        client.Dispose();
                    }
                }
            }

            string message = allowLegacyFtp
                ? $"Neither SFTP nor FTP could read {requiredPath}."
                : $"SFTP could not read {requiredPath}. Enable legacy FTP fallback only if this panel requires unencrypted FTP.";

            throw new PanelConnectionException(message, lastUnavailableException);
        }
    }
}
