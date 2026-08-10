using System.Text.Json;

namespace PanelExtractor
{
    internal enum SshHostKeyStatus
    {
        Unknown,
        Trusted,
        Changed
    }

    internal sealed class SshHostKeyStore
    {
        // Keep the existing storage ID so upgrades retain trusted host keys.
        private const string ApplicationStorageId = "CrestronPanelExtractor";

        private readonly string filePath;
        private readonly object syncRoot = new();

        public static SshHostKeyStore Default { get; } = new(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationStorageId,
                "ssh-host-keys.json"));

        internal SshHostKeyStore(string filePath)
        {
            this.filePath = Path.GetFullPath(filePath);
        }

        public SshHostKeyStatus Check(string host, int port, string fingerprint)
        {
            lock (syncRoot)
            {
                Dictionary<string, string> keys = Load();
                if (!keys.TryGetValue(CreateHostId(host, port), out string? savedFingerprint))
                {
                    return SshHostKeyStatus.Unknown;
                }

                return string.Equals(savedFingerprint, fingerprint, StringComparison.Ordinal)
                    ? SshHostKeyStatus.Trusted
                    : SshHostKeyStatus.Changed;
            }
        }

        public void Remember(string host, int port, string fingerprint)
        {
            lock (syncRoot)
            {
                Dictionary<string, string> keys = Load();
                string hostId = CreateHostId(host, port);

                if (keys.TryGetValue(hostId, out string? savedFingerprint) &&
                    !string.Equals(savedFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("The SSH host key changed while connecting.");
                }

                keys[hostId] = fingerprint;
                Save(keys);
            }
        }

        public bool Clear()
        {
            lock (syncRoot)
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                File.Delete(filePath);
                return true;
            }
        }

        private Dictionary<string, string> Load()
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                Dictionary<string, string>? keys = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(filePath));

                return keys is null
                    ? throw new InvalidDataException("The saved SSH host keys are invalid.")
                    : new Dictionary<string, string>(keys, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                throw new InvalidDataException("The saved SSH host keys are invalid.", ex);
            }
        }

        private void Save(Dictionary<string, string> keys)
        {
            string directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);

            string tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tempPath, JsonSerializer.Serialize(keys));
                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, null);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static string CreateHostId(string host, int port) =>
            $"{host.Trim().ToUpperInvariant()}:{port}";
    }

    internal sealed class SshHostKeyVerifier(
        string host,
        int port,
        SshHostKeyStore store)
    {
        private string? pendingFingerprint;

        public bool CanTrust(string fingerprint)
        {
            if (pendingFingerprint is not null)
            {
                return string.Equals(pendingFingerprint, fingerprint, StringComparison.Ordinal);
            }

            SshHostKeyStatus status = store.Check(host, port, fingerprint);
            if (status == SshHostKeyStatus.Unknown)
            {
                pendingFingerprint = fingerprint;
            }

            return status != SshHostKeyStatus.Changed;
        }

        public void Remember()
        {
            if (pendingFingerprint is null)
            {
                return;
            }

            store.Remember(host, port, pendingFingerprint);
            pendingFingerprint = null;
        }
    }
}
