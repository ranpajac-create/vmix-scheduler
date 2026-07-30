namespace VmixScheduler;

partial class ActivationForm
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

        this.lblExplain = new Label();
        this.txtLicenseKey = new TextBox();
        this.btnActivate = new Button();
        this.lblStatus = new Label();

        this.SuspendLayout();

        this.lblExplain.AutoSize = false;
        this.lblExplain.Location = new Point(12, 12);
        this.lblExplain.Size = new Size(360, 40);
        this.lblExplain.Text = "vMix Scheduler needs a license key to run. Enter the key you received when you purchased it below.";

        this.txtLicenseKey.Location = new Point(12, 58);
        this.txtLicenseKey.Size = new Size(360, 23);
        this.txtLicenseKey.PlaceholderText = "XXXXXXXX-XXXXXXXX-XXXXXXXX-XXXXXXXX";

        this.btnActivate.Location = new Point(12, 90);
        this.btnActivate.Size = new Size(120, 30);
        this.btnActivate.Text = "Activate";
        this.btnActivate.Click += new EventHandler(this.btnActivate_Click);

        this.lblStatus.AutoSize = false;
        this.lblStatus.Location = new Point(144, 94);
        this.lblStatus.Size = new Size(228, 42);
        this.lblStatus.ForeColor = Color.DimGray;
        this.lblStatus.Text = "";

        this.AcceptButton = this.btnActivate;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(384, 141);
        this.Text = "vMix Scheduler — Activation Required";
        this.Controls.Add(this.lblExplain);
        this.Controls.Add(this.txtLicenseKey);
        this.Controls.Add(this.btnActivate);
        this.Controls.Add(this.lblStatus);

        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private Label lblExplain;
    private TextBox txtLicenseKey;
    private Button btnActivate;
    private Label lblStatus;
}
