using Microsoft.Web.WebView2.Core;
using System.Text.Json;
namespace KayBrowser;
public sealed partial class BrowserWindow
{
    internal async Task<int> VerifyWebApps()
    {
        int checks=0;void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks++;}
        var initial=store.Settings.Apps.ToList();var first=Current!;BrowserTab? opened=null;
        try
        {
            if(assistantOpen)ToggleAssistant();if(appearanceOpen)ShowAppearance();
            foreach(var size in new[]{new Size(900,600),new Size(1320,860),new Size(1920,1080)})
            {
                Size=size;int count=tabs.Count;ShowApps();await Task.Delay(35);VerifyChromeLayout();Check(appsOpen&&apps!.Visible,"Painel apps abre");Check(tabs.Count==count,"Catálogo não carrega sites em segundo plano");
                SaveChromeScreenshot("layout-apps-"+size.Width+".png");ShowApps();Check(!appsOpen,"Fechar apps recolhe painel");
            }
            Size=new Size(1320,860);ShowApps();ShowAppearance();Check(!appsOpen&&appearanceOpen,"Aparência troca painel apps");ShowApps();Check(appsOpen&&!appearanceOpen,"Apps troca aparência");ToggleAssistant();Check(assistantOpen&&!appsOpen,"IA troca apps");ShowApps();Check(appsOpen&&!assistantOpen,"Apps troca IA");
            using(var other=new Form{Text="Apps focus fixture",Size=new Size(160,90),ShowInTaskbar=false}){other.Show(this);other.Activate();await Task.Delay(30);Check(appsOpen,"Perda de foco não fecha apps");other.Close();}Activate();
            using(var editor=new WebAppEditor(Colors))
            {
                editor.Show(this);var fields=AppearancePanel.Descendants(editor).OfType<TextBox>().ToArray();fields.Single(t=>t.AccessibleName=="Nome do app").Text="Teste local";fields.Single(t=>t.AccessibleName=="Endereço do app").Text="javascript:bad";
                editor.AcceptButton!.PerformClick();Check(editor.App==null&&editor.Visible,"Editor rejeita protocolo inválido sem fechar");fields.Single(t=>t.AccessibleName=="Endereço do app").Text="https://clean-ai.test/index.html";editor.AcceptButton.PerformClick();Check(editor.App?.Url=="https://clean-ai.test/index.html","Editor salva endereço válido");
            }
            var app=WebApps.Create("Teste local","https://clean-ai.test/index.html");store.Settings.Apps.Add(app);store.SaveSettings();apps!.RefreshApps();Check(new Storage().Settings.Apps.Any(a=>a.Id==app.Id),"App persiste no perfil");
            int before=tabs.Count;await OpenWebApp(app);opened=Current!;Check(tabs.Count==before+1&&opened.AppId==app.Id&&opened.View?.CoreWebView2!=null,"App abre aba WebView real");
            var core=opened.View!.CoreWebView2;core.SetVirtualHostNameToFolderMapping("clean-ai.test",Path.Combine(Storage.Root,"agent-fixture"),CoreWebView2HostResourceAccessKind.DenyCors);
            var loaded=new TaskCompletionSource<bool>();void Ready(object? s,CoreWebView2NavigationCompletedEventArgs e){if(e.IsSuccess)loaded.TrySetResult(true);}
            core.NavigationCompleted+=Ready;try{core.Navigate(app.Url);await loaded.Task.WaitAsync(TimeSpan.FromSeconds(12));}finally{core.NavigationCompleted-=Ready;}
            Check((await core.ExecuteScriptAsync("document.querySelector('h1').textContent")).Contains("Teste Clean AI"),"App mostra conteúdo local real");
            async Task<bool> HasCookie(CoreWebView2 browser)
            {
                for(int attempt=0;attempt<40;attempt++){if((await browser.CookieManager.GetCookiesAsync(app.Url)).Any(c=>c.Name=="clean_app_probe"&&c.Value=="saved"))return true;await Task.Delay(50);}return false;
            }
            var cookie=core.CookieManager.CreateCookie("clean_app_probe","saved","clean-ai.test","/");cookie.IsSecure=true;cookie.IsHttpOnly=true;cookie.Expires=DateTime.UtcNow.AddDays(1);core.CookieManager.AddOrUpdateCookie(cookie);Check(await HasCookie(core),"Cookie de teste confirmado pelo processo do navegador antes de fechar a aba");
            tabStrip.SelectedTab=first.Page;await Activate(first);await OpenWebApp(app);Check(Current==opened&&tabs.Count==before+1,"Reabrir reutiliza aba sem duplicar");
            ShowApps();ShowApps();Check(ReferenceEquals(Current!.View,opened.View)&&!opened.View.IsDisposed,"Fechar painel preserva app e reprodução");
            CloseTab(opened);opened=null;await OpenWebApp(app);opened=Current!;Check(await HasCookie(opened.View!.CoreWebView2),"Sessão persistente é reutilizada em outro WebView");
            var tile=AppearancePanel.Descendants(apps).OfType<Button>().Single(b=>b.AccessibleName=="Abrir app Teste local");tile.ContextMenuStrip!.Items[0].PerformClick();Check(!store.Settings.Apps.Any(a=>a.Id==app.Id)&&!new Storage().Settings.Apps.Any(a=>a.Id==app.Id),"Remover app persiste");Check(!opened.View.IsDisposed,"Remover catálogo não encerra sessão");
            foreach(bool light in new[]{true,false}){var p=Palette.Create(light,Accent);apps.SetPalette(p);Check(apps.BackColor==p.Background,"Apps acompanha paleta");}
            var dots=PointillistArt.Render(new Size(3840,2160));Check(dots.Size==new Size(3840,2160),"Pontos renderizados em resolução 4K");Check(ReferenceEquals(dots,PointillistArt.Render(dots.Size)),"Cache compartilhado evita imagens por aba");
            int ink=0,gaps=0;for(int y=2000;y<2100;y++)for(int x=1000;x<1100;x++){if(dots.GetPixel(x,y).A>10)ink++;else gaps++;}Check(ink>100&&gaps>100,"Arte contém pontos separados por transparência");
            return checks;
        }
        finally
        {
            if(opened!=null&&tabs.Contains(opened))CloseTab(opened);if(appsOpen)ShowApps();if(assistantOpen)ToggleAssistant();if(appearanceOpen)ShowAppearance();store.Settings.Apps=initial;store.SaveSettings();apps?.RefreshApps();tabStrip.SelectedTab=first.Page;await Activate(first);
        }
    }
    internal async Task ProbePublicApps()
    {
        environment=await CoreWebView2Environment.CreateAsync(null,Path.Combine(Storage.Root,"WebView"),new CoreWebView2EnvironmentOptions{AreBrowserExtensionsEnabled=true});
        var results=new List<object>();
        foreach(var app in WebApps.Defaults().Where(a=>a.Id is "spotify" or "youtube" or "notion"))
        {
            await OpenWebApp(app);var tab=Current!;var core=tab.View?.CoreWebView2;
            if(core==null){results.Add(new{app=app.Name,status="engine-error"});continue;}
            for(int i=0;i<60;i++){await Task.Delay(250);if(core.Source!="about:blank"&&await core.ExecuteScriptAsync("document.readyState")=="\"complete\"")break;}
            string page=await core.ExecuteScriptAsync("JSON.stringify({title:document.title,url:location.href,text:document.body?.innerText?.slice(0,1200)})");
            await core.ExecuteScriptAsync("window.__cleanDrmProbe='checking';navigator.requestMediaKeySystemAccess('com.widevine.alpha',[{initDataTypes:['cenc'],audioCapabilities:[{contentType:'audio/mp4; codecs=\"mp4a.40.2\"'}]}]).then(()=>window.__cleanDrmProbe='supported').catch(e=>window.__cleanDrmProbe=e.name)");
            string drm="checking";for(int i=0;i<20;i++){drm=await core.ExecuteScriptAsync("window.__cleanDrmProbe");if(drm!="\"checking\"")break;await Task.Delay(100);}
            results.Add(new{app=app.Name,page=JsonSerializer.Deserialize<string>(page),widevine=drm});CloseTab(tab);
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"app-compatibility.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
    }
}
public static class WebAppsCompatibility
{
    public static int Run()
    {
        int exit=1;using var window=new BrowserWindow(diagnostics:true);window.Shown+=async(_,_)=>{try{window.PrepareDiagnosticPage();await window.ProbePublicApps();exit=0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"app-compatibility.json"),ex.ToString());}finally{window.Close();}};Application.Run(window);return exit;
    }
}
