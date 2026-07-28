namespace VmixScheduler;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        this.lblHost = new Label();
        this.txtHost = new TextBox();
        this.lblPort = new Label();
        this.txtPort = new TextBox();
        this.btnRefreshInputs = new Button();
        this.lblConnectionStatus = new Label();

        this.grpRoles = new GroupBox();
        this.lblNowNextSongInterval = new Label();
        this.cmbNowNextSongInterval = new ComboBox();

        this.lblRoleFiller = new Label();
        this.lblRoleFillerValue = new Label();
        this.lblRoleNow = new Label();
        this.lblRoleNowValue = new Label();
        this.lblRoleNext = new Label();
        this.lblRoleNextValue = new Label();
        this.lblRoleNowSong = new Label();
        this.lblRoleNowSongValue = new Label();
        this.lblRoleNextSong = new Label();
        this.lblRoleNextSongValue = new Label();
        this.lblRoleBackin = new Label();
        this.lblRoleBackinValue = new Label();
        this.lblRoleOverlay1 = new Label();
        this.lblRoleOverlay1Value = new Label();
        this.lblRoleOverlay2 = new Label();
        this.lblRoleOverlay2Value = new Label();
        this.lblRoleOverlay3 = new Label();
        this.lblRoleOverlay3Value = new Label();
        this.lblRoleOverlay4 = new Label();
        this.lblRoleOverlay4Value = new Label();

        this.lblNowNextInterval = new Label();
        this.cmbNowNextInterval = new ComboBox();

        this.dgvSchedule = new DataGridView();
        this.colRawTitle = new DataGridViewTextBoxColumn();
        this.colDisplayName = new DataGridViewTextBoxColumn();
        this.colCategory = new DataGridViewTextBoxColumn();
        this.colRecurrence = new DataGridViewTextBoxColumn();
        this.colNextOccurrence = new DataGridViewTextBoxColumn();
        this.colStatus = new DataGridViewTextBoxColumn();

        this.btnTriggerSelected = new Button();

        this.lblLog = new Label();
        this.txtLog = new TextBox();

        this.tmrCheck = new System.Windows.Forms.Timer(this.components);

        ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).BeginInit();
        this.SuspendLayout();

        this.lblHost.AutoSize = true;
        this.lblHost.Location = new Point(12, 15);
        this.lblHost.Text = "vMix Host:";

        this.txtHost.Location = new Point(85, 12);
        this.txtHost.Size = new Size(110, 23);
        this.txtHost.Text = "127.0.0.1";

        this.lblPort.AutoSize = true;
        this.lblPort.Location = new Point(205, 15);
        this.lblPort.Text = "Port:";

        this.txtPort.Location = new Point(240, 12);
        this.txtPort.Size = new Size(60, 23);
        this.txtPort.Text = "8088";

        this.btnRefreshInputs.Location = new Point(315, 11);
        this.btnRefreshInputs.Size = new Size(140, 27);
        this.btnRefreshInputs.Text = "Refresh Inputs";
        this.btnRefreshInputs.Click += new EventHandler(this.btnRefreshInputs_Click);

        this.lblConnectionStatus.AutoSize = true;
        this.lblConnectionStatus.Location = new Point(470, 15);
        this.lblConnectionStatus.ForeColor = Color.DimGray;
        this.lblConnectionStatus.Text = "Not connected";

        // grpRoles
        this.grpRoles.Location = new Point(12, 50);
        this.grpRoles.Size = new Size(860, 180);
        this.grpRoles.Text = "Auto-Detected Roles (rename vMix inputs to these exact names)";

        this.lblNowNextSongInterval.AutoSize = true;
        this.lblNowNextSongInterval.Location = new Point(15, 23);
        this.lblNowNextSongInterval.Text = "NOWSong/NEXTSong Interval:";

        this.cmbNowNextSongInterval.Location = new Point(280, 20);
        this.cmbNowNextSongInterval.Size = new Size(80, 23);
        this.cmbNowNextSongInterval.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbNowNextSongInterval.Items.AddRange(new object[] { "1 min", "2 min", "3 min" });
        this.cmbNowNextSongInterval.SelectedItem = "1 min";

        this.lblNowNextInterval.AutoSize = true;
        this.lblNowNextInterval.Location = new Point(450, 23);
        this.lblNowNextInterval.Text = "NOW/NEXT Interval:";

        this.cmbNowNextInterval.Location = new Point(600, 20);
        this.cmbNowNextInterval.Size = new Size(80, 23);
        this.cmbNowNextInterval.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbNowNextInterval.Items.AddRange(new object[] { "5 min", "10 min", "20 min" });
        this.cmbNowNextInterval.SelectedItem = "10 min";

        SetupRolePair(this.lblRoleFiller, this.lblRoleFillerValue, "Filler:", 15, 55);
        SetupRolePair(this.lblRoleNow, this.lblRoleNowValue, "Now:", 440, 55);
        SetupRolePair(this.lblRoleNext, this.lblRoleNextValue, "Next:", 15, 78);
        SetupRolePair(this.lblRoleNowSong, this.lblRoleNowSongValue, "NowSong:", 440, 78);
        SetupRolePair(this.lblRoleNextSong, this.lblRoleNextSongValue, "NextSong:", 15, 101);
        SetupRolePair(this.lblRoleBackin, this.lblRoleBackinValue, "Backin:", 440, 101);
        SetupRolePair(this.lblRoleOverlay1, this.lblRoleOverlay1Value, "Overlay1:", 15, 124);
        SetupRolePair(this.lblRoleOverlay2, this.lblRoleOverlay2Value, "Overlay2:", 440, 124);
        SetupRolePair(this.lblRoleOverlay3, this.lblRoleOverlay3Value, "Overlay3:", 15, 147);
        SetupRolePair(this.lblRoleOverlay4, this.lblRoleOverlay4Value, "Overlay4:", 440, 147);

        this.grpRoles.Controls.Add(this.lblNowNextSongInterval);
        this.grpRoles.Controls.Add(this.cmbNowNextSongInterval);
        this.grpRoles.Controls.Add(this.lblNowNextInterval);
        this.grpRoles.Controls.Add(this.cmbNowNextInterval);
        this.grpRoles.Controls.Add(this.lblRoleFiller);
        this.grpRoles.Controls.Add(this.lblRoleFillerValue);
        this.grpRoles.Controls.Add(this.lblRoleNow);
        this.grpRoles.Controls.Add(this.lblRoleNowValue);
        this.grpRoles.Controls.Add(this.lblRoleNext);
        this.grpRoles.Controls.Add(this.lblRoleNextValue);
        this.grpRoles.Controls.Add(this.lblRoleNowSong);
        this.grpRoles.Controls.Add(this.lblRoleNowSongValue);
        this.grpRoles.Controls.Add(this.lblRoleNextSong);
        this.grpRoles.Controls.Add(this.lblRoleNextSongValue);
        this.grpRoles.Controls.Add(this.lblRoleBackin);
        this.grpRoles.Controls.Add(this.lblRoleBackinValue);
        this.grpRoles.Controls.Add(this.lblRoleOverlay1);
        this.grpRoles.Controls.Add(this.lblRoleOverlay1Value);
        this.grpRoles.Controls.Add(this.lblRoleOverlay2);
        this.grpRoles.Controls.Add(this.lblRoleOverlay2Value);
        this.grpRoles.Controls.Add(this.lblRoleOverlay3);
        this.grpRoles.Controls.Add(this.lblRoleOverlay3Value);
        this.grpRoles.Controls.Add(this.lblRoleOverlay4);
        this.grpRoles.Controls.Add(this.lblRoleOverlay4Value);

        // dgvSchedule
        this.dgvSchedule.Location = new Point(12, 240);
        this.dgvSchedule.Size = new Size(860, 290);
        this.dgvSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.dgvSchedule.AllowUserToAddRows = false;
        this.dgvSchedule.AllowUserToDeleteRows = false;
        this.dgvSchedule.ReadOnly = true;
        this.dgvSchedule.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvSchedule.MultiSelect = false;
        this.dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvSchedule.RowHeadersVisible = false;
        this.dgvSchedule.Columns.AddRange(new DataGridViewColumn[] {
            this.colRawTitle, this.colDisplayName, this.colCategory, this.colRecurrence, this.colNextOccurrence, this.colStatus });

        this.colRawTitle.HeaderText = "Input Name";
        this.colRawTitle.Name = "colRawTitle";
        this.colRawTitle.FillWeight = 22;

        this.colDisplayName.HeaderText = "Display Name";
        this.colDisplayName.Name = "colDisplayName";
        this.colDisplayName.FillWeight = 20;

        this.colCategory.HeaderText = "Category";
        this.colCategory.Name = "colCategory";
        this.colCategory.FillWeight = 12;

        this.colRecurrence.HeaderText = "Recurrence";
        this.colRecurrence.Name = "colRecurrence";
        this.colRecurrence.FillWeight = 18;

        this.colNextOccurrence.HeaderText = "Next Occurrence";
        this.colNextOccurrence.Name = "colNextOccurrence";
        this.colNextOccurrence.FillWeight = 16;

        this.colStatus.HeaderText = "Status";
        this.colStatus.Name = "colStatus";
        this.colStatus.FillWeight = 14;

        this.btnTriggerSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnTriggerSelected.Location = new Point(12, 540);
        this.btnTriggerSelected.Size = new Size(160, 27);
        this.btnTriggerSelected.Text = "Trigger Selected Now";
        this.btnTriggerSelected.Click += new EventHandler(this.btnTriggerSelected_Click);

        this.lblLog.AutoSize = true;
        this.lblLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.lblLog.Location = new Point(12, 578);
        this.lblLog.Text = "Log:";

        this.txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.txtLog.Location = new Point(12, 598);
        this.txtLog.Size = new Size(860, 110);
        this.txtLog.Multiline = true;
        this.txtLog.ReadOnly = true;
        this.txtLog.ScrollBars = ScrollBars.Vertical;

        this.tmrCheck.Interval = 1000;
        this.tmrCheck.Tick += new EventHandler(this.tmrCheck_Tick);

        this.ClientSize = new Size(884, 720);
        this.MinimumSize = new Size(700, 600);
        this.Text = "vMix Scheduler";
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        this.Controls.Add(this.lblHost);
        this.Controls.Add(this.txtHost);
        this.Controls.Add(this.lblPort);
        this.Controls.Add(this.txtPort);
        this.Controls.Add(this.btnRefreshInputs);
        this.Controls.Add(this.lblConnectionStatus);
        this.Controls.Add(this.grpRoles);
        this.Controls.Add(this.dgvSchedule);
        this.Controls.Add(this.btnTriggerSelected);
        this.Controls.Add(this.lblLog);
        this.Controls.Add(this.txtLog);

        ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private static void SetupRolePair(Label nameLabel, Label valueLabel, string text, int x, int y)
    {
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(x, y);
        nameLabel.Text = text;

        valueLabel.AutoSize = true;
        valueLabel.Location = new Point(x + 190, y);
        valueLabel.ForeColor = Color.DimGray;
        valueLabel.Text = "(not found)";
    }

    private Label lblHost;
    private TextBox txtHost;
    private Label lblPort;
    private TextBox txtPort;
    private Button btnRefreshInputs;
    private Label lblConnectionStatus;

    private GroupBox grpRoles;
    private Label lblNowNextSongInterval;
    private ComboBox cmbNowNextSongInterval;
    private Label lblRoleFiller;
    private Label lblRoleFillerValue;
    private Label lblRoleNow;
    private Label lblRoleNowValue;
    private Label lblRoleNext;
    private Label lblRoleNextValue;
    private Label lblRoleNowSong;
    private Label lblRoleNowSongValue;
    private Label lblRoleNextSong;
    private Label lblRoleNextSongValue;
    private Label lblRoleBackin;
    private Label lblRoleBackinValue;
    private Label lblRoleOverlay1;
    private Label lblRoleOverlay1Value;
    private Label lblRoleOverlay2;
    private Label lblRoleOverlay2Value;
    private Label lblRoleOverlay3;
    private Label lblRoleOverlay3Value;
    private Label lblRoleOverlay4;
    private Label lblRoleOverlay4Value;

    private Label lblNowNextInterval;
    private ComboBox cmbNowNextInterval;

    private DataGridView dgvSchedule;
    private DataGridViewTextBoxColumn colRawTitle;
    private DataGridViewTextBoxColumn colDisplayName;
    private DataGridViewTextBoxColumn colCategory;
    private DataGridViewTextBoxColumn colRecurrence;
    private DataGridViewTextBoxColumn colNextOccurrence;
    private DataGridViewTextBoxColumn colStatus;

    private Button btnTriggerSelected;
    private Label lblLog;
    private TextBox txtLog;
    private System.Windows.Forms.Timer tmrCheck;
}
