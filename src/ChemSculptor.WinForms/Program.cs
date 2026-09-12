namespace ChemSculptor.WinForms;

/// <summary>
/// WinForms 客户端入口。
/// </summary>
internal static class Program
{
    /// <summary>初始化 WinForms 并打开主窗口。</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
