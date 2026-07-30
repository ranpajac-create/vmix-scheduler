namespace VmixScheduler;

public partial class ActivationForm : Form
{
    private readonly LicenseService _licenseService;

    public ActivationForm(LicenseService licenseService)
    {
        _licenseService = licenseService;
        InitializeComponent();
    }

    private async void btnActivate_Click(object? sender, EventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            ShowStatus("Enter a license key.", isError: true);
            return;
        }

        btnActivate.Enabled = false;
        txtLicenseKey.Enabled = false;
        ShowStatus("Activating...", isError: false);

        LicenseActivationResult result;
        try
        {
            result = await _licenseService.ActivateAsync(key);
        }
        catch (Exception ex)
        {
            result = LicenseActivationResult.Fail($"Unexpected error — {ex.Message}");
        }

        if (result.Success)
        {
            ShowStatus("Activated.", isError: false);
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        btnActivate.Enabled = true;
        txtLicenseKey.Enabled = true;
        ShowStatus(result.ErrorMessage ?? "Activation failed.", isError: true);
    }

    private void ShowStatus(string message, bool isError)
    {
        lblStatus.ForeColor = isError ? Color.Firebrick : Color.DimGray;
        lblStatus.Text = message;
    }
}
