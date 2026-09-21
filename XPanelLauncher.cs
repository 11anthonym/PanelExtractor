using System.Diagnostics;
using Microsoft.Win32;

namespace PanelExtractor
{
    internal enum XPanelLaunchOutcome
    {
        Started,
        NotInstalled,
        Failed
    }

    internal readonly record struct XPanelLaunchResult(XPanelLaunchOutcome Outcome, string Message);

    // A VTZ archive is only meaningful to Crestron XPanel, so this looks for that one program
    // rather than handing the file to the shell. Installing XPanel lists it under the extension's
    // OpenWithProgIds but sets no default handler, so .vtz still has no association for the shell
    // to act on. Starting the program directly matches the command that association would run:
    // "CrestronXPanel.exe" "%1".
    internal sealed class XPanelLauncher
    {
        public const string DownloadUrl = "https://www.crestron.com/Resources/XPanel-Desktop-Installer";

        private const string ExecutableName = "CrestronXpanel.exe";

        private static readonly string[] AppPathKeys =
        [
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{ExecutableName}",
            $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\{ExecutableName}"
        ];

        private readonly IReadOnlyList<string> candidatePaths;
        private readonly Func<string, bool> fileExists;
        private readonly Action<string, string> startProcess;

        internal XPanelLauncher(
            IEnumerable<string> candidatePaths,
            Func<string, bool> fileExists,
            Action<string, string> startProcess)
        {
            this.candidatePaths = candidatePaths.ToArray();
            this.fileExists = fileExists;
            this.startProcess = startProcess;
        }

        public static XPanelLauncher CreateDefault() =>
            new(DefaultCandidatePaths(), File.Exists, Start);

        public string? FindInstalledExecutable() =>
            candidatePaths.FirstOrDefault(path => fileExists(path));

        public XPanelLaunchResult Launch(string vtzPath)
        {
            string? executable = FindInstalledExecutable();

            if (executable is null)
            {
                return new XPanelLaunchResult(
                    XPanelLaunchOutcome.NotInstalled,
                    "Crestron XPanel was not found on this computer.");
            }

            try
            {
                startProcess(executable, vtzPath);
                return new XPanelLaunchResult(
                    XPanelLaunchOutcome.Started,
                    $"Crestron XPanel was started from {executable}.");
            }
            catch (Exception ex) when (
                ex is System.ComponentModel.Win32Exception or InvalidOperationException or
                IOException or UnauthorizedAccessException)
            {
                return new XPanelLaunchResult(
                    XPanelLaunchOutcome.Failed,
                    $"Crestron XPanel could not be started: {ex.Message}");
            }
        }

        // The installer has shipped under both program folders across versions, so check each
        // rather than assuming one, then fall back to whatever the installer registered.
        internal static IEnumerable<string> DefaultCandidatePaths()
        {
            string[] roots =
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            ];

            foreach (string root in roots)
            {
                if (!string.IsNullOrEmpty(root))
                {
                    yield return Path.Combine(root, "Crestron", "XPanel", "CrestronXPanel", ExecutableName);
                }
            }

            foreach (string registered in RegisteredExecutablePaths())
            {
                yield return registered;
            }
        }

        private static IEnumerable<string> RegisteredExecutablePaths()
        {
            foreach (string keyPath in AppPathKeys)
            {
                string? registered;

                try
                {
                    using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
                    registered = key?.GetValue(null) as string;
                }
                catch (Exception ex) when (
                    ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(registered))
                {
                    yield return registered.Trim('"');
                }
            }
        }

        private static void Start(string executable, string vtzPath)
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty
            };

            startInfo.ArgumentList.Add(vtzPath);

            using Process? process = Process.Start(startInfo);
        }
    }
}
