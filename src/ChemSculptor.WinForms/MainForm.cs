using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ChemSculptor.WinForms;

public sealed class MainForm : Form
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly Dictionary<string, ClientJobItem> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };

    private readonly TextBox _serverUrlBox = new()
    {
        Text = "http://127.0.0.1:5178",
        Width = 220
    };

    private readonly Button _selectFileButton = new() { Text = "选择 txt", AutoSize = true };
    private readonly Button _submitButton = new() { Text = "提交给 ChemSculptor", AutoSize = true };
    private readonly Button _sendGeometryButton = new() { Text = "发送坐标", AutoSize = true };
    private readonly Button _saveResultButton = new() { Text = "保存结果", AutoSize = true };
    private readonly Label _selectedFileLabel = new() { Text = "未选择文件", AutoSize = true };
    private readonly ListBox _jobList = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = false
    };

    private string? _selectedFilePath;
    private bool _polling;

    public MainForm()
    {
        Text = "ChemSculptor WinForms 客户端";
        MinimumSize = new Size(980, 600);
        BuildLayout();

        _selectFileButton.Click += SelectFile;
        _submitButton.Click += async (_, _) => await SubmitAsync();
        _sendGeometryButton.Click += async (_, _) => await SendGeometryAsync();
        _saveResultButton.Click += SaveResult;
        _timer.Tick += async (_, _) => await PollActiveJobsAsync();
        _timer.Start();
    }

    private void BuildLayout()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8, 6, 8, 6)
        };

        toolbar.Controls.Add(new Label { Text = "服务地址：", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        toolbar.Controls.Add(_serverUrlBox);
        toolbar.Controls.Add(_selectFileButton);
        toolbar.Controls.Add(_submitButton);
        toolbar.Controls.Add(_sendGeometryButton);
        toolbar.Controls.Add(_saveResultButton);
        toolbar.Controls.Add(_selectedFileLabel);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 380
        };
        split.Panel1.Controls.Add(_jobList);
        split.Panel2.Controls.Add(_outputBox);

        Controls.Add(split);
        Controls.Add(toolbar);
    }

    private void SelectFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择客户输入 txt 文件",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedFilePath = dialog.FileName;
            _selectedFileLabel.Text = dialog.FileName;
        }
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            MessageBox.Show(this, "请先选择一个 txt 文件。", "ChemSculptor 客户端");
            return;
        }

        try
        {
            var fileName = Path.GetFileName(_selectedFilePath);
            await using var fileStream = File.OpenRead(_selectedFilePath);
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", fileName);

            var response = await _http.PostAsync(Endpoint("/client/jobs"), form);
            response.EnsureSuccessStatusCode();

            var summary = await response.Content.ReadFromJsonAsync<ClientJobSummary>();
            if (summary is null)
            {
                AppendOutput("提交失败：服务端没有返回任务编号。");
                return;
            }

            var jobId = string.IsNullOrWhiteSpace(summary.JobId) ? summary.Id : summary.JobId;
            if (string.IsNullOrWhiteSpace(jobId))
            {
                AppendOutput("提交失败：服务端没有返回任务编号。");
                return;
            }

            var job = new ClientJobItem { Id = jobId, Status = "Queued" };
            _jobs[job.Id] = job;
            AppendOutput($"已提交 {fileName} -> {job.Id}");
            RefreshJobList();
            await PollActiveJobsAsync();
        }
        catch (Exception ex)
        {
            AppendOutput($"提交失败：{ex.Message}");
        }
    }

    private async Task SendGeometryAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            MessageBox.Show(this, "请先选择一个包含分子坐标的 txt 文件。", "ChemSculptor 客户端");
            return;
        }

        try
        {
            var text = await File.ReadAllTextAsync(_selectedFilePath);
            using var content = new StringContent(text, Encoding.UTF8, "text/plain");

            var response = await _http.PostAsync(Endpoint("/geometries"), content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeometrySubmitResult>();
            if (result is null || result.AtomCount == 0)
            {
                AppendOutput("坐标发送失败：服务器没有返回分子数据。");
                return;
            }

            var elements = string.Join(", ", result.Atoms.Select(atom => atom.Element));
            AppendOutput($"服务器已接收 {result.SourceName}：{result.Formula}，共 {result.AtomCount} 个原子（{elements}）");

            foreach (var diagnostic in result.Diagnostics)
            {
                AppendOutput($"诊断：{diagnostic}");
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"坐标发送失败：{ex.Message}");
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
            AppendOutput($"轮询失败：{ex.Message}");
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
            AppendOutput($"{job.Id} -> {job.Status}");
        }

        if (summary.HasResult && job.ResultText is null)
        {
            var resultResponse = await _http.GetAsync(Endpoint($"/client/jobs/{job.Id}/result"));
            if (resultResponse.IsSuccessStatusCode)
            {
                job.ResultText = await resultResponse.Content.ReadAsStringAsync();
                AppendOutput($"{job.Id} 结果已就绪（{job.ResultText.Length} 字符）");
            }
        }

        RefreshJobList();
    }

    private void RefreshJobList()
    {
        _jobList.BeginUpdate();
        _jobList.Items.Clear();
        foreach (var job in _jobs.Values.OrderByDescending(job => job.Id, StringComparer.Ordinal))
        {
            _jobList.Items.Add(job);
        }

        _jobList.EndUpdate();
    }

    private void SaveResult(object? sender, EventArgs e)
    {
        if (_jobList.SelectedItem is not ClientJobItem job || string.IsNullOrWhiteSpace(job.ResultText))
        {
            MessageBox.Show(this, "请先选择一个已有结果的客户端任务。", "ChemSculptor 客户端");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "保存结果 txt",
            FileName = $"{job.Id}.result.txt",
            Filter = "文本文件 (*.txt)|*.txt"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, job.ResultText);
            AppendOutput($"结果已保存到 {dialog.FileName}");
        }
    }

    private Uri Endpoint(string path)
    {
        var baseUrl = _serverUrlBox.Text.Trim().TrimEnd('/');
        return new Uri(baseUrl + path, UriKind.Absolute);
    }

    private void AppendOutput(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendOutput(line));
            return;
        }

        _outputBox.AppendText($"{DateTime.Now:HH:mm:ss}  {line}{Environment.NewLine}");
    }
}
