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

        this.btnStart = new Button();
        this.btnStop = new Button();
        this.chkAutoStart = new CheckBox();
        this.lblLiveStatus = new Label();

        this.grpRoles = new GroupBox();
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
        this.lblRolePromo = new Label();
        this.lblRolePromoValue = new Label();

        this.grpAutomation = new GroupBox();
        this.lblNowNextInterval = new Label();
        this.cmbNowNextInterval = new ComboBox();
        this.lblNowNextDuration = new Label();
        this.numNowNextDuration = new NumericUpDown();
        this.lblTriggerOffset = new Label();
        this.numTriggerOffset = new NumericUpDown();
        this.lblSongInterval = new Label();
        this.numSongInterval = new NumericUpDown();
        this.lblSongDuration = new Label();
        this.numSongDuration = new NumericUpDown();
        this.lblPromoInterval = new Label();
        this.numPromoInterval = new NumericUpDown();
        this.lblAdsFrom = new Label();
        this.dtpAdsFrom = new DateTimePicker();
        this.lblAdsTo = new Label();
        this.dtpAdsTo = new DateTimePicker();

        this.dgvSchedule = new DataGridView();
        this.colRawTitle = new DataGridViewTextBoxColumn();
        this.colDisplayName = new DataGridViewTextBoxColumn();
        this.colCategory = new DataGridViewTextBoxColumn();
        this.colRecurrence = new DataGridViewTextBoxColumn();
        this.colNextOccurrence = new DataGridViewTextBoxColumn();
        this.colStatus = new DataGridViewTextBoxColumn();

        this.btnTriggerSelected = new Button();
        this.btnViewAsRunLog = new Button();

        this.lblLog = new Label();
        this.txtLog = new TextBox();

        this.tmrCheck = new System.Windows.Forms.Timer(this.components);

        ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numNowNextDuration)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numTriggerOffset)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSongInterval)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSongDuration)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numPromoInterval)).BeginInit();
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

        // Automation start/stop row
        this.btnStart.Location = new Point(12, 40);
        this.btnStart.Size = new Size(70, 27);
        this.btnStart.Text = "Start";
        this.btnStart.Click += new EventHandler(this.btnStart_Click);

        this.btnStop.Location = new Point(88, 40);
        this.btnStop.Size = new Size(70, 27);
        this.btnStop.Text = "Stop";
        this.btnStop.Click += new EventHandler(this.btnStop_Click);

        this.chkAutoStart.AutoSize = true;
        this.chkAutoStart.Location = new Point(168, 45);
        this.chkAutoStart.Text = "Auto Start";
        this.chkAutoStart.Checked = true;

        this.lblLiveStatus.AutoSize = true;
        this.lblLiveStatus.Location = new Point(300, 47);
        this.lblLiveStatus.ForeColor = Color.DimGray;
        this.lblLiveStatus.Text = "Position: --:-- / Duration: --:-- / Remaining: --:--";

        // grpRoles
        this.grpRoles.Location = new Point(12, 76);
        this.grpRoles.Size = new Size(860, 170);
        this.grpRoles.Text = "Auto-Detected Roles (rename vMix inputs to these exact names)";

        SetupRolePair(this.lblRoleFiller, this.lblRoleFillerValue, "Filler:", 15, 23);
        SetupRolePair(this.lblRoleNow, this.lblRoleNowValue, "Now:", 440, 23);
        SetupRolePair(this.lblRoleNext, this.lblRoleNextValue, "Next:", 15, 46);
        SetupRolePair(this.lblRoleNowSong, this.lblRoleNowSongValue, "NowSong:", 440, 46);
        SetupRolePair(this.lblRoleNextSong, this.lblRoleNextSongValue, "NextSong:", 15, 69);
        SetupRolePair(this.lblRoleBackin, this.lblRoleBackinValue, "Backin:", 440, 69);
        SetupRolePair(this.lblRoleOverlay1, this.lblRoleOverlay1Value, "Overlay1:", 15, 92);
        SetupRolePair(this.lblRoleOverlay2, this.lblRoleOverlay2Value, "Overlay2:", 440, 92);
        SetupRolePair(this.lblRoleOverlay3, this.lblRoleOverlay3Value, "Overlay3:", 15, 115);
        SetupRolePair(this.lblRoleOverlay4, this.lblRoleOverlay4Value, "Overlay4:", 440, 115);
        SetupRolePair(this.lblRolePromo, this.lblRolePromoValue, "Promo:", 15, 138);

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
        this.grpRoles.Controls.Add(this.lblRolePromo);
        this.grpRoles.Controls.Add(this.lblRolePromoValue);

        // grpAutomation
        this.grpAutomation.Location = new Point(12, 256);
        this.grpAutomation.Size = new Size(860, 155);
        this.grpAutomation.Text = "Automation Timing";

        this.lblNowNextInterval.AutoSize = true;
        this.lblNowNextInterval.Location = new Point(15, 26);
        this.lblNowNextInterval.Text = "NOW/NEXT Interval:";

        this.cmbNowNextInterval.Location = new Point(260, 23);
        this.cmbNowNextInterval.Size = new Size(80, 23);
        this.cmbNowNextInterval.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbNowNextInterval.Items.AddRange(new object[] { "5 min", "10 min", "20 min" });
        this.cmbNowNextInterval.SelectedItem = "10 min";

        this.lblNowNextDuration.AutoSize = true;
        this.lblNowNextDuration.Location = new Point(440, 26);
        this.lblNowNextDuration.Text = "Now/Next Duration (s):";

        this.numNowNextDuration.Location = new Point(680, 23);
        this.numNowNextDuration.Size = new Size(60, 23);
        this.numNowNextDuration.Minimum = 1;
        this.numNowNextDuration.Maximum = 120;
        this.numNowNextDuration.Value = 8;

        this.lblTriggerOffset.AutoSize = true;
        this.lblTriggerOffset.Location = new Point(15, 56);
        this.lblTriggerOffset.Text = "Trigger Offset (s):";

        this.numTriggerOffset.Location = new Point(260, 53);
        this.numTriggerOffset.Size = new Size(60, 23);
        this.numTriggerOffset.Minimum = 0;
        this.numTriggerOffset.Maximum = 300;
        this.numTriggerOffset.Value = 10;

        this.lblSongInterval.AutoSize = true;
        this.lblSongInterval.Location = new Point(440, 56);
        this.lblSongInterval.Text = "Song Interval (s):";

        this.numSongInterval.Location = new Point(680, 53);
        this.numSongInterval.Size = new Size(60, 23);
        this.numSongInterval.Minimum = 5;
        this.numSongInterval.Maximum = 3600;
        this.numSongInterval.Value = 60;

        this.lblSongDuration.AutoSize = true;
        this.lblSongDuration.Location = new Point(15, 86);
        this.lblSongDuration.Text = "Song Duration (s):";

        this.numSongDuration.Location = new Point(260, 83);
        this.numSongDuration.Size = new Size(60, 23);
        this.numSongDuration.Minimum = 1;
        this.numSongDuration.Maximum = 120;
        this.numSongDuration.Value = 8;

        this.lblPromoInterval.AutoSize = true;
        this.lblPromoInterval.Location = new Point(440, 86);
        this.lblPromoInterval.Text = "Promo Interval (min):";

        this.numPromoInterval.Location = new Point(680, 83);
        this.numPromoInterval.Size = new Size(60, 23);
        this.numPromoInterval.Minimum = 1;
        this.numPromoInterval.Maximum = 180;
        this.numPromoInterval.Value = 10;

        this.lblAdsFrom.AutoSize = true;
        this.lblAdsFrom.Location = new Point(15, 116);
        this.lblAdsFrom.Text = "Ads From:";

        this.dtpAdsFrom.Location = new Point(260, 113);
        this.dtpAdsFrom.Size = new Size(90, 23);
        this.dtpAdsFrom.Format = DateTimePickerFormat.Time;
        this.dtpAdsFrom.ShowUpDown = true;
        this.dtpAdsFrom.Value = DateTime.Today.AddHours(6);

        this.lblAdsTo.AutoSize = true;
        this.lblAdsTo.Location = new Point(440, 116);
        this.lblAdsTo.Text = "Ads To:";

        this.dtpAdsTo.Location = new Point(680, 113);
        this.dtpAdsTo.Size = new Size(90, 23);
        this.dtpAdsTo.Format = DateTimePickerFormat.Time;
        this.dtpAdsTo.ShowUpDown = true;
        this.dtpAdsTo.Value = DateTime.Today.AddHours(23);

        this.grpAutomation.Controls.Add(this.lblNowNextInterval);
        this.grpAutomation.Controls.Add(this.cmbNowNextInterval);
        this.grpAutomation.Controls.Add(this.lblNowNextDuration);
        this.grpAutomation.Controls.Add(this.numNowNextDuration);
        this.grpAutomation.Controls.Add(this.lblTriggerOffset);
        this.grpAutomation.Controls.Add(this.numTriggerOffset);
        this.grpAutomation.Controls.Add(this.lblSongInterval);
        this.grpAutomation.Controls.Add(this.numSongInterval);
        this.grpAutomation.Controls.Add(this.lblSongDuration);
        this.grpAutomation.Controls.Add(this.numSongDuration);
        this.grpAutomation.Controls.Add(this.lblPromoInterval);
        this.grpAutomation.Controls.Add(this.numPromoInterval);
        this.grpAutomation.Controls.Add(this.lblAdsFrom);
        this.grpAutomation.Controls.Add(this.dtpAdsFrom);
        this.grpAutomation.Controls.Add(this.lblAdsTo);
        this.grpAutomation.Controls.Add(this.dtpAdsTo);

        // dgvSchedule
        this.dgvSchedule.Location = new Point(12, 421);
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
        this.btnTriggerSelected.Location = new Point(12, 721);
        this.btnTriggerSelected.Size = new Size(160, 27);
        this.btnTriggerSelected.Text = "Trigger Selected Now";
        this.btnTriggerSelected.Click += new EventHandler(this.btnTriggerSelected_Click);

        this.btnViewAsRunLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnViewAsRunLog.Location = new Point(182, 721);
        this.btnViewAsRunLog.Size = new Size(160, 27);
        this.btnViewAsRunLog.Text = "View As-Run Log";
        this.btnViewAsRunLog.Click += new EventHandler(this.btnViewAsRunLog_Click);

        this.lblLog.AutoSize = true;
        this.lblLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.lblLog.Location = new Point(12, 759);
        this.lblLog.Text = "Log:";

        this.txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.txtLog.Location = new Point(12, 779);
        this.txtLog.Size = new Size(860, 110);
        this.txtLog.Multiline = true;
        this.txtLog.ReadOnly = true;
        this.txtLog.ScrollBars = ScrollBars.Vertical;

        this.tmrCheck.Interval = 1000;
        this.tmrCheck.Tick += new EventHandler(this.tmrCheck_Tick);

        this.ClientSize = new Size(884, 910);
        this.MinimumSize = new Size(700, 790);
        this.Text = "vMix Scheduler";
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        this.Controls.Add(this.lblHost);
        this.Controls.Add(this.txtHost);
        this.Controls.Add(this.lblPort);
        this.Controls.Add(this.txtPort);
        this.Controls.Add(this.btnRefreshInputs);
        this.Controls.Add(this.lblConnectionStatus);
        this.Controls.Add(this.btnStart);
        this.Controls.Add(this.btnStop);
        this.Controls.Add(this.chkAutoStart);
        this.Controls.Add(this.lblLiveStatus);
        this.Controls.Add(this.grpRoles);
        this.Controls.Add(this.grpAutomation);
        this.Controls.Add(this.dgvSchedule);
        this.Controls.Add(this.btnTriggerSelected);
        this.Controls.Add(this.btnViewAsRunLog);
        this.Controls.Add(this.lblLog);
        this.Controls.Add(this.txtLog);

        ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numNowNextDuration)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numTriggerOffset)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSongInterval)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSongDuration)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numPromoInterval)).EndInit();
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

    private Button btnStart;
    private Button btnStop;
    private CheckBox chkAutoStart;
    private Label lblLiveStatus;

    private GroupBox grpRoles;
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
    private Label lblRolePromo;
    private Label lblRolePromoValue;

    private GroupBox grpAutomation;
    private Label lblNowNextInterval;
    private ComboBox cmbNowNextInterval;
    private Label lblNowNextDuration;
    private NumericUpDown numNowNextDuration;
    private Label lblTriggerOffset;
    private NumericUpDown numTriggerOffset;
    private Label lblSongInterval;
    private NumericUpDown numSongInterval;
    private Label lblSongDuration;
    private NumericUpDown numSongDuration;
    private Label lblPromoInterval;
    private NumericUpDown numPromoInterval;
    private Label lblAdsFrom;
    private DateTimePicker dtpAdsFrom;
    private Label lblAdsTo;
    private DateTimePicker dtpAdsTo;

    private DataGridView dgvSchedule;
    private DataGridViewTextBoxColumn colRawTitle;
    private DataGridViewTextBoxColumn colDisplayName;
    private DataGridViewTextBoxColumn colCategory;
    private DataGridViewTextBoxColumn colRecurrence;
    private DataGridViewTextBoxColumn colNextOccurrence;
    private DataGridViewTextBoxColumn colStatus;

    private Button btnTriggerSelected;
    private Button btnViewAsRunLog;
    private Label lblLog;
    private TextBox txtLog;
    private System.Windows.Forms.Timer tmrCheck;
}
