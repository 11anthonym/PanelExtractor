namespace PanelExtractor
{
    internal readonly record struct PanelTransportCandidate(
        bool RequiresLegacyFtpPermission,
        Func<Action<string>, IReadOnlyPanelFileClient> CreateClient
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
                    log => new ReadOnlySftpClient(host, username, password, log)
                ),
                new PanelTransportCandidate(
                    RequiresLegacyFtpPermission: true,
                    _ => new ReadOnlyFtpClient(host, username, password)
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

                IReadOnlyPanelFileClient client = candidate.CreateClient(log);
                bool selected = false;

                try
                {
                    log($"Trying {client.ProtocolName}.");
                    await client.ConnectAsync(cancellationToken);

                    PanelDirectoryAccess access = await client.CheckDirectoryAccessAsync(
                        requiredPath,
                        cancellationToken);

                    if (access == PanelDirectoryAccess.PermissionDenied)
                    {
                        // The credentials were good, so another transport would fail the same
                        // way and would spend a second login attempt against the panel.
                        log($"{client.ProtocolName} signed in, but {requiredPath} is not readable by this account.");

                        throw new PanelAuthorizationException(
                            $"The panel accepted these credentials, but this account cannot read {requiredPath}. " +
                            "Use an account in the panel's Administrators group.");
                    }

                    if (access == PanelDirectoryAccess.NotFound)
                    {
                        log($"{client.ProtocolName} connected, but {requiredPath} could not be found.");
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
                : $"SFTP could not read {requiredPath}. Legacy FTP fallback only helps an older " +
                  "panel that has authentication turned off; panels with authentication on, and " +
                  "the newest panels, do not run an FTP server at all.";

            throw new PanelConnectionException(message, lastUnavailableException);
        }
    }
}
