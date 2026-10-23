namespace KayBrowser;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("shell32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetCurrentProcessExplicitAppUserModelID(string id);
    [STAThread]
    static void Main(string[] args)
    {
        SetCurrentProcessExplicitAppUserModelID("TheKay.CleanBrowser");
        ApplicationConfiguration.Initialize();
        if (args.Contains("--update-receipt"))
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "update-receipt.json"), System.Text.Json.JsonSerializer.Serialize(new { version = Updater.CurrentVersion.ToString(3), process = Environment.ProcessPath, status = "restarted" })); return;
        }
        if (args.Contains("--update-smoke"))
        {
            AppContext.SetData("KayBrowser.DiagnosticsRoot", Path.Combine(Path.GetTempPath(), "kay-update-check-" + Guid.NewGuid().ToString("N")));
            try
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4));
                var update = Updater.Check(Distribution.DefaultRepository, deadline.Token).GetAwaiter().GetResult() ?? throw new InvalidOperationException("Nenhuma versão mais nova para o teste.");
                Updater.Install(update, cancellation: deadline.Token, diagnostics: true).GetAwaiter().GetResult();
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "update-failure.json"), ex.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Contains("--layout-check"))
        {
            AppContext.SetData("KayBrowser.DiagnosticsRoot", Path.Combine(Path.GetTempPath(), "kay-layout-check-" + Guid.NewGuid().ToString("N")));
            Environment.ExitCode = ChromeDiagnostics.Run(); return;
        }
        Application.ThreadException += (_, e) => UiMessage.Show(Form.ActiveForm, Palette.Create(false, Color.MediumAquamarine), e.Exception.Message, "Clean Browser · ocorreu um erro");
        using var mutex = new Mutex(true, "Local\\KayBrowser.Main", out var first);
        if (!first) { UiMessage.Show(null, Palette.Create(false, Color.MediumAquamarine), "O Clean Browser já está aberto."); return; }
        Application.Run(new BrowserWindow());
    }
}
