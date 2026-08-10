namespace PanelExtractor
{
    public partial class MainForm : Form
    {
        private const string RemoteDisplayPath = "/display";

        private bool showDetails;

        public MainForm()
        {
            InitializeComponent();
            ApplyDetailsVisibility();
        }

        private void ApplyDetailsVisibility()
        {
            txtLog.Visible = showDetails;
            lblLog.Visible = showDetails;
            btnToggleDetails.Text = showDetails ? "Hide Details" : "Show Details";
            ClientSize = new Size(500, showDetails ? 416 : 337);
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");

            // Keep the Details box scrolled to the newest line.
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void SetBusyState(bool isBusy)
        {
            btnExtract.Enabled = !isBusy;
            btnTestConnection.Enabled = !isBusy;
            btnBrowseOutput.Enabled = !isBusy;
            chkAllowLegacyFtp.Enabled = !isBusy;
            forgetSavedSshHostKeysToolStripMenuItem.Enabled = !isBusy;

            Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
        }

        private bool TryGetConnectionDetails(out string host, out string username, out string password)
        {
            host = txtHost.Text.Trim();
            username = txtUsername.Text.Trim();
            password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(host))
            {
                ShowValidationError("Missing host or IP address.", "Enter a host or IP address.", "Missing Host");
                return false;
            }

            if (HostValidator.IsInvalidIpv4Literal(host))
            {
                ShowValidationError(
                    "Invalid IP address.",
                    "The value entered looks like an IPv4 address, but it is not valid.",
                    "Invalid IP Address"
                );
                return false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowValidationError("Missing username.", "Enter a username.", "Missing Username");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowValidationError("Missing password.", "Enter a password.", "Missing Password");
                return false;
            }

            return true;
        }

        private void ShowValidationError(string status, string message, string title)
        {
            lblStatus.Text = status;
            AppendLog($"Validation failed: {status}");
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string CreateTempExtractionFolder()
        {
            string tempFolder = Path.Combine(
                Path.GetTempPath(), nameof(PanelExtractor), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        private Task<IReadOnlyPanelFileClient> ConnectToPanelAsync(
            string host,
            string username,
            string password)
        {
            var selector = PanelTransportSelector.CreateDefault(host, username, password);
            return selector.ConnectAsync(
                RemoteDisplayPath,
                chkAllowLegacyFtp.Checked,
                AppendLog,
                CancellationToken.None);
        }

        private async Task DownloadRemoteDirectoryAsync(
            IReadOnlyPanelFileClient client,
            string remoteDirectory,
            string localDirectory)
        {
            AppendLog($"Reading remote folder: {remoteDirectory}");

            Directory.CreateDirectory(localDirectory);
            AppendLog($"Created local folder: {localDirectory}");

            await foreach (var item in client.ListDirectoryAsync(remoteDirectory, CancellationToken.None))
            {
                if (item.Name is "." or "..")
                {
                    continue;
                }

                string localPath = ExtractionPath.Combine(localDirectory, item.Name);
                string remotePath = $"{remoteDirectory.TrimEnd('/')}/{item.Name}";

                switch (item.Kind)
                {
                    case PanelFileKind.Directory:
                        AppendLog($"Entering folder: {remotePath}");
                        await DownloadRemoteDirectoryAsync(client, remotePath, localPath);
                        break;

                    case PanelFileKind.File:
                        AppendLog($"Downloading file: {remotePath}");

                        using (FileStream fileStream = File.Create(localPath))
                        {
                            await client.DownloadFileAsync(remotePath, fileStream, CancellationToken.None);
                        }

                        AppendLog($"Downloaded file: {remotePath} ({item.Length:N0} bytes)");
                        break;

                    default:
                        AppendLog($"Skipping unsupported item: {remotePath}");
                        break;
                }
            }

            AppendLog($"Finished folder: {remoteDirectory}");
        }

        private async Task DeleteTempExtractionFolderAsync(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            try
            {
                AppendLog("Cleaning up temporary workspace.");
                await Task.Run(() => Directory.Delete(folder, recursive: true));
                AppendLog("Temporary workspace deleted.");
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                AppendLog($"Temporary workspace cleanup failed: {ex.Message}");
            }
        }

        // Button clicks
        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select where the extracted VTZ file should be saved",
                SelectedPath = desktopPath
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtOutputFolder.Text = dialog.SelectedPath;
                AppendLog($"Output folder selected: {dialog.SelectedPath}");
            }
        }

        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            AppendLog("Starting connection test.");

            AppendLog("Validating input fields.");

            if (!TryGetConnectionDetails(out string host, out string username, out string password))
            {
                return;
            }

            try
            {
                SetBusyState(true);
                lblStatus.Text = "Finding the best connection method...";

                string protocolName;
                using (IReadOnlyPanelFileClient client = await ConnectToPanelAsync(host, username, password))
                {
                    protocolName = client.ProtocolName;
                }

                AppendLog($"{protocolName} connection closed.");
                lblStatus.Text = $"Connection successful using {protocolName}.";

                MessageBox.Show(
                    $"Connection successful using {protocolName}. Expected touch panel file contents were found.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Connection test failed.";
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

        private async void btnExtract_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            AppendLog("Starting extraction.");

            string? tempExtractionFolder = null;

            AppendLog("Validating input fields.");

            if (!TryGetConnectionDetails(out string host, out string username, out string password))
            {
                return;
            }

            try
            {
                SetBusyState(true);

                lblStatus.Text = "Creating temporary workspace...";
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

                lblStatus.Text = "Finding the best connection method...";

                string protocolName;
                using (IReadOnlyPanelFileClient client = await ConnectToPanelAsync(host, username, password))
                {
                    protocolName = client.ProtocolName;
                    lblStatus.Text = "Downloading touch panel files...";
                    AppendLog($"Starting recursive download using {protocolName}.");

                    await DownloadRemoteDirectoryAsync(client, RemoteDisplayPath, tempExtractionFolder);
                }

                AppendLog("Remote download complete.");
                AppendLog($"{protocolName} connection closed.");

                lblStatus.Text = "Creating VTZ archive...";
                AppendLog("Starting VTZ archive creation.");

                string outputVtzPath = await VtzArchive.CreateAsync(tempExtractionFolder, outputFolder);

                lblStatus.Text = "Extraction complete.";
                AppendLog("VTZ archive created successfully.");
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
                lblStatus.Text = "Extraction failed.";
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
                await DeleteTempExtractionFolderAsync(tempExtractionFolder);

                SetBusyState(false);
                AppendLog("Extraction process finished.");
            }
        }

        private void btnToggleDetails_Click(object sender, EventArgs e)
        {
            showDetails = !showDetails;
            ApplyDetailsVisibility();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void forgetSavedSshHostKeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string status = SshHostKeyStore.Default.Clear()
                    ? "Saved SSH host keys were forgotten."
                    : "No saved SSH host keys were found.";

                lblStatus.Text = status;
                AppendLog(status);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Saved SSH host keys could not be cleared.";
                AppendLog($"Saved SSH host key cleanup failed: {ex.Message}");
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Panel Extractor\n" +
                $"Version {Application.ProductVersion}\n\n" +
                "Recovers deployed project files from compatible Crestron® touch panels and packages them as a VTZ archive.\n\n" +
                "Use only on systems you own, manage, or are authorized to access.\n\n" +
                "Crestron® is a registered trademark of Crestron Electronics, Inc.\n" +
                "Panel Extractor is not affiliated with or endorsed by Crestron Electronics, Inc.\n\n" +
                "© 2026 Anthony Moretti\n" +
                "Licensed under the MIT License.",
                "About Panel Extractor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void howThisWorksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Enter the panel address and credentials. Test Connection can be used to confirm the panel is reachable before extracting.\n\n" +
                "SFTP is always tried first. Enable legacy FTP fallback only for older panels that require unencrypted FTP.\n\n" +
                "The first successful SFTP connection remembers the panel's SSH identity. Unexpected identity changes are blocked automatically.\n\n" +
                "Extract VTZ downloads the deployed touch panel files and packages them into a VTZ archive.\n\n" +
                "If no output folder is selected, the file is saved in the same folder the program was launched from.\n\n" +
                "If a file with the same name already exists, a numbered copy is created instead of overwriting it.\n\n" +
                "Use Show Details to view connection and extraction progress.",
                "How This Works",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
