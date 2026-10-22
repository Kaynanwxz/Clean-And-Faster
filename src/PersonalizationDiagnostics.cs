namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal void VerifyPersonalization()
    {
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        Motion.Enabled=false;store.Settings.Animations=false;
        string wallpaperFile=Path.Combine(Storage.Root,"diagnostic-wallpaper.png");
        using(var sample=new Bitmap(80,40))sample.Save(wallpaperFile,System.Drawing.Imaging.ImageFormat.Png);
        var wallpaper=BrandAssets.Wallpaper(wallpaperFile);Check(wallpaper!=null&&ReferenceEquals(wallpaper,BrandAssets.Wallpaper(wallpaperFile)),"Plano de fundo duplicado na memória.");BrandAssets.Wallpaper("");

        if(assistantOpen)ToggleAssistant();
        Size=new Size(1320,860);Application.DoEvents();
        foreach(var visual in VisualPresets.All)
        {
            VisualPresets.Apply(store.Settings,visual.Name);ApplyPersonalization();Application.DoEvents();VerifyChromeLayout();
            Check(tabStrip.Header.Vertical==visual.Vertical,"Posição de abas não acompanhou preset "+visual.Name);
            if(visual.Name is "Copiloto Azul" or "Egeu Noturno" or "Ágora Vertical" or "Atelier Personalizável")SaveChromeScreenshot("layout-preset-"+Array.IndexOf(VisualPresets.All,visual)+".png");
        }
        VisualPresets.Apply(store.Settings,"Copiloto Azul");ApplyPersonalization();
        ShowAppearance();Application.DoEvents();VerifyChromeLayout();SaveChromeScreenshot("layout-appearance.png");ShowAppearance();
        store.Settings.VerticalTabs=true;ApplyPersonalization();ToggleAssistant();Application.DoEvents();VerifyChromeLayout();SaveChromeScreenshot("layout-vertical-ai.png");ToggleAssistant();
        ToggleFocus();Application.DoEvents();VerifyChromeLayout();Check(chromeLayout.GetRowHeights()[3]==0,"Modo foco não recolheu status.");ToggleFocus();
        var first=Current!;var other=new BrowserTab{Url="kay://new",Title="Outro espaço"};other.Page.WorkspaceId=store.Settings.Workspaces[1].Id;tabs.Add(other);tabStrip.TabPages.Add(other.Page);
        string initial=WorkspaceId;using var workspaceCancel=new CancellationTokenSource();aiCancellation=workspaceCancel;var switchTask=SwitchWorkspace(other.Page.WorkspaceId);while(!switchTask.IsCompleted)Application.DoEvents();switchTask.GetAwaiter().GetResult();
        Check(workspaceCancel.IsCancellationRequested,"Troca de espaço não cancelou tarefa em curso.");aiCancellation=null;
        using(var context=System.Text.Json.JsonDocument.Parse(TabsContext()))Check(context.RootElement.GetArrayLength()==1,"Contexto de IA incluiu outro espaço.");
        Check(Current==other&&tabStrip.VisiblePages.Length==1,"Espaço exibiu aba de outro contexto.");
        var back=SwitchWorkspace(initial);while(!back.IsCompleted)Application.DoEvents();back.GetAwaiter().GetResult();Check(Current==first,"Troca de espaço perdeu aba viva.");
        tabs.Remove(other);tabStrip.TabPages.Remove(other.Page);other.Page.Dispose();
        int count=store.Settings.Shortcuts.Count;var link=store.Settings.Shortcuts[0];ShortcutCommand("right",link);Check(store.Settings.Shortcuts[1].Id==link.Id,"Reordenação de atalho falhou.");ShortcutCommand("left",link);Check(store.Settings.Shortcuts[0].Id==link.Id,"Reordenação inversa falhou.");Check(store.Settings.Shortcuts.Count==count,"Reordenação perdeu atalhos.");
        store.Library.Add(new("diag-note","Nota de teste","","note","Teste de persistência",DateTime.UtcNow));store.SaveLibrary();Check(new Storage().Library.Any(e=>e.Id=="diag-note"),"Nota não persistiu.");
        store.Library.RemoveAll(e=>e.Id=="diag-note");store.SaveLibrary();
        VisualPresets.Apply(store.Settings,"Copiloto Azul");ApplyPersonalization();ToggleAssistant();Application.DoEvents();SaveChromeScreenshot("layout-final.png");
    }
}
