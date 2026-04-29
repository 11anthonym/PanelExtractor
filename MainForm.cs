using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace CrestronPanelExtractor
{
    public partial class MainForm : Form
    {
        private const string RemoteDisplayPath = "/display";
        private bool showDebugLog = false;
        private bool ProgramDebug = true;

        public MainForm()
        {
            InitializeComponent();
            ApplyDebugVisibility();
        }

        private bool LooksLikeIpv4Address(string value)
        {
            return Regex.IsMatch(value, @"^[0-9.]+$");
        }

        private bool IsValidIpv4Address(string value)
        {
            // Try to parse the string into an IPAddress object.
            // If parsing fails, the value is not a valid IP address at all.
            if (!IPAddress.TryParse(value, out IPAddress? parsedAddress))
            {
                return false;
            }

            // Make sure the parsed address is IPv4.
            // AddressFamily.InterNetwork means IPv4.
            // IPv6 would be AddressFamily.InterNetworkV6.
            if (parsedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            // Require the normal dotted-quad IPv4 format:
            // four sections separated by dots, like 192.168.1.50.
            // This helps reject odd formats that IPAddress.TryParse may otherwise accept.
            if (value.Split('.').Length != 4)
            {
                return false;
            }

            // If all checks passed, treat it as a valid IPv4 address.
            return true;
        }
        private void ApplyDebugVisibility()
        {
            txtLog.Visible = showDebugLog;
            lblLog.Visible = showDebugLog;
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();

            dialog.Description = "Select where the extracted VTZ file should be saved";
            dialog.UseDescriptionForTitle = true;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dialog.SelectedPath = desktopPath;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtOutputFolder.Text = dialog.SelectedPath;
                AppendLog($"Output folder selected: {dialog.SelectedPath}");
            }
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Enter a username.", "Missing Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter a password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                AppendLog($"Testing SFTP connection to {host}...");

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                sftp.Connect();

                bool displayFolderExists = sftp.Exists(RemoteDisplayPath);

                sftp.Disconnect();

                if (displayFolderExists)
                {
                    MessageBox.Show("Connection successful. /display folder found.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendLog("Connection successful. /display folder found.");
                }
                else
                {
                    MessageBox.Show("Connection successful, but /display folder was not found.", "Folder Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AppendLog("Connection successful, but /display folder was not found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed:{Environment.NewLine}{ex.Message}", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"Connection failed: {ex.Message}");
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            if (ProgramDebug)
            {
                Debug.WriteLine("Extract Button Clicked");
            }

            string host = txtHost.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if(string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LooksLikeIpv4Address(host) && !IsValidIpv4Address(host))
            {
                MessageBox.Show(
                    "The value entered looks like an IPv4 address, but it is not valid.", "Invalid IP Address",MessageBoxButtons.OK,MessageBoxIcon.Warning
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Enter a username.", "Missing username.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter a password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Listing {RemoteDisplayPath} on {host}...");

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                sftp.Connect();

                var items = sftp.ListDirectory(RemoteDisplayPath)
                    .Where(item => item.Name != "." && item.Name != "..")
                    .ToList();

                sftp.Disconnect();

                Debug.WriteLine($"Found {items.Count} item(s) in {RemoteDisplayPath}:");

                foreach (var item in items)
                {
                    Debug.WriteLine($"- {item.Name}");
                }

                MessageBox.Show(
                    $"Found {items.Count} item(s) in {RemoteDisplayPath}.",
                    "Directory Listed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Extract test failed:{Environment.NewLine}{ex.Message}",
                    "Extract Test Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}