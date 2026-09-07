using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace ChemSculptor.WinForms;

public sealed class MainForm : Form
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly List<ChatSession> _sessions = [];
    private readonly Dictionary<string, ClientJobItem> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };

    private readonly TextBox _serverUrlBox = new()
    {
        Text = "http://127.0.0.1:5178",
        Width = 180
    };

    private readonly Button _selectFileButton = new() { Text = "选择 txt", AutoSize = true };
    private readonly Button _saveResultButton = new() { Text = "保存结果", AutoSize = true };
    private readonly Label _fileLabel = new()
    {
        Text = "未选择文件",
        AutoSize = true,
        MaximumSize = new Size(360, 0)
    };

    private readonly Button _newSessionButton = new() { Text = "新建会话", Dock = DockStyle.Top, Height = 36 };
    private readonly ListBox _sessionList = new() { Dock = DockStyle.Fill, IntegralHeight = false };

    private readonly Panel _chatPanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = Color.FromArgb(249, 250, 251)
    };

    private readonly FlowLayoutPanel _chatFlow = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(10)
    };

    private readonly TextBox _inputBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        PlaceholderText = "用自然语言描述目标（坐标/任务请配合 txt 按钮）..."
    };

    private readonly Button _sendTextButton = new() { Text = "发送", AutoSize = true };
    private readonly Button _sendGeometryButton = new() { Text = "发送坐标", AutoSize = true };
    private readonly Button _sendJobButton = new() { Text = "提交任务", AutoSize = true };

    private ChatSession? _activeSession;
    private string? _selectedFilePath;
    private string? _latestResultText;
    private bool _polling;
    private int _sessionCounter;

    public MainForm()
    {
        Text = "ChemSculptor";
        MinimumSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BuildLayout();

        _newSessionButton.Click += (_, _) => CreateSession("新会话");
        _sessionList.SelectedIndexChanged += (_, _) => SwitchSession();
        _selectFileButton.Click += SelectFile;
        _saveResultButton.Click += SaveResult;
        _sendTextButton.Click += async (_, _) => await SendTextAsync();
        _sendGeometryButton.Click += async (_, _) => await SendGeometryAsync();
        _sendJobButton.Click += async (_, _) => await SubmitJobAsync();
        _timer.Tick += async (_, _) => await PollActiveJobsAsync();
        _timer.Start();

        CreateSession("新会话");
        AppendMessage("system", "欢迎使用 ChemSculptor。选择左侧会话，或直接在下方描述你的科研目标。");
        AppendMessage("hint", "当前为界面骨架：自然语言理解将在后续版本接入；坐标发送与任务提交已可用。");
    }

    private void BuildLayout()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(247, 248, 250),
            Padding = new Padding(8)
        };

        var sidebarTitle = new Label
        {
            Text = "会话",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var sidebarInner = new Panel { Dock = DockStyle.Fill };
        sidebarInner.Controls.Add(_sessionList);
        sidebarInner.Controls.Add(_newSessionButton);

        sidebar.Controls.Add(sidebarInner);
        sidebar.Controls.Add(sidebarTitle);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10, 6, 10, 6)
        };
        toolbar.Controls.Add(new Label
        {
            Text = "服务地址：",
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        });
        toolbar.Controls.Add(_serverUrlBox);
        toolbar.Controls.Add(_selectFileButton);
        toolbar.Controls.Add(_fileLabel);
        toolbar.Controls.Add(_saveResultButton);

        var composer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 120,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 1
        };
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        var actionColumn = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        actionColumn.Controls.Add(_sendTextButton);
        actionColumn.Controls.Add(_sendGeometryButton);
        actionColumn.Controls.Add(_sendJobButton);

        composer.Controls.Add(_inputBox, 0, 0);
        composer.Controls.Add(actionColumn, 1, 0);

        _chatPanel.Controls.Add(_chatFlow);

        var mainArea = new Panel { Dock = DockStyle.Fill };
        mainArea.Controls.Add(_chatPanel);
        mainArea.Controls.Add(composer);
        mainArea.Controls.Add(toolbar);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 250,
            FixedPanel = FixedPanel.Panel1
        };
        split.Panel1.Controls.Add(sidebar);
        split.Panel2.Controls.Add(mainArea);

        Controls.Add(split);
        Resize += (_, _) => UpdateBubbleWidths();
    }

    private void CreateSession(string title)
    {
        _sessionCounter++;
        var session = new ChatSession
        {
            Id = $"session-{_sessionCounter}",
            Title = $"{title} {DateTime.Now:MM-dd HH:mm}"
        };
        _sessions.Add(session);
        _sessionList.Items.Add(session);
        _sessionList.SelectedItem = session;
    }

    private void SwitchSession()
    {
        if (_sessionList.SelectedItem is ChatSession session)
        {
            _activeSession = session;
            RenderChat();
        }
    }

    private void RenderChat()
    {
        _chatFlow.SuspendLayout();
        _chatFlow.Controls.Clear();

        if (_activeSession is not null)
        {
            foreach (var message in _activeSession.Messages)
            {
                AddBubble(message);
            }
        }

        _chatFlow.ResumeLayout();
        UpdateBubbleWidths();
        ScrollToBottom();
    }

    private void AppendMessage(string role, string text)
    {
        _activeSession ??= _sessions[^1];
        _activeSession.Messages.Add(new ChatMessage
        {
            Role = role,
            Text = text
        });

        AddBubble(_activeSession.Messages[^1]);
        ScrollToBottom();
    }

    private void AddBubble(ChatMessage message)
    {
        var (backColor, foreColor, prefix) = message.Role switch
        {
            "user" => (Color.FromArgb(220, 235, 255), Color.FromArgb(20, 40, 80), "你"),
            "system" => (Color.White, Color.FromArgb(30, 30, 30), "ChemSculptor"),
            "error" => (Color.FromArgb(255, 235, 235), Color.FromArgb(120, 30, 30), "错误"),
            _ => (Color.FromArgb(243, 244, 246), Color.FromArgb(80, 80, 80), "提示")
        };

        var width = Math.Max(320, _chatFlow.ClientSize.Width - 40);
        var bubble = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MaximumSize = new Size(width, 0),
            BackColor = backColor,
            Padding = new Padding(10),
            Margin = new Padding(2, 3, 2, 3),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        var header = new Label
        {
            AutoSize = true,
            Text = $"{prefix} · {message.Timestamp:HH:mm:ss}",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8)
        };

        var body = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(Math.Max(280, width - 24), 0),
            Text = message.Text,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 9.5f)
        };

        bubble.Controls.Add(header);
        bubble.Controls.Add(body);
        _chatFlow.Controls.Add(bubble);
    }

    private void UpdateBubbleWidths()
    {
        if (_chatFlow.IsDisposed)
        {
            return;
        }

        var width = Math.Max(320, _chatFlow.ClientSize.Width - 40);
        foreach (Control control in _chatFlow.Controls)
        {
            control.MaximumSize = new Size(width, 0);
            foreach (Control child in control.Controls)
            {
                child.MaximumSize = new Size(Math.Max(280, width - 24), 0);
            }
        }

        _chatFlow.PerformLayout();
    }

    private void ScrollToBottom()
    {
        _chatPanel.PerformLayout();
        _chatPanel.AutoScrollPosition = new Point(0, _chatPanel.VerticalScroll.Maximum);
    }

    private void SelectFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 txt 文件",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedFilePath = dialog.FileName;
            _fileLabel.Text = dialog.FileName;
        }
    }

    private Task SendTextAsync()
    {
        var text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            AppendMessage("error", "请先在输入框写下你的目标。");
            return Task.CompletedTask;
        }

        _inputBox.Clear();
        AppendMessage("user", text);
        AppendMessage("hint", "文本已记录到当前会话。自然语言理解将在后续版本由服务器端接入。");
        return Task.CompletedTask;
    }

    private async Task SendGeometryAsync()
    {
        if (!HasSelectedFile())
        {
            return;
        }

        AppendMessage("user", $"发送坐标文件：{Path.GetFileName(_selectedFilePath!)}");

        try
        {
            var text = await File.ReadAllTextAsync(_selectedFilePath!);
            using var content = new StringContent(text, Encoding.UTF8, "text/plain");

            var response = await _http.PostAsync(Endpoint("/geometries"), content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeometrySubmitResult>();
            if (result is null || result.AtomCount == 0)
            {
                AppendMessage("error", "坐标发送失败：服务器没有返回分子数据。");
                return;
            }

            var elements = string.Join(", ", result.Atoms.Select(atom => atom.Element));
            AppendMessage("system",
                $"服务器已接收 {result.SourceName}：{result.Formula}，共 {result.AtomCount} 个原子（{elements}）。");

            foreach (var diagnostic in result.Diagnostics)
            {
                AppendMessage("hint", $"诊断：{diagnostic}");
            }
        }
        catch (Exception ex)
        {
            AppendMessage("error", $"坐标发送失败：{ex.Message}");
        }
    }

    private async Task SubmitJobAsync()
    {
        if (!HasSelectedFile())
        {
            return;
        }

        AppendMessage("user", $"提交任务文件：{Path.GetFileName(_selectedFilePath!)}");

        try
        {
            var fileName = Path.GetFileName(_selectedFilePath!);
            await using var fileStream = File.OpenRead(_selectedFilePath!);
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", fileName);

            var response = await _http.PostAsync(Endpoint("/client/jobs"), form);
            response.EnsureSuccessStatusCode();

            var summary = await response.Content.ReadFromJsonAsync<ClientJobSummary>();
            if (summary is null)
            {
                AppendMessage("error", "提交失败：服务端没有返回任务编号。");
                return;
            }

            var jobId = string.IsNullOrWhiteSpace(summary.JobId) ? summary.Id : summary.JobId;
            if (string.IsNullOrWhiteSpace(jobId))
            {
                AppendMessage("error", "提交失败：服务端没有返回任务编号。");
                return;
            }

            var job = new ClientJobItem { Id = jobId, Status = "Queued" };
            _jobs[job.Id] = job;
            AppendMessage("system", $"任务已提交：{job.Id}，状态 {job.Status}。");
            await PollActiveJobsAsync();
        }
        catch (Exception ex)
        {
            AppendMessage("error", $"提交失败：{ex.Message}");
        }
    }

    private async Task PollActiveJobsAsync()
    {
        if (_polling)
        {
            return;
        }

        _polling = true;
        try
        {
            var active = _jobs.Values.Where(job => job.Status is not ("Passed" or "Failed")).ToList();
            foreach (var job in active)
            {
                await RefreshJobAsync(job);
            }
        }
        catch (Exception ex)
        {
            AppendMessage("error", $"轮询失败：{ex.Message}");
        }
        finally
        {
            _polling = false;
        }
    }

    private async Task RefreshJobAsync(ClientJobItem job)
    {
        var statusResponse = await _http.GetAsync(Endpoint($"/client/jobs/{job.Id}/status"));
        if (!statusResponse.IsSuccessStatusCode)
        {
            return;
        }

        var summary = await statusResponse.Content.ReadFromJsonAsync<ClientJobSummary>();
        if (summary is null)
        {
            return;
        }

        var previous = job.Status;
        job.Status = string.IsNullOrWhiteSpace(summary.Status) ? job.Status : summary.Status;

        if (previous != job.Status)
        {
            AppendMessage("system", $"{job.Id} 状态：{job.Status}");
        }

        if (summary.HasResult && job.ResultText is null)
        {
            var resultResponse = await _http.GetAsync(Endpoint($"/client/jobs/{job.Id}/result"));
            if (resultResponse.IsSuccessStatusCode)
            {
                job.ResultText = await resultResponse.Content.ReadAsStringAsync();
                _latestResultText = job.ResultText;
                var preview = job.ResultText.Length > 400
                    ? job.ResultText[..400] + "..."
                    : job.ResultText;
                AppendMessage("system", $"{job.Id} 结果已就绪：{Environment.NewLine}{preview}");
            }
        }
    }

    private void SaveResult(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_latestResultText))
        {
            MessageBox.Show(this, "当前还没有已就绪的任务结果。", "ChemSculptor");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "保存结果 txt",
            FileName = $"job-result-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Filter = "文本文件 (*.txt)|*.txt"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _latestResultText);
            AppendMessage("hint", $"结果已保存到 {dialog.FileName}");
        }
    }

    private bool HasSelectedFile()
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            AppendMessage("error", "请先在顶部选择 txt 文件。");
            return false;
        }

        return true;
    }

    private Uri Endpoint(string path)
    {
        var baseUrl = _serverUrlBox.Text.Trim().TrimEnd('/');
        return new Uri(baseUrl + path, UriKind.Absolute);
    }
}
