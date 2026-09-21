namespace PanelExtractor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblHost = new Label();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            txtHost = new TextBox();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnBrowseOutput = new Button();
            btnExtract = new Button();
            btnTestConnection = new Button();
            txtLog = new TextBox();
            lblStatus = new Label();
            btnToggleDetails = new Button();
            lblLog = new Label();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            howThisWorksToolStripMenuItem = new ToolStripMenuItem();
            chkAllowLegacyFtp = new CheckBox();
            chkOpenInXPanel = new CheckBox();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lblHost
            // 
            lblHost.AutoSize = true;
            lblHost.Location = new Point(109, 37);
            lblHost.Name = "lblHost";
            lblHost.Size = new Size(56, 15);
            lblHost.TabIndex = 0;
            lblHost.Text = "Host / IP:";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(109, 65);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(63, 15);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(200, 62);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(127, 23);
            txtUsername.TabIndex = 2;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(109, 95);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(60, 15);
            lblPassword.TabIndex = 3;
            lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(200, 91);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(127, 23);
            txtPassword.TabIndex = 4;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtHost
            // 
            txtHost.Location = new Point(200, 34);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(127, 23);
            txtHost.TabIndex = 5;
            //
            // chkAllowLegacyFtp
            //
            chkAllowLegacyFtp.AutoSize = true;
            chkAllowLegacyFtp.Location = new Point(109, 121);
            chkAllowLegacyFtp.Name = "chkAllowLegacyFtp";
            chkAllowLegacyFtp.Size = new Size(256, 19);
            chkAllowLegacyFtp.TabIndex = 6;
            chkAllowLegacyFtp.Text = "Allow legacy FTP fallback (unencrypted)";
            chkAllowLegacyFtp.UseVisualStyleBackColor = true;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(22, 159);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(84, 15);
            lblOutputFolder.TabIndex = 7;
            lblOutputFolder.Text = "Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Location = new Point(112, 154);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.ReadOnly = true;
            txtOutputFolder.Size = new Size(279, 23);
            txtOutputFolder.TabIndex = 8;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(397, 155);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(75, 23);
            btnBrowseOutput.TabIndex = 9;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += btnBrowseOutput_Click;
            //
            // chkOpenInXPanel
            //
            chkOpenInXPanel.AutoSize = true;
            chkOpenInXPanel.Location = new Point(112, 185);
            chkOpenInXPanel.Name = "chkOpenInXPanel";
            chkOpenInXPanel.Size = new Size(238, 19);
            chkOpenInXPanel.TabIndex = 10;
            chkOpenInXPanel.Text = "Open in Crestron XPanel after extracting";
            chkOpenInXPanel.UseVisualStyleBackColor = true;
            //
            // btnExtract
            //
            btnExtract.Location = new Point(265, 217);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(103, 23);
            btnExtract.TabIndex = 11;
            btnExtract.Text = "Extract VTZ";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // btnTestConnection
            // 
            btnTestConnection.Location = new Point(129, 217);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(103, 23);
            btnTestConnection.TabIndex = 12;
            btnTestConnection.Text = "Test Connection";
            btnTestConnection.UseVisualStyleBackColor = true;
            btnTestConnection.Click += btnTestConnection_Click;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(2, 329);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(481, 90);
            txtLog.TabIndex = 13;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(17, 243);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(450, 25);
            lblStatus.TabIndex = 15;
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnToggleDetails
            // 
            btnToggleDetails.Location = new Point(198, 280);
            btnToggleDetails.Name = "btnToggleDetails";
            btnToggleDetails.Size = new Size(88, 23);
            btnToggleDetails.TabIndex = 16;
            btnToggleDetails.Text = "Show Details";
            btnToggleDetails.UseVisualStyleBackColor = true;
            btnToggleDetails.Click += btnToggleDetails_Click;
            // 
            // lblLog
            // 
            lblLog.AutoSize = true;
            lblLog.Location = new Point(220, 311);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(45, 15);
            lblLog.TabIndex = 14;
            lblLog.Text = "Details:";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(484, 24);
            menuStrip1.TabIndex = 17;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { howThisWorksToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(180, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // howThisWorksToolStripMenuItem
            // 
            howThisWorksToolStripMenuItem.Name = "howThisWorksToolStripMenuItem";
            howThisWorksToolStripMenuItem.Size = new Size(180, 22);
            howThisWorksToolStripMenuItem.Text = "How This Works";
            howThisWorksToolStripMenuItem.Click += howThisWorksToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightSkyBlue;
            ClientSize = new Size(484, 416);
            Controls.Add(chkAllowLegacyFtp);
            Controls.Add(chkOpenInXPanel);
            Controls.Add(btnToggleDetails);
            Controls.Add(lblStatus);
            Controls.Add(lblLog);
            Controls.Add(txtLog);
            Controls.Add(btnTestConnection);
            Controls.Add(btnExtract);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputFolder);
            Controls.Add(lblOutputFolder);
            Controls.Add(txtHost);
            Controls.Add(txtPassword);
            Controls.Add(lblPassword);
            Controls.Add(txtUsername);
            Controls.Add(lblUsername);
            Controls.Add(lblHost);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(500, 376);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Panel Extractor";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblHost;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private TextBox txtHost;
        private Label lblOutputFolder;
        private TextBox txtOutputFolder;
        private Button btnBrowseOutput;
        private Button btnExtract;
        private Button btnTestConnection;
        private TextBox txtLog;
        private Label lblStatus;
        private Button btnToggleDetails;
        private Label lblLog;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem howThisWorksToolStripMenuItem;
        private CheckBox chkAllowLegacyFtp;
        private CheckBox chkOpenInXPanel;
    }
}
