using Microsoft.Web.WebView2.Core;
using System.Text.Json;
namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task<string[]> VerifyNavigationButtons()
    {
        var checks=new List<string>();void Check(bool ok,string text){File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),(ok?"PASS ":"FAIL ")+text+Environment.NewLine);if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        Button Toolbar(string name)=>AppearancePanel.Descendants(navigationRow!).OfType<Button>().Single(b=>b.AccessibleName?.StartsWith(name)==true);
        var tab=Current!;await CreateView(tab,false);var core=tab.View!.CoreWebView2;
        string fixture=Path.Combine(Storage.Root,"agent-fixture");File.WriteAllText(Path.Combine(fixture,"next.html"),"<!doctype html><html><head><meta charset=\"utf-8\"><title>Segunda página</title></head><body>Segunda página local</body></html>");core.SetVirtualHostNameToFolderMapping("clean-ai.test",fixture,CoreWebView2HostResourceAccessKind.DenyCors);
        async Task Navigate(Action trigger)
        {
            var ready=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);void Loaded(object? s,CoreWebView2NavigationCompletedEventArgs e){if(e.IsSuccess)ready.TrySetResult(true);else ready.TrySetException(new IOException("Fixture: "+e.WebErrorStatus));}
            core.NavigationCompleted+=Loaded;try{trigger();await ready.Task.WaitAsync(TimeSpan.FromSeconds(12));}finally{core.NavigationCompleted-=Loaded;}
        }
        await Navigate(()=>core.Navigate("https://clean-ai.test/index.html"));await Navigate(()=>core.Navigate("https://clean-ai.test/next.html"));
        await Navigate(()=>Toolbar("Voltar").PerformClick());Check(core.Source.EndsWith("index.html"),"Botão voltar navega na fixture");await Navigate(()=>Toolbar("Avançar").PerformClick());Check(core.Source.EndsWith("next.html"),"Botão avançar navega na fixture");await Navigate(()=>Toolbar("Recarregar").PerformClick());Check(core.DocumentTitle=="Segunda página","Botão recarregar conclui navegação");
        await core.ExecuteScriptAsync("window.cleanDismissalClicks=0;document.addEventListener('click',()=>window.cleanDismissalClicks++)");
        for(int i=0;i<4;i++)
        {
            ShowMenu();await Task.Delay(80);var point=tab.View.PointToScreen(new Point(30,100));Cursor.Position=point;mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);await Task.Delay(120);Check(!menuOpen&&!closing,"Clique no WebView fecha menu sem encerrar app "+i);
            Check(await core.ExecuteScriptAsync("window.cleanDismissalClicks")=="0","Clique de dispensa não chega à página WebView "+i);
        }
        ShowMenu();browserMenu!.ClickCommand("zoom-in");Check(tab.View.ZoomFactor>1,"Botão zoom mais altera WebView");browserMenu.ClickCommand("zoom-out");Check(Math.Abs(tab.View.ZoomFactor-1)<.01,"Botão zoom menos restaura WebView");CloseBrowserMenu(false);
        int bookmarks=store.Bookmarks.Count;Toolbar("Salvar ou remover favorito").PerformClick();Check(store.Bookmarks.Count==bookmarks+1,"Estrela da barra salva favorito");Toolbar("Salvar ou remover favorito").PerformClick();Check(store.Bookmarks.Count==bookmarks,"Estrela da barra remove favorito");
        // Install an isolated extension, then operate the real toggle/remove UI against it.
        string extensionFolder=Path.Combine(Storage.Root,"diagnostic-extension");Directory.CreateDirectory(extensionFolder);File.WriteAllText(Path.Combine(extensionFolder,"manifest.json"),"""{"manifest_version":3,"name":"Clean Diagnostic","version":"1.0","description":"Isolated regression fixture","content_scripts":[{"matches":["https://clean-ai.test/*"],"js":["probe.js"]}]}""");File.WriteAllText(Path.Combine(extensionFolder,"probe.js"),"document.documentElement.dataset.cleanExtension='active';");
        var extension=await core.Profile.AddBrowserExtensionAsync(extensionFolder).WaitAsync(TimeSpan.FromSeconds(25));Check((await core.Profile.GetBrowserExtensionsAsync()).Any(e=>e.Id==extension.Id),"Instalar extensão local pela API real");
        int stage=0;Exception? failure=null;var deadline=DateTime.UtcNow.AddSeconds(15);using var driver=new System.Windows.Forms.Timer{Interval=40};
        driver.Tick+=(_,_)=>{
            try
            {
                if(DateTime.UtcNow>deadline)throw new TimeoutException("Extensões não responderam.");
                var dialog=Application.OpenForms.Cast<Form>().OfType<AppDialog>().LastOrDefault(f=>f.Text=="Extensões · por pasta");if(dialog==null)return;
                var list=AppearancePanel.Descendants(dialog).OfType<ListView>().Single();var item=list.Items.Cast<ListViewItem>().FirstOrDefault(i=>i.Text=="Clean Diagnostic");
                Button Action(string text)=>AppearancePanel.Descendants(dialog).OfType<Button>().Single(b=>b.Text==text);
                if(stage==0&&item!=null){list.Focus();item.Selected=true;stage=1;Action("Ativar / desativar").PerformClick();}
                else if(stage==1&&item?.SubItems[2].Text=="Desativada"){Check(true,"Desativar extensão pelo botão");item.Selected=true;stage=2;Action("Ativar / desativar").PerformClick();}
                else if(stage==2&&item?.SubItems[2].Text=="Ativa"){Check(true,"Reativar extensão pelo botão");item.Selected=true;stage=3;Action("Remover").PerformClick();}
                else if(stage==3&&item==null){Check(true,"Remover extensão pelo botão");stage=4;dialog.Close();}
            }
            catch(Exception e){failure=e;driver.Stop();foreach(var f in Application.OpenForms.Cast<Form>().OfType<AppDialog>().ToArray())f.Close();}
        };
        driver.Start();try{await ShowExtensions();}finally{driver.Stop();}
        if(failure!=null)throw failure;Check(stage==4&&!(await core.Profile.GetBrowserExtensionsAsync()).Any(e=>e.Id==extension.Id),"Extensão de teste removida do perfil");
        int canceled=0,downloadCount=downloads.Count;var downloadEvent=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Downloaded(object? sender,CoreWebView2DownloadStartingEventArgs args)=>downloadEvent.TrySetResult(true);
        core.DownloadStarting+=Downloaded;using(var cancelPicker=new System.Windows.Forms.Timer{Interval=30})
        {
            cancelPicker.Tick+=(_,_)=>{var h=GetForegroundWindow();var name=new System.Text.StringBuilder(64);GetClassName(h,name,64);if(name.ToString()=="#32770"){canceled++;SendMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);}foreach(var f in Application.OpenForms.Cast<Form>().OfType<AppDialog>().ToArray())f.Close();};cancelPicker.Start();
            try{await core.ExecuteScriptAsync("(()=>{const a=document.createElement('a');a.href='data:text/plain,diagnostic';a.download='diagnostic.txt';document.body.append(a);a.click();})()");await downloadEvent.Task.WaitAsync(TimeSpan.FromSeconds(12));await Task.Delay(300);}
            finally{cancelPicker.Stop();core.DownloadStarting-=Downloaded;}
        }
        Check(canceled>0&&downloads.Count==downloadCount,"Cancelar download real não trava nem salva arquivo");
        Toolbar("Página inicial").PerformClick();Check(Current!.View==null&&Current.Page.Controls.OfType<HomeBoard>().Any(),"Botão início restaura página inicial");
        return checks.ToArray();
    }
}
