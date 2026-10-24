using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task FinishSuspension(BrowserTab tab,WebView2 view,Microsoft.Web.WebView2.Core.CoreWebView2 core,Task<bool> pending)
    {
        bool suspended=await pending;
        if(closing||!tabs.Contains(tab)||tab.View!=view)return;
        if(tab==Current||tab.Pinned||core.IsDocumentPlayingAudio){core.Resume();view.Visible=true;tab.Suspended=false;}
        else tab.Suspended=suspended;
    }
    internal async Task<bool> VerifyStartup()
    {
        await Start();
        if(environment==null||Current is not {} tab)return false;
        await CreateView(tab,false);
        return tab.View?.CoreWebView2!=null;
    }
    internal async Task<int> VerifyStabilityFixes()
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);count++;}
        bool enabled=Motion.Enabled;bool? animations=Motion.DiagnosticAnimations;
        var original=WindowState;var originalBounds=Bounds;
        try
        {
            if(assistantOpen)ToggleAssistant();if(appearanceOpen)ShowAppearance();if(appsOpen)ShowApps();
            Motion.Enabled=false;SettleRightPanel();
            WindowState=FormWindowState.Normal;var restored=Bounds;
            Fullscreen();await Task.Delay(80);Fullscreen();await Task.Delay(80);
            Check(WindowState==FormWindowState.Normal,"Fullscreen restores normal state");
            Check(Math.Abs(Left-restored.Left)<=2&&Math.Abs(Top-restored.Top)<=2&&Math.Abs(Width-restored.Width)<=2&&Math.Abs(Height-restored.Height)<=2,"Fullscreen restores bounds");
            WindowState=FormWindowState.Maximized;Fullscreen();Fullscreen();await Task.Delay(80);
            Check(WindowState==FormWindowState.Maximized,"Fullscreen restores maximized state");
            WindowState=FormWindowState.Normal;Bounds=restored;RefreshMonitorBounds();
            Check(MaximizedBounds==Screen.FromControl(this).WorkingArea,"Monitor work area refreshed");
            Motion.Enabled=true;Motion.DiagnosticAnimations=true;
            ToggleAssistant();
            Check(pageHost.ColumnStyles[1].Width<AssistantWidth,"Sidebar does not jump to full width");
            await Task.Delay(80);
            Check(pageHost.ColumnStyles[1].Width>0&&pageHost.ColumnStyles[1].Width<AssistantWidth,"Sidebar width interpolates");
            ToggleAssistant();ToggleAssistant();ShowAppearance();ToggleAssistant();await Task.Delay(350);
            Check(assistantOpen&&!appearanceOpen&&!appsOpen&&!assistantTween.Running,"Rapid panel switches settle");
            Check(Math.Abs(pageHost.ColumnStyles[1].Width-AssistantWidth)<2,"Panel final width");
            ToggleAssistant();await Task.Delay(50);
            var beforeResize=Size;Size=new Size(Width+(Width>MinimumSize.Width+20?-12:12),Height);await Task.Delay(40);
            Check(Size!=beforeResize,"Resize fixture changes the actual window size");
            Check(!assistantTween.Running&&pageHost.ColumnStyles[1].Width==0,"Resize during closing settles");
            ToggleAssistant();await Task.Delay(40);SettleRightPanel();
            Check(!assistantTween.Running&&assistant!.Left==0,"DPI/resize settlement keeps panel aligned");
            ToggleAssistant();await Task.Delay(250);
            Check(pageHost.ColumnStyles[1].Width==0,"Closing releases sidebar width");
            var tab=Current!;
            await CreateView(tab,false);
            var oldView=tab.View!;
            RenderFailedTab(tab,oldView);
            Check(tab.View==null&&oldView.IsDisposed,"Failed view is disposed");
            Check(tab.Page.Controls.Count==1,"Recovery UI replaces failed content");
            await CreateView(tab,false);
            Check(tab.View?.CoreWebView2!=null,"Recovery recreates real WebView");
            await AddTab("https://clean-ai.test/index.html",false);
            var background=tabs.Last();await CreateView(background,false);
            store.Settings.MemoryBudgetMb=256;store.Settings.IdleMinutes=1;background.LastActive=DateTime.UtcNow.AddMinutes(-5);
            background.View!.Visible=false;
            var core=background.View.CoreWebView2;
            bool suspended=await core.TrySuspendAsync();
            var gate=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var suspension=FinishSuspension(background,background.View,core,gate.Task);
            tabStrip.SelectedTab=background.Page;await Activate(background);gate.SetResult(suspended);await suspension;
            Check(!background.Suspended&&background.View!.Visible,"Selected tab remains resumed after memory management");
            CloseTab(background);
            return count;
        }
        finally
        {
            if(assistantOpen)ToggleAssistant();if(appearanceOpen)ShowAppearance();if(appsOpen)ShowApps();
            SettleRightPanel();Motion.Enabled=enabled;Motion.DiagnosticAnimations=animations;
            WindowState=original;if(original==FormWindowState.Normal)Bounds=originalBounds;
        }
    }
    void QueueExternalLink(string url,bool userInitiated)
    {
        if(closing||!IsHandleCreated)return;
        BeginInvoke(()=>{
            if(closing||IsDisposed)return;
            if(!userInitiated||!Navigation.IsAllowedExternal(url))
            {status.Text="Link externo bloqueado.";return;}
            if(ShowNotice("Abrir este link no aplicativo associado do Windows?\n\n"+url[..Math.Min(url.Length,500)],"Abrir aplicativo externo",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
            catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {ShowNotice("Não foi possível abrir o aplicativo. "+ex.Message);}
        });
    }
    void QueueFailedViews(BrowserTab tab,WebView2 failedView,bool browserExited)
    {
        if(closing||!IsHandleCreated)return;
        BeginInvoke(()=>{
            if(closing||IsDisposed)return;
            foreach(var t in (browserExited?tabs.ToArray():new[]{tab}))
                if(t.View is {} view&&(browserExited||view==failedView))RenderFailedTab(t,view);
        });
    }
    void RenderFailedTab(BrowserTab tab,WebView2 view)
    {
        if(closing||!tabs.Contains(tab)||tab.View!=view)return;
        tab.View=null;tab.Suspended=false;view.Dispose();
        foreach(Control control in tab.Page.Controls.Cast<Control>().ToArray())control.Dispose();
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(Ui(30))};
        body.RowStyles.Add(new RowStyle(SizeType.Percent,50));body.RowStyles.Add(new RowStyle(SizeType.Absolute,Ui(55)));body.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        body.Controls.Add(new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomCenter,Text="O conteúdo desta aba foi interrompido.\nVocê pode reabrir o endereço. Dados não salvos da página podem ter sido perdidos."},0,0);
        var retry=Button("Reabrir aba",()=>{if(!closing&&tabs.Contains(tab)){tabStrip.SelectedTab=tab.Page;_ = CreateView(tab);}});
        retry.Dock=DockStyle.Fill;body.Controls.Add(retry,0,1);ApplyTheme(body);tab.Page.Controls.Add(body);
        status.Text="Aba interrompida — use Reabrir aba para recuperar o conteúdo.";
        UpdateChrome(tab);
    }
}
