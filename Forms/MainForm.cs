using System.Diagnostics;
using System.Text.Json;
using RunPodClient.Models;
using RunPodClient.Services;

namespace RunPodClient.Forms;

public class MainForm : Form
{
    // ── State ─────────────────────────────────────────────────────────────────
    private AppSettings _settings = SettingsService.Load();
    private RunPodService? _api;
    private CancellationTokenSource? _cts;
    private string? _currentJobId;
    private EndpointType _selectedEndpoint = EndpointType.Image;

    // ── Controls ──────────────────────────────────────────────────────────────
    private Panel       _sidebar        = null!;
    private Button      _btnImage       = null!;
    private Button      _btnVideo       = null!;
    private Button      _btnSettings    = null!;

    private Label       _lblEndpoint    = null!;
    private Label       _lblJobId       = null!;
    private Label       _lblStatus      = null!;

    private RichTextBox _txtPrompt      = null!;
    private RichTextBox _txtWorkflow    = null!;
    private TabControl  _inputTabs      = null!;

    private Button      _btnSubmit      = null!;
    private Button      _btnCancel      = null!;
    private Button      _btnOpenFolder  = null!;

    private ProgressBar _progressBar    = null!;
    private ListBox     _lstResults     = null!;
    private PictureBox  _picPreview     = null!;
    private RichTextBox _txtLog         = null!;

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainForm()
    {
        BuildUI();
        RefreshEndpointLabel();
        EnsureApiClient();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI BUILD
    // ══════════════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        Text            = "RunPod Client";
        Size            = new Size(1100, 720);
        MinimumSize     = new Size(900, 600);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Color.FromArgb(18, 20, 28);
        ForeColor       = Color.FromArgb(220, 225, 240);
        Font            = new Font("Segoe UI", 9.5f);

        // ── Root split: sidebar | content ────────────────────────────────────
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        // ── SIDEBAR ──────────────────────────────────────────────────────────
        _sidebar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 14, 20),
            Padding   = new Padding(0, 8, 0, 8)
        };
        root.Controls.Add(_sidebar, 0, 0);

        BuildSidebar();

        // ── CONTENT ──────────────────────────────────────────────────────────
        var content = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            BackColor   = Color.FromArgb(18, 20, 28)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // status bar
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 45));    // input area
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 55));    // output area
        root.Controls.Add(content, 1, 0);

        BuildStatusBar(content);
        BuildInputArea(content);
        BuildOutputArea(content);
    }

    // ── Sidebar ───────────────────────────────────────────────────────────────
    private void BuildSidebar()
    {
        var logo = new Label
        {
            Text      = "🎮  RunPod\nClient",
            Dock      = DockStyle.Top,
            Height    = 70,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 130, 230),
            Padding   = new Padding(0, 12, 0, 0)
        };

        var sep1 = MakeSep();

        var navLabel = new Label
        {
            Text      = "ENDPOINT",
            Dock      = DockStyle.Top,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 90, 110),
            Padding   = new Padding(16, 0, 0, 0)
        };

        _btnImage = MakeNavButton("🖼️  Image", true);
        _btnVideo = MakeNavButton("🎬  Video", false);
        _btnImage.Click += (_, _) => SelectEndpoint(EndpointType.Image);
        _btnVideo.Click += (_, _) => SelectEndpoint(EndpointType.Video);

        var sep2 = MakeSep();

        _btnSettings = MakeNavButton("⚙️  Settings", false);
        _btnSettings.Click += BtnSettings_Click;

        var btnOpenFolder = MakeNavButton("📁  Open Output", false);
        btnOpenFolder.Click += (_, _) =>
        {
            if (Directory.Exists(_settings.OutputFolder))
                Process.Start("explorer.exe", _settings.OutputFolder);
        };

        // Stack top-down (add in reverse since Dock=Top)
        foreach (var c in new Control[]
            { btnOpenFolder, _btnSettings, sep2, _btnVideo, _btnImage, navLabel, sep1, logo })
        {
            _sidebar.Controls.Add(c);
        }
    }

    // ── Status bar ────────────────────────────────────────────────────────────
    private void BuildStatusBar(TableLayoutPanel parent)
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 14, 20),
            Padding   = new Padding(14, 0, 14, 0)
        };

        _lblEndpoint = new Label
        {
            AutoSize  = false,
            Width     = 220,
            Dock      = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(140, 150, 180),
            Font      = new Font("Segoe UI", 9f)
        };

        _lblJobId = new Label
        {
            AutoSize  = false,
            Width     = 320,
            Dock      = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(100, 110, 140),
            Font      = new Font("Cascadia Code, Consolas", 8.5f)
        };

        _lblStatus = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(80, 200, 120),
            Font      = new Font("Segoe UI", 9f)
        };

        bar.Controls.Add(_lblStatus);
        bar.Controls.Add(_lblJobId);
        bar.Controls.Add(_lblEndpoint);
        parent.Controls.Add(bar, 0, 0);
    }

    // ── Input area ────────────────────────────────────────────────────────────
    private void BuildInputArea(TableLayoutPanel parent)
    {
        var card = MakeCard();
        parent.Controls.Add(card, 0, 1);

        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            Padding     = new Padding(16, 12, 16, 8)
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        card.Controls.Add(inner);

        // Title
        inner.Controls.Add(MakeSectionLabel("📤  Input"), 0, 0);

        // Tabs: Prompt | Workflow JSON
        _inputTabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons
        };
        StyleTabControl(_inputTabs);

        var tabPrompt   = new TabPage("  Prompt  ") { BackColor = Color.FromArgb(13, 15, 20), Padding = new Padding(6) };
        var tabWorkflow = new TabPage("  Workflow JSON  ") { BackColor = Color.FromArgb(13, 15, 20), Padding = new Padding(6) };

        _txtPrompt = MakeRichBox();
        _txtPrompt.Text = "a pixel art warrior standing on a hilltop at sunset, 16-bit style";
        tabPrompt.Controls.Add(_txtPrompt);

        _txtWorkflow = MakeRichBox();
        _txtWorkflow.Font = new Font("Cascadia Code, Consolas", 9f);
        _txtWorkflow.Text = "{\n  \"paste your ComfyUI API-export workflow JSON here\"\n}";
        tabWorkflow.Controls.Add(_txtWorkflow);

        _inputTabs.TabPages.Add(tabPrompt);
        _inputTabs.TabPages.Add(tabWorkflow);
        inner.Controls.Add(_inputTabs, 0, 1);

        // Action buttons
        var btnRow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding       = new Padding(0, 6, 0, 0)
        };

        _btnSubmit = MakeActionButton("▶  Submit Job", Color.FromArgb(100, 70, 180));
        _btnCancel = MakeActionButton("✖  Cancel", Color.FromArgb(140, 40, 40));
        _btnCancel.Enabled = false;

        _progressBar = new ProgressBar
        {
            Width   = 140,
            Height  = 30,
            Style   = ProgressBarStyle.Marquee,
            Visible = false,
            Margin  = new Padding(8, 0, 0, 0)
        };

        _btnSubmit.Click += BtnSubmit_Click;
        _btnCancel.Click += BtnCancel_Click;

        btnRow.Controls.Add(_btnSubmit);
        btnRow.Controls.Add(_btnCancel);
        btnRow.Controls.Add(_progressBar);
        inner.Controls.Add(btnRow, 0, 2);
    }

    // ── Output area ───────────────────────────────────────────────────────────
    private void BuildOutputArea(TableLayoutPanel parent)
    {
        var split = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 340,
            BackColor   = Color.FromArgb(22, 25, 35),
            Panel1MinSize = 200,
            Panel2MinSize = 200
        };
        parent.Controls.Add(split, 0, 2);

        // LEFT — results list + preview
        var leftPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            Padding     = new Padding(8, 8, 4, 8)
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        split.Panel1.Controls.Add(leftPanel);

        leftPanel.Controls.Add(MakeSectionLabel("📋  Results"), 0, 0);

        _lstResults = new ListBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(13, 15, 20),
            ForeColor = Color.FromArgb(180, 190, 210),
            BorderStyle = BorderStyle.None,
            Font      = new Font("Cascadia Code, Consolas", 8.5f)
        };
        _lstResults.SelectedIndexChanged += LstResults_SelectedIndexChanged;
        leftPanel.Controls.Add(_lstResults, 0, 1);

        _picPreview = new PictureBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 12, 18),
            SizeMode  = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.None,
            Cursor    = Cursors.Hand
        };
        _picPreview.Click += PicPreview_Click;
        leftPanel.Controls.Add(_picPreview, 0, 2);

        // RIGHT — log
        var rightPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 2,
            ColumnCount = 1,
            Padding     = new Padding(4, 8, 8, 8)
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(rightPanel);

        var logHeader = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        logHeader.Controls.Add(MakeSectionLabel("📋  Log"));
        var btnClearLog = MakeSmallButton("Clear");
        btnClearLog.Click += (_, _) => _txtLog.Clear();
        logHeader.Controls.Add(btnClearLog);
        rightPanel.Controls.Add(logHeader, 0, 0);

        _txtLog = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = Color.FromArgb(8, 10, 14),
            ForeColor   = Color.FromArgb(160, 175, 200),
            BorderStyle = BorderStyle.None,
            Font        = new Font("Cascadia Code, Consolas", 8.5f),
            ReadOnly    = true,
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };
        rightPanel.Controls.Add(_txtLog, 0, 1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LOGIC
    // ══════════════════════════════════════════════════════════════════════════

    private void SelectEndpoint(EndpointType type)
    {
        _selectedEndpoint = type;
        _btnImage.BackColor = type == EndpointType.Image
            ? Color.FromArgb(40, 30, 70) : Color.Transparent;
        _btnVideo.BackColor = type == EndpointType.Video
            ? Color.FromArgb(40, 30, 70) : Color.Transparent;
        RefreshEndpointLabel();
        Log($"Switched to {type} endpoint.");
    }

    private void RefreshEndpointLabel()
    {
        var id = _selectedEndpoint == EndpointType.Image
            ? _settings.ImageEndpointId
            : _settings.VideoEndpointId;

        _lblEndpoint.Text = _selectedEndpoint == EndpointType.Image
            ? $"🖼️  Image  |  {(string.IsNullOrEmpty(id) ? "not configured" : id)}"
            : $"🎬  Video  |  {(string.IsNullOrEmpty(id) ? "not configured" : id)}";
    }

    private void EnsureApiClient()
    {
        _api?.Dispose();
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            _api = new RunPodService(_settings.ApiKey);
    }

    // ── Submit ────────────────────────────────────────────────────────────────
    private async void BtnSubmit_Click(object? sender, EventArgs e)
    {
        if (_api is null)
        {
            MessageBox.Show("Please configure your RunPod API key in Settings.",
                "Not configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var endpointId = _selectedEndpoint == EndpointType.Image
            ? _settings.ImageEndpointId
            : _settings.VideoEndpointId;

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            MessageBox.Show($"Please configure the {_selectedEndpoint} Endpoint ID in Settings.",
                "Not configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Build input payload
        object payload;
        if (_inputTabs.SelectedIndex == 1)
        {
            // Workflow JSON tab
            try
            {
                payload = JsonSerializer.Deserialize<object>(_txtWorkflow.Text)
                    ?? throw new InvalidOperationException("Empty workflow.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid workflow JSON:\n{ex.Message}",
                    "JSON Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        else
        {
            // Simple prompt
            payload = new { prompt = _txtPrompt.Text.Trim() };
        }

        SetBusy(true);
        _cts = new CancellationTokenSource();

        try
        {
            Log($"Submitting job to {_selectedEndpoint} endpoint ({endpointId})…");
            var job = await _api.SubmitJobAsync(endpointId, payload, _cts.Token);

            _currentJobId     = job.Id;
            _lblJobId.Text    = $"Job: {job.Id}";
            _lblStatus.Text   = job.Status;
            Log($"Job submitted: {job.Id}  status={job.Status}");

            // Poll until done
            var result = await _api.WaitForCompletionAsync(
                endpointId, job.Id,
                _settings.PollIntervalMs,
                _settings.TimeoutSeconds,
                s =>
                {
                    Invoke(() =>
                    {
                        _lblStatus.Text = s;
                        Log(s);
                    });
                },
                _cts.Token);

            _lblStatus.ForeColor = result.Status == "COMPLETED"
                ? Color.FromArgb(60, 200, 100)
                : Color.FromArgb(220, 80, 80);
            _lblStatus.Text = result.Status;

            if (result.Status == "COMPLETED" && result.Output is not null)
            {
                Log($"Job completed in {result.ExecutionTime ?? 0} ms. Downloading results…");
                await DownloadResultsAsync(result.Output, job.Id);
            }
            else if (result.Error is not null)
            {
                Log($"❌ Error: {result.Error}");
            }
        }
        catch (OperationCanceledException)
        {
            Log("⚠️  Job cancelled.");
            _lblStatus.Text = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"❌ {ex.Message}");
            _lblStatus.Text = "Error";
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ── Cancel ────────────────────────────────────────────────────────────────
    private async void BtnCancel_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();

        if (_api is not null && _currentJobId is not null)
        {
            var endpointId = _selectedEndpoint == EndpointType.Image
                ? _settings.ImageEndpointId
                : _settings.VideoEndpointId;
            try
            {
                await _api.CancelJobAsync(endpointId, _currentJobId);
                Log($"Cancel request sent for job {_currentJobId}.");
            }
            catch (Exception ex)
            {
                Log($"Cancel request failed: {ex.Message}");
            }
        }
    }

    // ── Download results ──────────────────────────────────────────────────────
    private async Task DownloadResultsAsync(RunPodOutput output, string jobId)
    {
        Directory.CreateDirectory(_settings.OutputFolder);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        int idx = 0;

        // Images
        if (output.Images is { Count: > 0 })
        {
            foreach (var img in output.Images)
            {
                idx++;
                string filename = img.Filename ?? $"{jobId}_{timestamp}_{idx}.png";
                string dest     = Path.Combine(_settings.OutputFolder, filename);

                byte[] bytes;
                if (!string.IsNullOrEmpty(img.Url))
                {
                    Log($"Downloading image {idx}: {img.Url}");
                    bytes = await _api!.DownloadBytesAsync(img.Url);
                }
                else if (!string.IsNullOrEmpty(img.Base64))
                {
                    Log($"Decoding base64 image {idx}…");
                    bytes = RunPodService.DecodeBase64(img.Base64);
                }
                else continue;

                await File.WriteAllBytesAsync(dest, bytes);
                Log($"✅ Saved: {dest}");
                Invoke(() => AddResult(dest));
            }
        }

        // Video
        if (!string.IsNullOrEmpty(output.Video))
        {
            string filename = $"{jobId}_{timestamp}.mp4";
            string dest     = Path.Combine(_settings.OutputFolder, filename);
            byte[] bytes;

            if (output.Video.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Log($"Downloading video: {output.Video}");
                bytes = await _api!.DownloadBytesAsync(output.Video);
            }
            else
            {
                Log("Decoding base64 video…");
                bytes = RunPodService.DecodeBase64(output.Video);
            }

            await File.WriteAllBytesAsync(dest, bytes);
            Log($"✅ Saved: {dest}");
            Invoke(() => AddResult(dest));
        }

        if (idx == 0 && string.IsNullOrEmpty(output.Video))
            Log("ℹ️  No downloadable output found in response.");
    }

    // ── Results list / preview ────────────────────────────────────────────────
    private void AddResult(string path)
    {
        _lstResults.Items.Insert(0, path);
        _lstResults.SelectedIndex = 0;
    }

    private void LstResults_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lstResults.SelectedItem is not string path) return;
        if (!File.Exists(path)) return;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp")
        {
            try
            {
                _picPreview.Image?.Dispose();
                _picPreview.Image = Image.FromFile(path);
            }
            catch { }
        }
        else
        {
            _picPreview.Image = null;
        }
    }

    private void PicPreview_Click(object? sender, EventArgs e)
    {
        if (_lstResults.SelectedItem is string path && File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // ── Settings ──────────────────────────────────────────────────────────────
    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new SettingsForm(_settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings = SettingsService.Load();
            EnsureApiClient();
            RefreshEndpointLabel();
            Log("Settings saved.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetBusy(bool busy)
    {
        _btnSubmit.Enabled    = !busy;
        _btnCancel.Enabled    = busy;
        _progressBar.Visible  = busy;
        _lblStatus.ForeColor  = busy
            ? Color.FromArgb(240, 180, 60)
            : Color.FromArgb(60, 200, 100);
        if (!busy) _lblStatus.Text = "Ready";
    }

    private void Log(string message)
    {
        if (InvokeRequired) { Invoke(() => Log(message)); return; }
        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  {message}\n");
        _txtLog.ScrollToCaret();
    }

    // ── Control factories ─────────────────────────────────────────────────────
    private static Panel MakeCard() => new()
    {
        Dock      = DockStyle.Fill,
        BackColor = Color.FromArgb(22, 25, 35),
        Padding   = new Padding(0)
    };

    private static Label MakeSectionLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
        ForeColor = Color.FromArgb(160, 130, 230)
    };

    private static Panel MakeSep() => new()
    {
        Dock      = DockStyle.Top,
        Height    = 1,
        BackColor = Color.FromArgb(35, 40, 55)
    };

    private Button MakeNavButton(string text, bool active) => new()
    {
        Text      = text,
        Dock      = DockStyle.Top,
        Height    = 40,
        FlatStyle = FlatStyle.Flat,
        BackColor = active ? Color.FromArgb(40, 30, 70) : Color.Transparent,
        ForeColor = Color.FromArgb(200, 210, 230),
        TextAlign = ContentAlignment.MiddleLeft,
        Padding   = new Padding(16, 0, 0, 0),
        Font      = new Font("Segoe UI", 9.5f),
        Cursor    = Cursors.Hand
    };

    private static Button MakeActionButton(string text, Color bg) => new()
    {
        Text      = text,
        Height    = 34,
        AutoSize  = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = bg,
        ForeColor = Color.White,
        Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Cursor    = Cursors.Hand,
        Padding   = new Padding(10, 0, 10, 0),
        Margin    = new Padding(0, 0, 8, 0)
    };

    private static Button MakeSmallButton(string text) => new()
    {
        Text      = text,
        Height    = 22,
        AutoSize  = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(35, 40, 55),
        ForeColor = Color.FromArgb(160, 170, 190),
        Font      = new Font("Segoe UI", 8f),
        Cursor    = Cursors.Hand,
        Margin    = new Padding(6, 2, 0, 0)
    };

    private static RichTextBox MakeRichBox() => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = Color.FromArgb(13, 15, 20),
        ForeColor   = Color.FromArgb(210, 220, 240),
        BorderStyle = BorderStyle.None,
        Font        = new Font("Segoe UI", 9.5f),
        ScrollBars  = RichTextBoxScrollBars.Vertical
    };

    private static void StyleTabControl(TabControl tc)
    {
        tc.BackColor = Color.FromArgb(13, 15, 20);
        tc.Dock      = DockStyle.Fill;
    }
}
