using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChemSculptor.WinForms;

/// <summary>
/// ChemSculptor 客户端主窗口。
/// 界面分为左侧会话列表、中间对话区、底部输入区。
/// </summary>
public sealed class MainForm : Form
{
    // HTTP 客户端：用于与本地或远程 ChemSculptor.Api 通信。
    private readonly HttpClient _http;

    // 会话与任务运行状态。
    private readonly List<ChatSession> _sessions;
    private readonly Dictionary<string, CalculationJobItem> _calculations;
    private readonly System.Windows.Forms.Timer _timer;

    // 界面控件。
    private readonly TextBox _serverUrlBox;
    private readonly Button _selectFileButton;
    private readonly Button _saveResultButton;
    private readonly Label _fileLabel;
    private readonly Button _newSessionButton;
    private readonly ListBox _sessionList;
    private readonly Panel _chatPanel;
    private readonly FlowLayoutPanel _chatFlow;
    private readonly TextBox _inputBox;
    private readonly Button _sendTextButton;
    private readonly Button _cancelJobButton;

    private ChatSession? _activeSession;
    private string? _selectedFilePath;
    private string? _latestResultText;
    private string _activeCalculationId = string.Empty;
    private bool _polling;
    private int _sessionCounter;

    /// <summary>初始化窗口、控件与事件订阅。</summary>
    public MainForm()
    {
        _http = new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(30);

        _sessions = new List<ChatSession>();
        _calculations = new Dictionary<string, CalculationJobItem>(
            StringComparer.OrdinalIgnoreCase);
        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1500;

        _serverUrlBox = new TextBox();
        _serverUrlBox.Text = "http://127.0.0.1:5178";
        _serverUrlBox.Width = 180;

        _selectFileButton = new Button();
        _selectFileButton.Text = "选择 txt";
        _selectFileButton.AutoSize = true;

        _saveResultButton = new Button();
        _saveResultButton.Text = "保存结果";
        _saveResultButton.AutoSize = true;

        _fileLabel = new Label();
        _fileLabel.Text = "未选择文件";
        _fileLabel.AutoSize = true;
        _fileLabel.MaximumSize = new Size(360, 0);

        _newSessionButton = new Button();
        _newSessionButton.Text = "新建会话";
        _newSessionButton.Dock = DockStyle.Top;
        _newSessionButton.Height = 36;

        _sessionList = new ListBox();
        _sessionList.Dock = DockStyle.Fill;
        _sessionList.IntegralHeight = false;

        _chatPanel = new Panel();
        _chatPanel.Dock = DockStyle.Fill;
        _chatPanel.AutoScroll = true;
        _chatPanel.BackColor = Color.FromArgb(249, 250, 251);

        _chatFlow = new FlowLayoutPanel();
        _chatFlow.Dock = DockStyle.Top;
        _chatFlow.AutoSize = true;
        _chatFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _chatFlow.FlowDirection = FlowDirection.TopDown;
        _chatFlow.WrapContents = false;
        _chatFlow.Padding = new Padding(10);

        _inputBox = new TextBox();
        _inputBox.Dock = DockStyle.Fill;
        _inputBox.Multiline = true;
        _inputBox.ScrollBars = ScrollBars.Vertical;
        _inputBox.PlaceholderText = "用自然语言描述目标（坐标/任务请配合 txt 按钮）...";

        _sendTextButton = new Button();
        _sendTextButton.Text = "发送";
        _sendTextButton.AutoSize = true;

        _cancelJobButton = new Button();
        _cancelJobButton.Text = "取消计算";
        _cancelJobButton.AutoSize = true;
        _cancelJobButton.Enabled = false;

        Text = "ChemSculptor";
        MinimumSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();

        _newSessionButton.Click += OnNewSessionClick;
        _sessionList.SelectedIndexChanged += OnSessionIndexChanged;
        _selectFileButton.Click += OnSelectFileClick;
        _saveResultButton.Click += OnSaveResultClick;
        _sendTextButton.Click += OnSendTextClick;
        _cancelJobButton.Click += OnCancelJobClick;
        _timer.Tick += OnTimerTick;
        _timer.Start();

        CreateSession("新会话");
        AppendMessage("system", "欢迎使用 ChemSculptor。请选择坐标 txt，并在下方描述你的科研目标。");
        AppendMessage("hint", "提交后客户端会轮询计算状态，并显示结果和验证报告。");
    }

    /// <summary>构建三区界面布局。</summary>
    private void BuildLayout()
    {
        Panel sidebar = new Panel();
        sidebar.Dock = DockStyle.Fill;
        sidebar.BackColor = Color.FromArgb(247, 248, 250);
        sidebar.Padding = new Padding(8);

        Label sidebarTitle = new Label();
        sidebarTitle.Text = "会话";
        sidebarTitle.Dock = DockStyle.Top;
        sidebarTitle.Height = 32;
        sidebarTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        sidebarTitle.TextAlign = ContentAlignment.MiddleLeft;

        Panel sidebarInner = new Panel();
        sidebarInner.Dock = DockStyle.Fill;
        sidebarInner.Controls.Add(_sessionList);
        sidebarInner.Controls.Add(_newSessionButton);

        sidebar.Controls.Add(sidebarInner);
        sidebar.Controls.Add(sidebarTitle);

        FlowLayoutPanel toolbar = new FlowLayoutPanel();
        toolbar.Dock = DockStyle.Top;
        toolbar.AutoSize = true;
        toolbar.Padding = new Padding(10, 6, 10, 6);

        Label serverLabel = new Label();
        serverLabel.Text = "服务地址：";
        serverLabel.AutoSize = true;
        serverLabel.Padding = new Padding(0, 6, 0, 0);

        toolbar.Controls.Add(serverLabel);
        toolbar.Controls.Add(_serverUrlBox);
        toolbar.Controls.Add(_selectFileButton);
        toolbar.Controls.Add(_fileLabel);
        toolbar.Controls.Add(_saveResultButton);

        TableLayoutPanel composer = new TableLayoutPanel();
        composer.Dock = DockStyle.Bottom;
        composer.Height = 120;
        composer.Padding = new Padding(10);
        composer.ColumnCount = 2;
        composer.RowCount = 1;
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        FlowLayoutPanel actionColumn = new FlowLayoutPanel();
        actionColumn.Dock = DockStyle.Fill;
        actionColumn.FlowDirection = FlowDirection.TopDown;
        actionColumn.WrapContents = false;
        actionColumn.Controls.Add(_sendTextButton);
        actionColumn.Controls.Add(_cancelJobButton);

        composer.Controls.Add(_inputBox, 0, 0);
        composer.Controls.Add(actionColumn, 1, 0);

        _chatPanel.Controls.Add(_chatFlow);

        Panel mainArea = new Panel();
        mainArea.Dock = DockStyle.Fill;
        mainArea.Controls.Add(_chatPanel);
        mainArea.Controls.Add(composer);
        mainArea.Controls.Add(toolbar);

        SplitContainer split = new SplitContainer();
        split.Dock = DockStyle.Fill;
        split.Orientation = Orientation.Vertical;
        split.SplitterDistance = 250;
        split.FixedPanel = FixedPanel.Panel1;
        split.Panel1.Controls.Add(sidebar);
        split.Panel2.Controls.Add(mainArea);

        Controls.Add(split);
        Resize += OnFormResize;
    }

    /// <summary>窗口尺寸变化时重新计算消息卡片宽度。</summary>
    private void OnFormResize(object? sender, EventArgs e)
    {
        UpdateBubbleWidths();
    }

    /// <summary>新建会话按钮事件。</summary>
    private void OnNewSessionClick(object? sender, EventArgs e)
    {
        CreateSession("新会话");
    }

    /// <summary>切换会话事件。</summary>
    private void OnSessionIndexChanged(object? sender, EventArgs e)
    {
        SwitchSession();
    }

    /// <summary>选择 txt 文件事件。</summary>
    private void OnSelectFileClick(object? sender, EventArgs e)
    {
        SelectFile();
    }

    /// <summary>保存最近结果事件。</summary>
    private void OnSaveResultClick(object? sender, EventArgs e)
    {
        SaveResult();
    }

    /// <summary>发送自然语言文本事件。</summary>
    private async void OnSendTextClick(object? sender, EventArgs e)
    {
        await SendTextAsync();
    }

    /// <summary>取消当前计算事件。</summary>
    private async void OnCancelJobClick(object? sender, EventArgs e)
    {
        await CancelActiveCalculationAsync();
    }

    /// <summary>定时轮询任务状态事件。</summary>
    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await PollActiveCalculationsAsync();
    }

    /// <summary>创建一个新的本地会话并选中它。</summary>
    private void CreateSession(string title)
    {
        _sessionCounter++;

        ChatSession session = new ChatSession();
        session.Id = "session-" + _sessionCounter.ToString();
        session.Title = title + " " + DateTime.Now.ToString("MM-dd HH:mm");

        _sessions.Add(session);
        _sessionList.Items.Add(session);
        _sessionList.SelectedItem = session;
    }

    /// <summary>切换到列表中选择的会话并刷新消息。</summary>
    private void SwitchSession()
    {
        if (_sessionList.SelectedItem == null)
        {
            return;
        }

        ChatSession? session = _sessionList.SelectedItem as ChatSession;
        if (session == null)
        {
            return;
        }

        _activeSession = session;
        RenderChat();
    }

    /// <summary>清空并重新绘制当前会话的全部消息。</summary>
    private void RenderChat()
    {
        _chatFlow.SuspendLayout();
        _chatFlow.Controls.Clear();

        if (_activeSession != null)
        {
            for (int index = 0; index < _activeSession.Messages.Count; index++)
            {
                AddBubble(_activeSession.Messages[index]);
            }
        }

        _chatFlow.ResumeLayout();
        UpdateBubbleWidths();
        ScrollToBottom();
    }

    /// <summary>向当前会话追加一条消息。</summary>
    private void AppendMessage(string role, string text)
    {
        if (_activeSession == null)
        {
            _activeSession = _sessions[_sessions.Count - 1];
        }

        ChatMessage message = new ChatMessage();
        message.Role = role;
        message.Text = text;
        message.Timestamp = DateTimeOffset.Now;

        _activeSession.Messages.Add(message);
        AddBubble(message);
        ScrollToBottom();
    }

    /// <summary>把一条消息渲染成对话卡片。</summary>
    private void AddBubble(ChatMessage message)
    {
        Color backColor = Color.FromArgb(243, 244, 246);
        Color foreColor = Color.FromArgb(80, 80, 80);
        string prefix = "提示";

        if (message.Role == "user")
        {
            backColor = Color.FromArgb(220, 235, 255);
            foreColor = Color.FromArgb(20, 40, 80);
            prefix = "你";
        }
        else if (message.Role == "system")
        {
            backColor = Color.White;
            foreColor = Color.FromArgb(30, 30, 30);
            prefix = "ChemSculptor";
        }
        else if (message.Role == "error")
        {
            backColor = Color.FromArgb(255, 235, 235);
            foreColor = Color.FromArgb(120, 30, 30);
            prefix = "错误";
        }

        int width = Math.Max(320, _chatFlow.ClientSize.Width - 40);

        FlowLayoutPanel bubble = new FlowLayoutPanel();
        bubble.AutoSize = true;
        bubble.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bubble.MaximumSize = new Size(width, 0);
        bubble.BackColor = backColor;
        bubble.Padding = new Padding(10);
        bubble.Margin = new Padding(2, 3, 2, 3);
        bubble.FlowDirection = FlowDirection.TopDown;
        bubble.WrapContents = false;

        Label header = new Label();
        header.AutoSize = true;
        header.Text = prefix + " · " + message.Timestamp.ToString("HH:mm:ss");
        header.ForeColor = Color.Gray;
        header.Font = new Font("Segoe UI", 8);

        Label body = new Label();
        body.AutoSize = true;
        body.MaximumSize = new Size(Math.Max(280, width - 24), 0);
        body.Text = message.Text;
        body.ForeColor = foreColor;
        body.Font = new Font("Segoe UI", 9.5f);

        bubble.Controls.Add(header);
        bubble.Controls.Add(body);
        _chatFlow.Controls.Add(bubble);
    }

    /// <summary>根据窗口宽度调整所有消息卡片的换行宽度。</summary>
    private void UpdateBubbleWidths()
    {
        if (_chatFlow.IsDisposed)
        {
            return;
        }

        int width = Math.Max(320, _chatFlow.ClientSize.Width - 40);

        for (int index = 0; index < _chatFlow.Controls.Count; index++)
        {
            Control control = _chatFlow.Controls[index];
            control.MaximumSize = new Size(width, 0);

            for (int childIndex = 0; childIndex < control.Controls.Count; childIndex++)
            {
                Control child = control.Controls[childIndex];
                child.MaximumSize = new Size(Math.Max(280, width - 24), 0);
            }
        }

        _chatFlow.PerformLayout();
    }

    /// <summary>把对话区滚动到底部。</summary>
    private void ScrollToBottom()
    {
        _chatPanel.PerformLayout();
        _chatPanel.AutoScrollPosition = new Point(0, _chatPanel.VerticalScroll.Maximum);
    }

    /// <summary>选择一个 txt 文件作为坐标或任务输入。</summary>
    private void SelectFile()
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Title = "选择 txt 文件";
        dialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";

        DialogResult result = dialog.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            _selectedFilePath = dialog.FileName;
            _fileLabel.Text = dialog.FileName;
        }

        dialog.Dispose();
    }

    /// <summary>把用户自然语言原样发送给服务器。</summary>
    private async Task SendTextAsync()
    {
        string text = _inputBox.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            AppendMessage("error", "请先在输入框写下你的目标。");
            return;
        }

        _inputBox.Clear();
        AppendMessage("user", text);
        await SendAgentMessageAsync(text);
    }

    /// <summary>
    /// 把原始自然语言和当前坐标文件发送给服务器。
    /// 任务类型由服务器解释，客户端不做判断。
    /// </summary>
    private async Task SendAgentMessageAsync(string text)
    {
        string filePath;
        if (!TryGetSelectedFile(out filePath))
        {
            return;
        }

        try
        {
            string coordinateText = await File.ReadAllTextAsync(filePath);

            AgentMessageRequestDto request = new AgentMessageRequestDto();
            if (_activeSession == null)
            {
                request.SessionId = string.Empty;
            }
            else
            {
                request.SessionId = _activeSession.Id;
            }

            request.Text = text;
            request.CoordinateText = coordinateText;

            string json = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response =
                await _http.PostAsync(Endpoint("/agent/messages"), content);

            if (!response.IsSuccessStatusCode)
            {
                string errorText = await response.Content.ReadAsStringAsync();
                AppendMessage("error", "服务器处理失败：" + errorText);
                return;
            }

            AgentMessageResultDto? result =
                await response.Content.ReadFromJsonAsync<AgentMessageResultDto>();

            if (result == null)
            {
                AppendMessage("error", "服务器没有返回结果。");
                return;
            }

            if (string.IsNullOrWhiteSpace(result.JobId))
            {
                AppendMessage("error", "服务器没有返回计算作业标识。");
                return;
            }

            AppendMessage(
                "system",
                "任务类型：" + result.TaskType +
                "，作业：" + result.JobId +
                "，状态：" + result.Status + "。");
            AppendMessage("hint", "输入文件：" + result.InputFilePath);
            AppendMessage("hint", "输出文件：" + result.OutputFilePath);
            AppendMessage("hint", result.Message);

            for (int index = 0; index < result.Diagnostics.Count; index++)
            {
                AppendMessage("hint", "诊断：" + result.Diagnostics[index]);
            }

            CalculationJobItem calculation = new CalculationJobItem();
            calculation.JobId = result.JobId;
            calculation.State = result.Status;
            _calculations[calculation.JobId] = calculation;
            _activeCalculationId = calculation.JobId;
            _cancelJobButton.Enabled = true;

            await RefreshCalculationAsync(calculation);
        }
        catch (Exception ex)
        {
            AppendMessage("error", "服务器处理失败：" + ex.Message);
        }
    }

    /// <summary>轮询所有未结束计算作业的状态。</summary>
    private async Task PollActiveCalculationsAsync()
    {
        if (_polling)
        {
            return;
        }

        _polling = true;

        try
        {
            List<CalculationJobItem> activeCalculations =
                new List<CalculationJobItem>();

            foreach (KeyValuePair<string, CalculationJobItem> pair in _calculations)
            {
                if (!pair.Value.IsFinished)
                {
                    activeCalculations.Add(pair.Value);
                }
            }

            for (int index = 0; index < activeCalculations.Count; index++)
            {
                await RefreshCalculationAsync(activeCalculations[index]);
            }
        }
        catch (Exception ex)
        {
            AppendMessage("error", "轮询失败：" + ex.Message);
        }
        finally
        {
            _polling = false;
        }
    }

    /// <summary>刷新单个计算作业并在结束时读取结果和验证报告。</summary>
    private async Task RefreshCalculationAsync(CalculationJobItem calculation)
    {
        HttpResponseMessage statusResponse =
            await _http.GetAsync(
                Endpoint("/calculations/" + calculation.JobId + "/status"));

        if (!statusResponse.IsSuccessStatusCode)
        {
            return;
        }

        CalculationStatusDto? status =
            await statusResponse.Content.ReadFromJsonAsync<CalculationStatusDto>();

        if (status == null)
        {
            return;
        }

        string previousState = calculation.State;

        if (!string.IsNullOrWhiteSpace(status.State))
        {
            calculation.State = status.State;
        }

        if (!string.Equals(
            previousState,
            calculation.State,
            StringComparison.OrdinalIgnoreCase))
        {
            AppendMessage(
                "system",
                calculation.JobId + " 状态：" + calculation.State);
        }

        if (!IsTerminalState(calculation.State)
            || calculation.ResultText != null)
        {
            return;
        }

        calculation.IsFinished = true;

        if (string.Equals(
            _activeCalculationId,
            calculation.JobId,
            StringComparison.OrdinalIgnoreCase))
        {
            _activeCalculationId = string.Empty;
            _cancelJobButton.Enabled = false;
        }

        if (string.Equals(
            calculation.State,
            "Canceled",
            StringComparison.OrdinalIgnoreCase))
        {
            calculation.ResultText = "计算已取消。";
            _latestResultText = calculation.ResultText;
            AppendMessage("system", calculation.ResultText);
            return;
        }

        CalculationResultDto? result = null;
        CalculationValidationDto? validation = null;

        HttpResponseMessage resultResponse =
            await _http.GetAsync(
                Endpoint("/calculations/" + calculation.JobId + "/result"));

        if (resultResponse.IsSuccessStatusCode)
        {
            result =
                await resultResponse.Content.ReadFromJsonAsync<CalculationResultDto>();
        }

        HttpResponseMessage validationResponse =
            await _http.GetAsync(
                Endpoint("/calculations/" + calculation.JobId + "/validation"));

        if (validationResponse.IsSuccessStatusCode)
        {
            validation =
                await validationResponse.Content.ReadFromJsonAsync<CalculationValidationDto>();
        }

        calculation.ResultText = BuildCalculationResultText(
            calculation,
            result,
            validation);
        _latestResultText = calculation.ResultText;
        AppendMessage(
            "system",
            calculation.JobId + " 计算结束：" +
            Environment.NewLine +
            calculation.ResultText);
    }

    /// <summary>取消当前正在运行的计算。</summary>
    private async Task CancelActiveCalculationAsync()
    {
        if (string.IsNullOrWhiteSpace(_activeCalculationId))
        {
            AppendMessage("error", "当前没有正在运行的计算作业。");
            return;
        }

        try
        {
            HttpResponseMessage response = await _http.PostAsync(
                Endpoint(
                    "/calculations/" +
                    _activeCalculationId +
                    "/cancel"),
                null);

            if (!response.IsSuccessStatusCode)
            {
                string errorText = await response.Content.ReadAsStringAsync();
                AppendMessage("error", "取消失败：" + errorText);
                return;
            }

            AppendMessage("system", "已请求取消作业：" + _activeCalculationId);
            _cancelJobButton.Enabled = false;
        }
        catch (Exception ex)
        {
            AppendMessage("error", "取消失败：" + ex.Message);
        }
    }

    private static bool IsTerminalState(string state)
    {
        return string.Equals(state, "Validated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "Canceled", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCalculationResultText(
        CalculationJobItem calculation,
        CalculationResultDto? result,
        CalculationValidationDto? validation)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("作业：" + calculation.JobId);
        builder.AppendLine("状态：" + calculation.State);

        if (result != null)
        {
            builder.AppendLine();
            builder.AppendLine("--- 计算结果 ---");

            if (result.Energy.HasValue)
            {
                builder.AppendLine(
                    "能量：" +
                    result.Energy.Value.ToString(
                        "G17",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " " +
                    result.EnergyUnit);
            }

            builder.AppendLine("程序：" + result.Program);
            builder.AppendLine("方法：" + result.Method);
            builder.AppendLine("基组：" + result.Basis);
            builder.AppendLine("正常终结：" + result.NormalTermination.ToString());
            builder.AppendLine("失败类别：" + result.FailureKind);
            builder.AppendLine("输出文件：" + result.OutputFilePath);

            for (int index = 0; index < result.Diagnostics.Count; index++)
            {
                CalculationDiagnosticDto diagnostic = result.Diagnostics[index];
                builder.AppendLine(
                    "诊断：" + diagnostic.Code + " - " + diagnostic.Message);
            }
        }
        else
        {
            builder.AppendLine("结果尚未就绪。");
        }

        if (validation != null)
        {
            builder.AppendLine();
            builder.AppendLine("--- 验证报告 ---");
            builder.AppendLine("验证：" + validation.Status);
            builder.AppendLine(validation.Summary);

            for (int index = 0; index < validation.Checks.Count; index++)
            {
                CalculationValidationCheckDto check = validation.Checks[index];

                if (!check.Passed)
                {
                    builder.AppendLine(
                        "未通过：" +
                        check.Code +
                        " [" +
                        check.Requirement +
                        "] - " +
                        check.Message);
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>把最近一次任务结果保存为用户选择的 txt 文件。</summary>
    private void SaveResult()
    {
        if (string.IsNullOrWhiteSpace(_latestResultText))
        {
            MessageBox.Show(this, "当前还没有已就绪的任务结果。", "ChemSculptor");
            return;
        }

        SaveFileDialog dialog = new SaveFileDialog();
        dialog.Title = "保存结果 txt";
        dialog.FileName = "job-result-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
        dialog.Filter = "文本文件 (*.txt)|*.txt";

        DialogResult result = dialog.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _latestResultText);
            AppendMessage("hint", "结果已保存到 " + dialog.FileName);
        }

        dialog.Dispose();
    }

    /// <summary>检查是否已选择有效文件，并返回其路径。</summary>
    private bool TryGetSelectedFile(out string filePath)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath))
        {
            AppendMessage("error", "请先在顶部选择 txt 文件。");
            filePath = string.Empty;
            return false;
        }

        if (!File.Exists(_selectedFilePath))
        {
            AppendMessage("error", "请先在顶部选择 txt 文件。");
            filePath = string.Empty;
            return false;
        }

        filePath = _selectedFilePath;
        return true;
    }

    /// <summary>根据服务地址和路径拼出完整请求地址。</summary>
    private Uri Endpoint(string path)
    {
        string baseUrl = _serverUrlBox.Text.Trim().TrimEnd('/');
        return new Uri(baseUrl + path, UriKind.Absolute);
    }
}
