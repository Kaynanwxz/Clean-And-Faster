using System.Drawing.Imaging;
using Microsoft.Web.WebView2.Core;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    AppearancePanel? appearance;bool appearanceOpen;
    void ShowAppearance()
    {
        if(appearanceOpen){appearanceOpen=false;appearance!.Visible=false;assistantTween.Start(pageHost.ColumnStyles[1].Width,0,w=>pageHost.ColumnStyles[1].Width=w,220,ApplyWorkspaceMetrics);return;}
        if(assistantOpen){assistantOpen=false;aiCancellation?.Cancel();assistant!.Visible=false;}
        appearance?.Dispose();appearance=new(store,ApplyPersonalization,ChooseWallpaper);appearance.CloseRequested+=ShowAppearance;rightDock.Controls.Add(appearance);appearanceOpen=true;appearance.BringToFront();appearance.Visible=true;
        assistantTween.Start(pageHost.ColumnStyles[1].Width,AssistantWidth,w=>pageHost.ColumnStyles[1].Width=w,220,ApplyWorkspaceMetrics);ApplyWorkspaceMetrics();
    }
    void ApplyPersonalization()
    {
        Motion.Enabled=store.Settings.Animations;ApplyTheme(this);ApplyChromeMetrics();ApplyWorkspaceMetrics();RefreshHomePages();
        foreach(var t in tabs)if(t.View?.CoreWebView2 is {} c)c.Profile.PreferredColorScheme=store.Settings.Theme=="Claro"?CoreWebView2PreferredColorScheme.Light:CoreWebView2PreferredColorScheme.Dark;
    }
    void ToggleFocus()
    {
        store.Settings.FocusMode=!store.Settings.FocusMode;if(store.Settings.FocusMode&&assistantOpen)ToggleAssistant();store.SaveSettings();ApplyPersonalization();
    }
    void ChooseWallpaper()
    {
        using var picker=new OpenFileDialog{Title="Escolher plano de fundo",Filter="Imagens PNG ou JPEG|*.png;*.jpg;*.jpeg",CheckFileExists=true};if(picker.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            if(new FileInfo(picker.FileName).Length>12*1024*1024)throw new IOException("Escolha uma imagem de até 12 MB.");
            using var source=Image.FromFile(picker.FileName);if((long)source.Width*source.Height>24000000)throw new IOException("Escolha uma imagem de até 24 megapixels.");
            int width=Math.Min(1600,source.Width),height=source.Height*width/source.Width;using var reduced=new Bitmap(width,height);using(var g=Graphics.FromImage(reduced)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,width,height);}
            string root=Path.Combine(Storage.Root,"Appearance");Directory.CreateDirectory(root);string path=Path.Combine(root,Guid.NewGuid().ToString("N")+".png");reduced.Save(path,ImageFormat.Png);
            string old=store.Settings.Wallpaper;store.Settings.Wallpaper=path;store.Settings.Background="Personalizado";store.SaveSettings();RefreshHomePages();
            if(old.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))try{File.Delete(old);}catch(IOException){}
        }
        catch(Exception ex)when(ex is IOException or ArgumentException or OutOfMemoryException){ShowNotice("Não foi possível usar a imagem. "+ex.Message,"Plano de fundo");}
    }
    void ShortcutCommand(string action,ShortcutLink? link)
    {
        var list=store.Settings.Shortcuts;
        if(store.Settings.FrequentShortcuts&&link!=null)
        {
            if(action=="edit"){if(list.Count>=20){ShowNotice("Limite de 20 atalhos. Remova um existente antes de salvar outro.");return;}link=new(Guid.NewGuid().ToString("N"),link.Name,link.Url);}
            else{ShowNotice("Para controlar ordem e remoção, escolha Meus atalhos em Personalizar.");return;}
        }
        if(action=="edit")
        {
            if(link==null&&list.Count>=20){ShowNotice("Limite de 20 atalhos. Edite ou remova um existente.");return;}
            using var f=Dialog(link==null?"Adicionar atalho":"Editar atalho",560,310);var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{28,38,28,38,48})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            var name=new TextBox{Text=link?.Name??"",Dock=DockStyle.Fill,MaxLength=60};var url=new TextBox{Text=link?.Url??"https://",Dock=DockStyle.Fill,MaxLength=4096};var save=new SoftButton{Text="Salvar",Dock=DockStyle.Right,Width=130};save.SetPalette(Colors);
            save.Click+=(_,_)=>{try{string address=WebLinks.Normalize(url.Text);if(string.IsNullOrWhiteSpace(name.Text))throw new ArgumentException("Dê um nome ao atalho.");var entry=new ShortcutLink(link?.Id??Guid.NewGuid().ToString("N"),name.Text.Trim(),address);if(link==null)list.Add(entry);else{int index=list.FindIndex(s=>s.Id==link.Id);if(index>=0)list[index]=entry;else list.Add(entry);}store.Settings.FrequentShortcuts=false;store.SaveSettings();f.DialogResult=DialogResult.OK;f.Close();RefreshHomePages();}catch(ArgumentException ex){ShowNotice(ex.Message,"Atalho");}};
            grid.Controls.Add(new Label{Text="Nome",Dock=DockStyle.Fill},0,0);grid.Controls.Add(name,0,1);grid.Controls.Add(new Label{Text="Endereço",Dock=DockStyle.Fill},0,2);grid.Controls.Add(url,0,3);grid.Controls.Add(save,0,4);f.Controls.Add(grid);ApplyTheme(f);f.AcceptButton=save;f.ShowDialog(this);return;
        }
        if(link==null)return;int from=list.FindIndex(x=>x.Id==link.Id);if(from<0)return;
        if(action=="remove")list.RemoveAt(from);
        else{int to=action=="left"?from-1:action=="right"?from+1:action.StartsWith("move:")&&int.TryParse(action[5..],out int n)?n:from;to=Math.Clamp(to,0,list.Count-1);var item=list[from];list.RemoveAt(from);list.Insert(to,item);}
        store.SaveSettings();RefreshHomePages();
    }
}
