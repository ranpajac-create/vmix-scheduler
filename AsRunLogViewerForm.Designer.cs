namespace VmixScheduler;

partial class AsRunLogViewerForm
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

        this.lblDate = new Label();
        this.dtpLogDate = new DateTimePicker();
        this.btnRefresh = new Button();
        this.btnOpenFolder = new Button();
        this.lblStatus = new Label();

        this.dgvAsRunLog = new DataGridView();
        this.colTimestamp = new DataGridViewTextBoxColumn();
        this.colTriggerType = new DataGridViewTextBoxColumn();
        this.colCategory = new DataGridViewTextBoxColumn();
        this.colDisplayName = new DataGridViewTextBoxColumn();
        this.colRawTitle = new DataGridViewTextBoxColumn();

        ((System.ComponentModel.ISupportInitialize)(this.dgvAsRunLog)).BeginInit();
        this.SuspendLayout();

        this.lblDate.AutoSize = true;
        this.lblDate.Location = new Point(12, 18);
        this.lblDate.Text = "Date:";

        this.dtpLogDate.Location = new Point(55, 14);
        this.dtpLogDate.Size = new Size(150, 23);
        this.dtpLogDate.Format = DateTimePickerFormat.Short;

        this.btnRefresh.Location = new Point(215, 12);
        this.btnRefresh.Size = new Size(90, 27);
        this.btnRefresh.Text = "Refresh";
        this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

        this.btnOpenFolder.Location = new Point(315, 12);
        this.btnOpenFolder.Size = new Size(120, 27);
        this.btnOpenFolder.Text = "Open Folder";
        this.btnOpenFolder.Click += new EventHandler(this.btnOpenFolder_Click);

        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new Point(450, 18);
        this.lblStatus.ForeColor = Color.DimGray;
        this.lblStatus.Text = "";

        this.dgvAsRunLog.Location = new Point(12, 50);
        this.dgvAsRunLog.Size = new Size(860, 430);
        this.dgvAsRunLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.dgvAsRunLog.AllowUserToAddRows = false;
        this.dgvAsRunLog.AllowUserToDeleteRows = false;
        this.dgvAsRunLog.ReadOnly = true;
        this.dgvAsRunLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvAsRunLog.MultiSelect = false;
        this.dgvAsRunLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvAsRunLog.RowHeadersVisible = false;
        this.dgvAsRunLog.Columns.AddRange(new DataGridViewColumn[] {
            this.colTimestamp, this.colTriggerType, this.colCategory, this.colDisplayName, this.colRawTitle });

        this.colTimestamp.HeaderText = "Timestamp";
        this.colTimestamp.Name = "colTimestamp";
        this.colTimestamp.FillWeight = 18;

        this.colTriggerType.HeaderText = "Trigger Type";
        this.colTriggerType.Name = "colTriggerType";
        this.colTriggerType.FillWeight = 14;

        this.colCategory.HeaderText = "Category";
        this.colCategory.Name = "colCategory";
        this.colCategory.FillWeight = 14;

        this.colDisplayName.HeaderText = "Display Name";
        this.colDisplayName.Name = "colDisplayName";
        this.colDisplayName.FillWeight = 27;

        this.colRawTitle.HeaderText = "Raw Title";
        this.colRawTitle.Name = "colRawTitle";
        this.colRawTitle.FillWeight = 27;

        // dtpLogDate's ValueChanged is wired here (not above) so it fires only after
        // InitializeComponent finishes constructing the rest of the form.
        this.dtpLogDate.ValueChanged += new EventHandler(this.dtpLogDate_ValueChanged);

        this.ClientSize = new Size(884, 500);
        this.MinimumSize = new Size(700, 400);
        this.Text = "As-Run Log Viewer";
        this.Controls.Add(this.lblDate);
        this.Controls.Add(this.dtpLogDate);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.btnOpenFolder);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.dgvAsRunLog);

        ((System.ComponentModel.ISupportInitialize)(this.dgvAsRunLog)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private Label lblDate;
    private DateTimePicker dtpLogDate;
    private Button btnRefresh;
    private Button btnOpenFolder;
    private Label lblStatus;
    private DataGridView dgvAsRunLog;
    private DataGridViewTextBoxColumn colTimestamp;
    private DataGridViewTextBoxColumn colTriggerType;
    private DataGridViewTextBoxColumn colCategory;
    private DataGridViewTextBoxColumn colDisplayName;
    private DataGridViewTextBoxColumn colRawTitle;
}
