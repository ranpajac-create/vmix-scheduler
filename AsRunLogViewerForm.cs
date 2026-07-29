namespace VmixScheduler;

public partial class AsRunLogViewerForm : Form
{
    public AsRunLogViewerForm()
    {
        InitializeComponent();
        dtpLogDate.Value = DateTime.Today; // triggers ValueChanged -> LoadLog
    }

    private void dtpLogDate_ValueChanged(object? sender, EventArgs e) => LoadLog(dtpLogDate.Value.Date);

    private void btnRefresh_Click(object? sender, EventArgs e) => LoadLog(dtpLogDate.Value.Date);

    private void btnOpenFolder_Click(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.AsRunLogDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppPaths.AsRunLogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't open the folder — {ex.Message}", "As-Run Log Viewer",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadLog(DateTime date)
    {
        dgvAsRunLog.Rows.Clear();
        var path = Path.Combine(AppPaths.AsRunLogDirectory, $"as-run-{date:yyyy-MM-dd}.csv");

        if (!File.Exists(path))
        {
            lblStatus.Text = "No log file for this date.";
            return;
        }

        try
        {
            var lines = File.ReadAllLines(path);
            int rowCount = 0;
            // First line is the header row written by RecordAsRun — not data.
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = CsvUtil.ParseLine(line);
                while (fields.Count < 5) fields.Add("");
                dgvAsRunLog.Rows.Add(fields[0], fields[1], fields[2], fields[3], fields[4]);
                rowCount++;
            }
            lblStatus.Text = $"{rowCount} row(s)";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Failed to read log — {ex.Message}";
        }
    }
}
