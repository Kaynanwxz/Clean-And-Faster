using System.Text.Json;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal void PrepareDiagnosticPage()
    {
        var tab = new BrowserTab();tab.Page.WorkspaceId=WorkspaceId; tabs.Add(tab); tabStrip.TabPages.Add(tab.Page);
    }
    internal void SetDiagnosticAssistant(bool open)
    {
        Motion.Enabled=false;
        if(open!=assistantOpen)ToggleAssistant();
        ApplyChromeMetrics();
    }
    internal void SaveChromeScreenshot(string name)
    {
        // Capture only our native controls; never capture unrelated foreground windows.
        // WebView2 content is validated through its local DOM fixture, not DrawToBitmap.
        using var bitmap=new Bitmap(Width,Height);
        DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory,name));
    }
    internal void VerifyChromeLayout()
    {
        ApplyChromeMetrics();
        void Layout(Control control)
        { control.PerformLayout(); foreach (Control c in control.Controls) Layout(c); }
        Layout(this);
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        foreach (var table in new[] { titleRow!, windowButtons!, navigationRow! })
        {
            Check(table.RowCount == 1 && table.RowStyles.Count == 1 && table.RowStyles[0].SizeType == SizeType.Percent, "Barra sem linha de altura limitada.");
            foreach (Control child in table.Controls)
                Check(child.Left >= 0 && child.Top >= 0 && child.Right <= table.ClientSize.Width && child.Bottom <= table.ClientSize.Height, $"Controle cortado: {child.AccessibleName ?? child.GetType().Name}; controle={child.Bounds}, pai={table.ClientRectangle}");
        }
        Check(locationBox!.ClientRectangle.Contains(address.Bounds), "Campo de endereço fora da barra.");
        Check(locationBox.ClientRectangle.Contains(connectionIcon!.Bounds), "Ícone de conexão fora da barra.");
        Check(!address.Bounds.IntersectsWith(connectionIcon.Bounds), "Texto do endereço sobreposto ao ícone.");
        Check(chromeLayout.GetRowHeights()[0] >= Ui(store.Settings.Compact||store.Settings.FocusMode?40:46), "Linha de abas insuficiente.");
        Check(navigationRow!.Height >= Ui(store.Settings.Compact||store.Settings.FocusMode?50:58), "Linha de navegação insuficiente.");
        Check(pageHost.Top >= navigationRow.Bottom, "Conteúdo web sobreposto ao topo.");
        if(assistantOpen)
        {
            Check(assistant!=null&&assistant.Width>=Ui(340),"Painel IA estreito demais.");
            Check(tabStrip.Width>=Ui(store.Settings.VerticalTabs?380:420),"Painel IA cortou conteúdo web.");
            Check(assistant!.Right<=pageHost.Width,"Painel IA fora da janela.");
        }
        foreach(var home in tabs.SelectMany(t=>t.Page.Controls.OfType<HomeBoard>()))home.VerifyLayout();
        var page = new BrowserPage { Caption = "YouTube · teste de título" };
        tabStrip.TabPages.Add(page); Check(page.Caption == "YouTube · teste de título", "Título da aba foi perdido.");
        tabStrip.TabPages.Remove(page); page.Dispose();
    }
}
public static class ChromeDiagnostics
{
    public static int Run(bool physicalInput=true)
    {
        int exit=1;var result=new List<object>();using var window=new BrowserWindow(diagnostics:true);
        // Use the same message loop and synchronization context as the real application.
        window.Shown+=async(_,_)=>
        {
            try
            {
                window.PrepareDiagnosticPage();await Task.Delay(40);
                foreach(var size in new[]{new Size(900,600),new Size(1320,860),new Size(1920,1080)})
                {
                    window.WindowState=FormWindowState.Normal;window.Size=size;await Task.Delay(40);window.VerifyChromeLayout();window.SaveChromeScreenshot($"layout-{size.Width}x{size.Height}.png");
                    result.Add(new{width=window.Width,height=window.Height,dpi=window.DeviceDpi,status="PASS"});
                }
                window.Size=new Size(900,600);window.Location=new Point(20,20);window.SetDiagnosticAssistant(true);await Task.Delay(40);window.VerifyChromeLayout();window.SaveChromeScreenshot("layout-assistant-900.png");result.Add(new{mode="assistant-open-900",status="PASS"});
                window.SetDiagnosticAssistant(false);await Task.Delay(40);window.VerifyChromeLayout();window.SaveChromeScreenshot("layout-focused.png");
                using(var other=new Form{Text="Focus test",Size=new Size(180,100),StartPosition=FormStartPosition.Manual,Location=new Point(window.Right-185,window.Bottom-105)})
                {
                    other.Show();other.Activate();await Task.Delay(40);window.VerifyChromeLayout();window.SaveChromeScreenshot("layout-inactive.png");result.Add(new{mode="focus-lost",status="PASS"});other.Close();window.Activate();await Task.Delay(40);window.VerifyChromeLayout();result.Add(new{mode="focus-returned",status="PASS"});
                }
                window.WindowState=FormWindowState.Maximized;await Task.Delay(40);window.VerifyChromeLayout();result.Add(new{mode="maximized",status="PASS"});window.WindowState=FormWindowState.Normal;await Task.Delay(40);window.VerifyChromeLayout();result.Add(new{mode="restored",status="PASS"});
                await window.VerifyAgentDom().WaitAsync(TimeSpan.FromSeconds(80));result.Add(new{mode="agent-dom-read-fill-click-stale-and-dpapi",status="PASS"});
                int appChecks=await window.VerifyWebApps().WaitAsync(TimeSpan.FromSeconds(60));result.Add(new{mode="apps-catalog-real-webview-cookie-reuse-layout-and-pointillism",status="PASS",checks=appChecks});
                int aiChecks=await window.VerifyIntegratedAi().WaitAsync(TimeSpan.FromSeconds(60));result.Add(new{mode="ai-provider-conversation-image-selected-window-crop",status="PASS",checks=aiChecks});
                if(physicalInput){var interactions=await window.VerifyInteractions().WaitAsync(TimeSpan.FromSeconds(180));result.Add(new{mode="native-buttons-menu-outside-escape-appearance-motion",status="PASS",checks=interactions.Length});}
                else result.Add(new{mode="physical-input",status="SKIPPED",reason="Render-only diagnostic; requires an interactive foreground desktop."});
                int stabilityChecks=await window.VerifyStabilityFixes();result.Add(new{mode="stability-fullscreen-panels-recovery-suspension",status="PASS",checks=stabilityChecks});
                int motionChecks=await window.VerifyCleanMotion();result.Add(new{mode="clean-tab-menu-and-native-window-motion",status="PASS",checks=motionChecks});
                int visualChecks=await window.VerifyWindowVisuals();result.Add(new{mode="real-window-client-geometry-fullscreen-and-recorded-command-frames",status="PASS",checks=visualChecks});
                window.VerifyPersonalization();result.Add(new{mode="presets-10-vertical-focus-shortcuts-library-workspaces",status="PASS"});
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout-check.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));exit=0;
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout-check.json"),JsonSerializer.Serialize(new{status="FAIL",error=ex.ToString()}));}
            finally{window.Close();}
        };
        Application.Run(window);return exit;
    }
}
