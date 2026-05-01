using System.Diagnostics;
using System.IO.Compression;
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
        private bool programDebug = true;

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

        private void SetBusyState(bool isBusy)
        {
            btnExtract.Enabled = !isBusy;
            btnTestConnection.Enabled = !isBusy;
            btnBrowseOutput.Enabled = !isBusy;

            Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
        }

        private string GetUniqueFilePath(string folder, string fileNameWithoutExtension, string extension)
        {
            string filePath = Path.Combine(folder, fileNameWithoutExtension + extension);
            int counter = 1;

            while (File.Exists(filePath))
            {
                filePath = Path.Combine(folder, $"{fileNameWithoutExtension}_{counter}{extension}");
                counter++;
            }

            return filePath;
        }

        private string CreateTempExtractionFolder()
        {
            string tempFolder = Path.Combine(
                Path.GetTempPath(),
                "CrestronPanelExtractor",
                Guid.NewGuid().ToString()
            );

            Directory.CreateDirectory(tempFolder);

            return tempFolder;
        }

        private void DownloadRemoteDirectory(SftpClient sftp, string remoteDirectory, string localDirectory)
        {
            Directory.CreateDirectory(localDirectory);

            var items = sftp.ListDirectory(remoteDirectory)
                .Where(item => item.Name != "." && item.Name != "..")
                .ToList();

            foreach (var item in items)
            {
                string remotePath = $"{remoteDirectory.TrimEnd('/')}/{item.Name}";
                string localPath = Path.Combine(localDirectory, item.Name);

                if (item.IsDirectory)
                {
                    DownloadRemoteDirectory(sftp, remotePath, localPath);
                }
                else if (item.IsRegularFile)
                {
                    if (programDebug)
                    {
                        Debug.WriteLine($"Downloading {remotePath}");
                    }

                    using FileStream fileStream = File.Create(localPath);
                    sftp.DownloadFile(remotePath, fileStream);
                }
            }
        }

        private string CreateVtzArchive(string tempExtractionFolder, string outputFolder)
        {
            string[] vtxFiles = Directory.GetFiles(tempExtractionFolder, "*.vtx", SearchOption.TopDirectoryOnly);

            if (vtxFiles.Length == 0)
            {
                throw new InvalidOperationException("No .vtx file was found in the downloaded display folder.");
            }

            if (vtxFiles.Length > 1)
            {
                throw new InvalidOperationException("Multiple .vtx files were found in the downloaded /display folder.");
            }

            string vtxFile = vtxFiles[0];
            string projectName = Path.GetFileNameWithoutExtension(vtxFile);
            string outputVtzPath = GetUniqueFilePath(outputFolder, projectName, ".vtz");

            ZipFile.CreateFromDirectory(
                tempExtractionFolder,
                outputVtzPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false
            );

            return outputVtzPath;
        }

        // Button clicks
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
                SetStatus("Missing host or IP address.");
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LooksLikeIpv4Address(host) && !IsValidIpv4Address(host))
            {
                SetStatus("Invalid IP address.");
                MessageBox.Show(
                    "The value entered looks like an IPv4 address, but it is not valid.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatus("Missing username.");
                MessageBox.Show("Enter a username.", "Missing Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Missing password.");
                MessageBox.Show("Enter a password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusyState(true);
                SetStatus("Preparing connection test...");

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                SetStatus("Connecting to touch panel...");
                sftp.Connect();

                SetStatus("Checking for touch panel file contents...");
                bool displayFolderExists = sftp.Exists(RemoteDisplayPath);

                sftp.Disconnect();

                if (displayFolderExists)
                {
                    SetStatus("Connection successful. Touch panel files found.");

                    MessageBox.Show(
                        "Connection successful. Expected touch panel file contents found.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    SetStatus("Connected, but touch panel files were not found.");

                    MessageBox.Show(
                        "Connection successful, but expected touch panel file contents were not found.",
                        "Folder Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                SetStatus("Connection test failed.");

                MessageBox.Show(
                    $"Connection failed:{Environment.NewLine}{ex.Message}",
                    "Connection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            if (programDebug)
            {
                Debug.WriteLine("Extract Button Clicked");
            }

            string host = txtHost.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string? tempExtractionFolder = null;

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LooksLikeIpv4Address(host) && !IsValidIpv4Address(host))
            {
                MessageBox.Show(
                    "The value entered looks like an IPv4 address, but it is not valid.",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
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
                SetBusyState(true);
                SetStatus("Creating temporary workspace...");

                tempExtractionFolder = CreateTempExtractionFolder();
                string outputFolder;

                if (programDebug)
                {
                    Debug.WriteLine($"Temp extraction folder: {tempExtractionFolder}");
                }

                Debug.WriteLine($"Downloading {RemoteDisplayPath} from {host}...");

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                SetStatus("Connecting to touch panel...");
                sftp.Connect();

                SetStatus("Downloading touch panel files...");
                DownloadRemoteDirectory(sftp, RemoteDisplayPath, tempExtractionFolder);

                sftp.Disconnect();

                Debug.WriteLine($"Downloaded {RemoteDisplayPath} to {tempExtractionFolder}");

                if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
                {
                    outputFolder = AppContext.BaseDirectory;
                }
                else
                {
                    outputFolder = txtOutputFolder.Text;
                }

                SetStatus("Creating VTZ archive...");
                string outputVtzPath = CreateVtzArchive(tempExtractionFolder, outputFolder);
                SetStatus("Extraction complete.");

                MessageBox.Show(
                    $"VTZ created successfully:{Environment.NewLine}{outputVtzPath}",
                    "Extraction Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                SetStatus("Extraction failed.");
                MessageBox.Show(
                    $"Extraction failed:{Environment.NewLine}{ex.Message}",
                    "Extraction Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                SetBusyState(false);

                if (!string.IsNullOrWhiteSpace(tempExtractionFolder) &&
                    Directory.Exists(tempExtractionFolder))
                {
                    Directory.Delete(tempExtractionFolder, recursive: true);

                    if (programDebug)
                    {
                        Debug.WriteLine($"Temp folder cleaned up successfully: {tempExtractionFolder}");
                    }
                }
            }
        }
    }
}