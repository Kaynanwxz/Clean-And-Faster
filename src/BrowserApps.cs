namespace KayBrowser;
public sealed partial class BrowserWindow
{
    AppsPanel? apps;bool appsOpen;
    void ShowApps()
    {
        CloseBrowserMenu(false);
        if(appsOpen){appsOpen=false;AnimateRightPanel(apps!,false);return;}
        if(assistantOpen){assistantOpen=false;aiCancellation?.Cancel();assistant!.Visible=false;}
        if(appearanceOpen){appearanceOpen=false;appearance!.Visible=false;}
        if(apps==null){apps=new(store,a=>_ = OpenWebApp(a),AddWebApp);apps.CloseRequested+=ShowApps;rightDock.Controls.Add(apps);}
        apps.RefreshApps();apps.SetPalette(Palette.Create(store.Settings.Theme=="Claro",Accent));appsOpen=true;AnimateRightPanel(apps,true);
    }
    async Task OpenWebApp(WebAppLink app)
    {
        try
        {
            var existing=WorkspaceTabs.FirstOrDefault(t=>t.AppId==app.Id);
            existing??=WorkspaceTabs.FirstOrDefault(t=>WebApps.SameSite(t.Url,app.Url));
            if(existing!=null){existing.AppId=app.Id;tabStrip.SelectedTab=existing.Page;await Activate(existing);}
            else{await AddTab(app.Url);if(Current is {} t)t.AppId=app.Id;}
        }
        catch(Exception ex)when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException or IOException){ShowNotice("Não foi possível abrir o app. "+ex.Message,"Apps");}
    }
    void AddWebApp()
    {
        if(store.Settings.Apps.Count>=20){ShowNotice("Limite de 20 apps. Remova um antes de adicionar outro.");return;}
        using var editor=new WebAppEditor(Palette.Create(store.Settings.Theme=="Claro",Accent));
        if(editor.ShowDialog(this)==DialogResult.OK&&editor.App is {} app){store.Settings.Apps.Add(app);store.SaveSettings();apps?.RefreshApps();}
    }
}
