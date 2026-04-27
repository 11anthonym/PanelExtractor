namespace CrestronPanelExtractor
{
    public partial class MainForm : Form
    {
        private bool showDebugLog = false;
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
    }
}