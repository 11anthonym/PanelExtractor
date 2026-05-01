namespace CrestronPanelExtractor
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
            lblLog = new Label();
            lblStatus = new Label();
            SuspendLayout();
            // 
            // lblHost
            // 
            lblHost.AutoSize = true;
            lblHost.Location = new Point(109, 9);
            lblHost.Name = "lblHost";
            lblHost.Size = new Size(56, 15);
            lblHost.TabIndex = 0;
            lblHost.Text = "Host / IP:";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(109, 43);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(63, 15);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(216, 40);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(111, 23);
            txtUsername.TabIndex = 2;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(109, 77);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(60, 15);
            lblPassword.TabIndex = 3;
            lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(216, 74);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(111, 23);
            txtPassword.TabIndex = 4;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtHost
            // 
            txtHost.Location = new Point(216, 6);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(111, 23);
            txtHost.TabIndex = 5;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(22, 124);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(84, 15);
            lblOutputFolder.TabIndex = 6;
            lblOutputFolder.Text = "Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Location = new Point(112, 119);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.ReadOnly = true;
            txtOutputFolder.Size = new Size(279, 23);
            txtOutputFolder.TabIndex = 7;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(397, 120);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(75, 23);
            btnBrowseOutput.TabIndex = 8;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += btnBrowseOutput_Click;
            // 
            // btnExtract
            // 
            btnExtract.Location = new Point(257, 161);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(103, 23);
            btnExtract.TabIndex = 9;
            btnExtract.Text = "Extract VTZ";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // btnTestConnection
            // 
            btnTestConnection.Location = new Point(138, 161);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(103, 23);
            btnTestConnection.TabIndex = 10;
            btnTestConnection.Text = "Test Connection";
            btnTestConnection.UseVisualStyleBackColor = true;
            btnTestConnection.Click += btnTestConnection_Click;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(2, 242);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(481, 37);
            txtLog.TabIndex = 11;
            // 
            // lblLog
            // 
            lblLog.AutoSize = true;
            lblLog.Location = new Point(232, 224);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(30, 15);
            lblLog.TabIndex = 12;
            lblLog.Text = "Log:";
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(22, 192);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(450, 25);
            lblStatus.TabIndex = 13;
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 281);
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
            MinimumSize = new Size(500, 320);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Crestron Panel Extractor";
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
        private Label lblLog;
        private Label lblStatus;
    }
}
