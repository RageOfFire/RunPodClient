using RunPodClient.Models;
using RunPodClient.Services;

namespace RunPodClient.Forms;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;

    // Controls
    private TextBox _txtApiKey        = null!;
    private TextBox _txtImageEndpoint = null!;
    private TextBox _txtVideoEndpoint = null!;
    private TextBox _txtOutputFolder  = null!;
    private NumericUpDown _numPoll    = null!;
    private NumericUpDown _numTimeout = null!;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        BuildUI();
        LoadValues();
    }

    private void BuildUI()
    {
        Text            = "Settings";
        Size            = new Size(520, 380);
        MinimumSize     = new Size(520, 380);
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Color.FromArgb(18, 20, 28);
        ForeColor       = Color.FromArgb(220, 225, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Font            = new Font("Segoe UI", 9.5f);

        var panel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            Padding     = new Padding(20),
            AutoSize    = true,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // API Key
        AddRow(panel, row++, "RunPod API Key:",
            _txtApiKey = MakeTextBox(passwordChar: '●'));

        // Image Endpoint
        AddRow(panel, row++, "Image Endpoint ID:",
            _txtImageEndpoint = MakeTextBox());

        // Video Endpoint
        AddRow(panel, row++, "Video Endpoint ID:",
            _txtVideoEndpoint = MakeTextBox());

        // Output folder
        var folderPanel = new Panel { Dock = DockStyle.Fill };
        _txtOutputFolder = MakeTextBox();
        _txtOutputFolder.Width = 260;
        var btnBrowse = MakeButton("…", 30);
        btnBrowse.Click += BtnBrowse_Click;
        _txtOutputFolder.Dock = DockStyle.Fill;
        btnBrowse.Dock = DockStyle.Right;
        folderPanel.Controls.Add(_txtOutputFolder);
        folderPanel.Controls.Add(btnBrowse);
        AddRow(panel, row++, "Output Folder:", folderPanel);

        // Poll interval
        _numPoll = new NumericUpDown
        {
            Minimum   = 500,
            Maximum   = 30000,
            Increment = 500,
            Value     = 3000,
            BackColor = Color.FromArgb(13, 15, 20),
            ForeColor = Color.FromArgb(220, 225, 240),
            Dock      = DockStyle.Fill
        };
        AddRow(panel, row++, "Poll interval (ms):", _numPoll);

        // Timeout
        _numTimeout = new NumericUpDown
        {
            Minimum   = 30,
            Maximum   = 3600,
            Increment = 30,
            Value     = 300,
            BackColor = Color.FromArgb(13, 15, 20),
            ForeColor = Color.FromArgb(220, 225, 240),
            Dock      = DockStyle.Fill
        };
        AddRow(panel, row++, "Timeout (seconds):", _numTimeout);

        Controls.Add(panel);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height        = 50,
            Padding       = new Padding(10, 8, 10, 8),
            BackColor     = Color.FromArgb(13, 15, 20)
        };

        var btnSave   = MakeButton("Save", 90);
        var btnCancel = MakeButton("Cancel", 90);
        btnSave.BackColor   = Color.FromArgb(100, 70, 180);
        btnSave.Click      += BtnSave_Click;
        btnCancel.Click    += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(btnPanel);
    }

    private void LoadValues()
    {
        _txtApiKey.Text        = _settings.ApiKey;
        _txtImageEndpoint.Text = _settings.ImageEndpointId;
        _txtVideoEndpoint.Text = _settings.VideoEndpointId;
        _txtOutputFolder.Text  = _settings.OutputFolder;
        _numPoll.Value         = Math.Clamp(_settings.PollIntervalMs, 500, 30000);
        _numTimeout.Value      = Math.Clamp(_settings.TimeoutSeconds, 30, 3600);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _settings.ApiKey          = _txtApiKey.Text.Trim();
        _settings.ImageEndpointId = _txtImageEndpoint.Text.Trim();
        _settings.VideoEndpointId = _txtVideoEndpoint.Text.Trim();
        _settings.OutputFolder    = _txtOutputFolder.Text.Trim();
        _settings.PollIntervalMs  = (int)_numPoll.Value;
        _settings.TimeoutSeconds  = (int)_numTimeout.Value;
        SettingsService.Save(_settings);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description         = "Select output folder",
            SelectedPath        = _txtOutputFolder.Text,
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            _txtOutputFolder.Text = dlg.SelectedPath;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddRow(TableLayoutPanel panel, int row, string label, Control ctrl)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var lbl = new Label
        {
            Text      = label,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 8, 0),
            ForeColor = Color.FromArgb(140, 150, 180)
        };
        panel.Controls.Add(lbl,  0, row);
        panel.Controls.Add(ctrl, 1, row);
    }

    private static TextBox MakeTextBox(char passwordChar = '\0') => new()
    {
        Dock            = DockStyle.Fill,
        BackColor       = Color.FromArgb(13, 15, 20),
        ForeColor       = Color.FromArgb(220, 225, 240),
        BorderStyle     = BorderStyle.FixedSingle,
        PasswordChar    = passwordChar,
        Font            = new Font("Segoe UI", 9.5f)
    };

    private static Button MakeButton(string text, int width) => new()
    {
        Text      = text,
        Width     = width,
        BackColor = Color.FromArgb(35, 40, 55),
        ForeColor = Color.FromArgb(220, 225, 240),
        FlatStyle = FlatStyle.Flat,
        Cursor    = Cursors.Hand,
        Font      = new Font("Segoe UI", 9.5f)
    };
}
