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
        int probeIndex=Array.IndexOf(args,"--startup-check");
        if(probeIndex>=0)
        {
            if(probeIndex+1>=args.Length||!Guid.TryParseExact(args[probeIndex+1],"N",out _)){Environment.ExitCode=1;return;}
            string token=args[probeIndex+1];
            AppContext.SetData("KayBrowser.DiagnosticsRoot",Path.Combine(Path.GetTempPath(),"kay-startup-check-"+token));
            using var probe=new BrowserWindow(diagnostics:true);
            probe.Shown+=async(_,_)=>{
                try
                {
                    if(!await probe.VerifyStartup().WaitAsync(TimeSpan.FromSeconds(45)))throw new IOException("WebView2 indispon�vel.");
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory,".clean-health-"+token+".json"),System.Text.Json.JsonSerializer.Serialize(new{version=Updater.CurrentVersion.ToString(3),status="healthy"}));
                }
                catch{Environment.ExitCode=1;}
                finally{probe.Close();}
            };
            Application.Run(probe);return;
        }
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
                var update = Updater.Check(Distribution.DefaultRepository, deadline.Token).GetAwaiter().GetResult() ?? throw new InvalidOperationException("Nenhuma vers�o mais nova para o teste.");
                Updater.Install(update, cancellation: deadline.Token, diagnostics: true).GetAwaiter().GetResult();
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "update-failure.json"), ex.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if(args.Contains("--apps-check"))
        {
            AppContext.SetData("KayBrowser.DiagnosticsRoot",Path.Combine(Path.GetTempPath(),"kay-apps-check-"+Guid.NewGuid().ToString("N")));
            Environment.ExitCode=WebAppsCompatibility.Run();return;
        }
        if (args.Contains("--layout-check") || args.Contains("--render-check"))
        {
            AppContext.SetData("KayBrowser.DiagnosticsRoot", Path.Combine(Path.GetTempPath(), "kay-layout-check-" + Guid.NewGuid().ToString("N")));
            try { Environment.ExitCode = ChromeDiagnostics.Run(!args.Contains("--render-check")); }
            catch(Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout-check.json"), System.Text.Json.JsonSerializer.Serialize(new { status="FAIL", error=ex.ToString() })); Environment.ExitCode=1; }
            return;
        }
        Application.ThreadException += (_, e) => UiMessage.Show(Form.ActiveForm, Palette.Create(false, Color.MediumAquamarine), e.Exception.Message, "Clean Browser � ocorreu um erro");
        using var mutex = new Mutex(true, "Local\\KayBrowser.Main", out var first);
        if (!first) { UiMessage.Show(null, Palette.Create(false, Color.MediumAquamarine), "O Clean Browser j� est� aberto."); return; }
        Application.Run(new BrowserWindow());
    }
}
