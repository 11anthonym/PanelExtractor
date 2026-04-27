using Renci.SshNet;

namespace CrestronPanelExtractor
{
    public partial class MainForm : Form
    {
        private const string RemoteDisplayPath = "/display";
        private bool showDebugLog = true;

        public MainForm()
        {
            InitializeComponent();
            ApplyDebugVisibility();
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

                connectionInfo.Timeout = TimeSpan.FromSeconds(10);

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
    }
}