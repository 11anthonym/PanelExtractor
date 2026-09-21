namespace PanelExtractor
{
    public partial class MainForm : Form
    {
        private const string RemoteDisplayPath = "/display";

        private bool showDetails;

        public MainForm()
        {
            InitializeComponent();

            System.Drawing.Icon? applicationIcon = System.Drawing.Icon.ExtractAssociatedIcon(
                Application.ExecutablePath);
            if (applicationIcon is not null)
            {
                Icon = applicationIcon;
            }

            ApplyDetailsVisibility();
        }

        private void ApplyDetailsVisibility()
        {
            txtLog.Visible = showDetails;
            lblLog.Visible = showDetails;
            btnToggleDetails.Text = showDetails ? "Hide Details" : "Show Details";
            ClientSize = new Size(500, showDetails ? 442 : 363);
        }

        private void AppendLog(string message)
        {
            // The transport reports a panel's SSH login banner from the SSH message thread, so
            // this can arrive off the UI thread.
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(message));
                return;
            }

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
            chkOpenInXPanel.Enabled = !isBusy;

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

            // A panel that still takes a blank password has never had an account created, so it
            // has no project loaded and nothing to extract. Stop before spending a login attempt.
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

        private async Task<IReadOnlyPanelFileClient> ConnectToPanelAsync(
            string host,
            string username,
            string password)
        {
            try
            {
                return await TryConnectToPanelAsync(host, username, password);
            }
            catch (PanelHostKeyException ex) when (ex.IsIdentityChange)
            {
                if (!ConfirmPanelIdentityChange(ex))
                {
                    throw;
                }

                try
                {
                    SshHostKeyStore.Default.Replace(
                        ex.Host!,
                        ex.Port,
                        ex.SavedFingerprint!,
                        ex.PresentedFingerprint!);
                }
                catch (Exception replaceException) when (
                    replaceException is InvalidDataException or IOException or UnauthorizedAccessException or
                    System.Security.SecurityException)
                {
                    throw new PanelHostKeyException(
                        "The new SSH identity could not be saved safely. SFTP was stopped.",
                        replaceException);
                }

                AppendLog($"Trusted the new SSH identity for {ex.Host}:{ex.Port}.");
                return await TryConnectToPanelAsync(host, username, password);
            }
        }

        private Task<IReadOnlyPanelFileClient> TryConnectToPanelAsync(
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

        private bool ConfirmPanelIdentityChange(PanelHostKeyException exception)
        {
            DialogResult result = MessageBox.Show(
                this,
                $"The SSH identity for {exception.Host} has changed.\n\n" +
                "This can happen if the panel was reset or replaced, but it can also mean " +
                "another device is answering at this address.\n\n" +
                $"Saved fingerprint:\n{exception.SavedFingerprint}\n\n" +
                $"New fingerprint:\n{exception.PresentedFingerprint}\n\n" +
                "Trust this new identity and try again?",
                "Panel Identity Changed",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            return result == DialogResult.Yes;
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

        private void OpenInXPanel(string vtzPath)
        {
            AppendLog("Looking for Crestron XPanel.");

            XPanelLaunchResult result = XPanelLauncher.CreateDefault().Launch(vtzPath);
            AppendLog(result.Message);

            if (result.Outcome == XPanelLaunchOutcome.Started)
            {
                return;
            }

            string detail = result.Outcome == XPanelLaunchOutcome.NotInstalled
                ? "Crestron XPanel was not found on this computer. A VTZ archive can only be " +
                  $"opened with XPanel, which can be installed from:{Environment.NewLine}" +
                  XPanelLauncher.DownloadUrl
                : result.Message;

            MessageBox.Show(
                $"{detail}{Environment.NewLine}{Environment.NewLine}The VTZ was still saved to:" +
                $"{Environment.NewLine}{vtzPath}",
                "Could Not Open XPanel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private async Task DeleteTempExtractionFolderAsync(string? folder)
        {
            if (folder is null || !Directory.Exists(folder))
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
            string? selectedFolder = ModernFolderPicker.Show(
                this,
                "Select where the extracted VTZ file should be saved",
                desktopPath);

            if (selectedFolder is not null)
            {
                txtOutputFolder.Text = selectedFolder;
                AppendLog($"Output folder selected: {selectedFolder}");
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

                if (chkOpenInXPanel.Checked)
                {
                    OpenInXPanel(outputVtzPath);
                }
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
                "Use an account in the panel's Administrators group; lower access levels can sign in but cannot read the project folder.\n\n" +
                "A panel with authentication turned off is reached with its default credentials.\n\n" +
                "Panels lock the account and block this computer after a few failed sign-in attempts, so check credentials rather than retrying.\n\n" +
                "SFTP is always tried first. Legacy FTP fallback only helps an older panel with authentication turned off; turning authentication on disables the panel's FTP server, and the newest panels have none.\n\n" +
                "The first successful SFTP connection remembers the panel's SSH identity. If it later changes, only that panel's new identity can be approved.\n\n" +
                "Extract VTZ downloads the deployed touch panel files and packages them into a VTZ archive.\n\n" +
                "Tick Open in Crestron XPanel after extracting to launch the finished archive. It stays off unless selected. Nothing but XPanel reads the format, so if XPanel is not installed the file is still saved and a download link is shown.\n\n" +
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
