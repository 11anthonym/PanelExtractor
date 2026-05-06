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
        private bool programDebug = false;

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
            if (showDebugLog)
            {
                txtLog.Visible = true;
                lblLog.Visible = true;
                btnToggleDetails.Text = "Hide Details";
                ClientSize = new Size(500, 389);
            }
            else
            {
                txtLog.Visible = false;
                lblLog.Visible = false;
                btnToggleDetails.Text = "Show Details";
                ClientSize = new Size(500, 320);
            }
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");

            // Keep the Details box scrolled to the newest line.
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();

            // Force the UI to repaint while the extraction is running.
            txtLog.Refresh();
            Application.DoEvents();
        }

        private void ClearLog()
        {
            txtLog.Clear();
        }

        private void SetBusyState(bool isBusy)
        {
            btnExtract.Enabled = !isBusy;
            btnTestConnection.Enabled = !isBusy;
            btnBrowseOutput.Enabled = !isBusy;
            btnToggleDetails.Enabled = !isBusy;

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
            AppendLog($"Reading remote folder: {remoteDirectory}");

            Directory.CreateDirectory(localDirectory);
            AppendLog($"Created local folder: {localDirectory}");

            var items = sftp.ListDirectory(remoteDirectory)
                .Where(item => item.Name != "." && item.Name != "..")
                .ToList();

            AppendLog($"Found {items.Count} item(s) in {remoteDirectory}.");

            foreach (var item in items)
            {
                string remotePath = $"{remoteDirectory.TrimEnd('/')}/{item.Name}";
                string localPath = Path.Combine(localDirectory, item.Name);

                if (item.IsDirectory)
                {
                    AppendLog($"Entering folder: {remotePath}");
                    DownloadRemoteDirectory(sftp, remotePath, localPath);
                }
                else if (item.IsRegularFile)
                {
                    AppendLog($"Downloading file: {remotePath}");

                    if (programDebug)
                    {
                        Debug.WriteLine($"Downloading {remotePath}");
                    }

                    using FileStream fileStream = File.Create(localPath);
                    sftp.DownloadFile(remotePath, fileStream);

                    AppendLog($"Downloaded file: {remotePath} ({item.Length:N0} bytes)");
                }
                else
                {
                    AppendLog($"Skipping unsupported item: {remotePath}");
                }
            }

            AppendLog($"Finished folder: {remoteDirectory}");
        }

        private string CreateVtzArchive(string tempExtractionFolder, string outputFolder)
        {
            AppendLog("Searching for .vtx project file...");

            string[] vtxFiles = Directory.GetFiles(tempExtractionFolder, "*.vtx", SearchOption.TopDirectoryOnly);

            if (vtxFiles.Length == 0)
            {
                AppendLog("No .vtx file was found.");
                throw new InvalidOperationException("No .vtx file was found in the downloaded display folder.");
            }

            if (vtxFiles.Length > 1)
            {
                AppendLog("Multiple .vtx files were found.");
                throw new InvalidOperationException("Multiple .vtx files were found in the downloaded /display folder.");
            }

            string vtxFile = vtxFiles[0];
            string projectName = Path.GetFileNameWithoutExtension(vtxFile);

            AppendLog($"Found project file: {Path.GetFileName(vtxFile)}");
            AppendLog($"Using project name: {projectName}");

            string outputVtzPath = GetUniqueFilePath(outputFolder, projectName, ".vtz");

            AppendLog($"Creating VTZ archive: {outputVtzPath}");

            ZipFile.CreateFromDirectory(
                tempExtractionFolder,
                outputVtzPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false
            );

            AppendLog("VTZ archive created successfully.");

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
            ClearLog();
            AppendLog("Starting connection test.");

            string host = txtHost.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            AppendLog("Validating input fields.");

            if (string.IsNullOrWhiteSpace(host))
            {
                SetStatus("Missing host or IP address.");
                AppendLog("Validation failed: missing host or IP address.");
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LooksLikeIpv4Address(host) && !IsValidIpv4Address(host))
            {
                SetStatus("Invalid IP address.");
                AppendLog($"Validation failed: invalid IPv4 address entered: {host}");
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
                AppendLog("Validation failed: missing username.");
                MessageBox.Show("Enter a username.", "Missing Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Missing password.");
                AppendLog("Validation failed: missing password.");
                MessageBox.Show("Enter a password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusyState(true);
                SetStatus("Preparing connection test...");
                AppendLog("Preparing SFTP connection.");

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                SetStatus("Connecting to touch panel...");
                AppendLog($"Connecting to touch panel at {host} on SFTP port 22.");
                sftp.Connect();

                AppendLog("SFTP connection established.");

                SetStatus("Checking for touch panel file contents...");
                AppendLog($"Checking for remote folder: {RemoteDisplayPath}");
                bool displayFolderExists = sftp.Exists(RemoteDisplayPath);

                sftp.Disconnect();
                AppendLog("SFTP connection closed.");

                if (displayFolderExists)
                {
                    SetStatus("Connection successful. Touch panel files found.");
                    AppendLog($"Connection successful. Remote folder found: {RemoteDisplayPath}");

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
                    AppendLog($"Connected, but remote folder was not found: {RemoteDisplayPath}");

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
                AppendLog($"Connection test failed: {ex.Message}");

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
                AppendLog("Connection test finished.");
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            ClearLog();
            AppendLog("Starting extraction.");

            if (programDebug)
            {
                Debug.WriteLine("Extract Button Clicked");
            }

            string host = txtHost.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string? tempExtractionFolder = null;

            AppendLog("Validating input fields.");

            if (string.IsNullOrWhiteSpace(host))
            {
                AppendLog("Validation failed: missing host or IP address.");
                MessageBox.Show("Enter a host or IP address.", "Missing Host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LooksLikeIpv4Address(host) && !IsValidIpv4Address(host))
            {
                AppendLog($"Validation failed: invalid IPv4 address entered: {host}");
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
                AppendLog("Validation failed: missing username.");
                MessageBox.Show("Enter a username.", "Missing Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                AppendLog("Validation failed: missing password.");
                MessageBox.Show("Enter a password.", "Missing Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusyState(true);

                SetStatus("Creating temporary workspace...");
                AppendLog("Creating temporary workspace.");

                tempExtractionFolder = CreateTempExtractionFolder();
                AppendLog($"Temporary workspace created: {tempExtractionFolder}");

                string outputFolder;

                if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
                {
                    outputFolder = AppContext.BaseDirectory;
                    AppendLog($"No output folder selected. Using application folder: {outputFolder}");
                }
                else
                {
                    outputFolder = txtOutputFolder.Text;
                    AppendLog($"Using selected output folder: {outputFolder}");
                }

                var connectionInfo = new ConnectionInfo(
                    host,
                    22,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

                connectionInfo.Timeout = TimeSpan.FromSeconds(5);

                using var sftp = new SftpClient(connectionInfo);

                SetStatus("Connecting to touch panel...");
                AppendLog($"Connecting to touch panel at {host} on SFTP port 22.");

                sftp.Connect();

                AppendLog("SFTP connection established.");

                SetStatus("Checking for touch panel file contents...");
                AppendLog($"Checking for remote folder: {RemoteDisplayPath}");

                if (!sftp.Exists(RemoteDisplayPath))
                {
                    AppendLog($"Remote folder was not found: {RemoteDisplayPath}");
                    throw new InvalidOperationException("The /display folder was not found on the touch panel.");
                }

                AppendLog($"Remote folder found: {RemoteDisplayPath}");

                SetStatus("Downloading touch panel files...");
                AppendLog("Starting recursive download.");

                DownloadRemoteDirectory(sftp, RemoteDisplayPath, tempExtractionFolder);

                AppendLog("Remote download complete.");

                sftp.Disconnect();
                AppendLog("SFTP connection closed.");

                SetStatus("Creating VTZ archive...");
                AppendLog("Starting VTZ archive creation.");

                string outputVtzPath = CreateVtzArchive(tempExtractionFolder, outputFolder);

                SetStatus("Extraction complete.");
                AppendLog($"Extraction complete. Output file: {outputVtzPath}");

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
                AppendLog($"Extraction failed: {ex.Message}");

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
                    AppendLog("Cleaning up temporary workspace.");

                    Directory.Delete(tempExtractionFolder, recursive: true);

                    AppendLog("Temporary workspace deleted.");

                    if (programDebug)
                    {
                        Debug.WriteLine($"Temp folder cleaned up successfully: {tempExtractionFolder}");
                    }
                }

                AppendLog("Extraction process finished.");
            }
        }

        private void btnToggleDetails_Click(object sender, EventArgs e)
        {
            if (showDebugLog)
            {
                showDebugLog = false;
            }
            else
            {
                showDebugLog = true;
            }

            ApplyDebugVisibility();
        }
    }
}