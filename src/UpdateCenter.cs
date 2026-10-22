using System.Text.Json;

namespace KayBrowser;

public sealed class UpdateCenter : AppDialog
{
    readonly Storage store;
    readonly Palette palette;
    Palette Colors() => palette;
    readonly Label description;
    readonly Label current;
    readonly ProgressBar progress;
    readonly SoftButton action;
    readonly SoftButton cancel;
    readonly SoftButton source;
    CancellationTokenSource? cancellation;
    ReleaseUpdate? available;
    bool busy;

    public UpdateCenter(Storage storage, Palette colors) : base("Atualizações do Kay", colors, 620, 405)
    {
        palette=colors;store=storage;Text="Atualizações do Kay";ClientSize=new Size(620,405);MinimumSize=new Size(570,390);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=colors.Background;ForeColor=colors.Text;MinimizeBox=false;MaximizeBox=false;
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(26)};
        foreach(int height in new[]{40,34,74,26,48,48})body.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        body.Controls.Add(new Label{Text="Mantenha seu Kay atualizado",Dock=DockStyle.Fill,Font=new Font("Segoe UI",17,FontStyle.Bold)},0,0);
        current=new Label{Text="Versão instalada: "+Updater.CurrentVersion.ToString(3),Dock=DockStyle.Fill,ForeColor=colors.Muted};body.Controls.Add(current,0,1);
        description=new Label{Dock=DockStyle.Fill,ForeColor=colors.Muted,Text="Verifique se há uma versão nova. O navegador baixa e substitui o executável, preservando seus dados."};body.Controls.Add(description,0,2);
        progress=new ProgressBar{Dock=DockStyle.Fill,Minimum=0,Maximum=100,Margin=new Padding(0,5,0,5)};body.Controls.Add(progress,0,3);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,Padding=Padding.Empty};
        action=new SoftButton{Text="Verificar agora",Width=215,Height=40};action.SetPalette(colors);action.Click+=async(_,_)=>{if(available==null)await Check();else await DownloadAndInstall();};
        cancel=new SoftButton{Text="Fechar",Width=105,Height=40};cancel.SetPalette(colors);cancel.Click+=(_,_)=>{if(busy)cancellation?.Cancel();else Close();};
        buttons.Controls.AddRange([action,cancel]);body.Controls.Add(buttons,0,4);
        source=new SoftButton{Text="Canal de atualização",Dock=DockStyle.Left,Width=240,Height=36};source.SetPalette(colors);source.Click+=(_,_)=>ConfigureSource();body.Controls.Add(source,0,5);
        Controls.Add(body);FormClosing+=(_,e)=>{if(busy){cancellation?.Cancel();e.Cancel=true;}};
        Shown+=async(_,_)=>await Check();
    }
    string Feed()
    {
        if(UpdateManifest.ValidFeed(store.Settings.UpdateFeedUrl))return store.Settings.UpdateFeedUrl;
        if(UpdateManifest.ValidFeed(Distribution.DefaultFeedUrl))return Distribution.DefaultFeedUrl;
        return "";
    }
    void SetBusy(bool value,string message)
    {busy=value;description.Text=message;action.Enabled=!value;source.Enabled=!value;cancel.Text=value?"Cancelar":"Fechar";}
    async Task Check()
    {
        if(busy)return;
        available=null;action.Text="Verificar agora";progress.Value=0;
        string feed=Feed();
        if(feed.Length==0&&!Updater.ValidRepository(Distribution.Repository(store.Settings.UpdateRepository)))
        {
            description.Text="O canal de atualização não está configurado. Nenhuma atualização foi instalada.";return;
        }
        using var cts=new CancellationTokenSource(TimeSpan.FromSeconds(45));cancellation=cts;SetBusy(true,"Verificando a versão disponível…");progress.Style=ProgressBarStyle.Marquee;
        try
        {
            available=feed.Length>0?await Updater.CheckFeed(feed,cts.Token):await Updater.Check(Distribution.Repository(store.Settings.UpdateRepository), cts.Token);
            description.Text=available==null?"Você já está na versão mais recente disponível.":$"Versão {available.Version} disponível. Clique abaixo para baixar e reiniciar. Seus favoritos, histórico e configurações serão preservados.";
            action.Text=available==null?"Verificar novamente":"Atualizar e reiniciar";
        }
        catch(OperationCanceledException){description.Text="Verificação cancelada ou servidor sem resposta.";}
        catch(Exception ex){description.Text="Não foi possível verificar: "+ex.Message;}
        finally{busy=false;progress.Style=ProgressBarStyle.Continuous;action.Enabled=true;source.Enabled=true;cancel.Text="Fechar";cancellation=null;}
    }
    async Task DownloadAndInstall()
    {
        if(busy||available==null)return;
        if(UiMessage.Show(this, Colors(),"O navegador será reiniciado após o download. Salve formulários e trabalhos em andamento antes de continuar.","Atualizar o Kay",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
        using var cts=new CancellationTokenSource(TimeSpan.FromMinutes(10));cancellation=cts;SetBusy(true,"Baixando atualização…");progress.Value=0;
        var report=new Progress<int>(value=>{if(!IsDisposed){progress.Value=Math.Clamp(value,0,100);description.Text=value<99?$"Baixando atualização… {value}%":"Verificando o arquivo e preparando o reinício…";}});
        try
        {
            await Updater.Install(available,report,cts.Token);
            busy=false;DialogResult=DialogResult.OK;Close();
        }
        catch(OperationCanceledException){description.Text="Download cancelado. Sua versão atual continua instalada.";}
        catch(Exception ex){description.Text="A atualização não foi instalada: "+ex.Message;}
        finally{busy=false;if(!IsDisposed){action.Enabled=true;source.Enabled=true;cancel.Text="Fechar";}cancellation=null;}
    }
    void ConfigureSource()
    {
        using var f = new AppDialog("Canal de atualização", Colors(), 620, 250);
        var note = new Label { Text = "Canal oficial: Kaynanwxz/Clean-And-Faster (GitHub Releases). Você não precisa configurar uma hospedagem nem instalar ZIPs para atualizar.", Dock = DockStyle.Top, Height = 75, Padding = new Padding(12), ForeColor = ForeColor };
        var useOfficial = new SoftButton { Text = "Usar canal oficial", Dock = DockStyle.Bottom, Height = 42 };
        useOfficial.SetPalette(Colors());
        useOfficial.Click += (_, _) => { store.Settings.UpdateRepository = Distribution.DefaultRepository; store.Settings.UpdateFeedUrl = ""; store.SaveSettings(); available = null; action.Text = "Verificar agora"; description.Text = "Canal oficial selecionado. Clique em Verificar agora."; f.Close(); };
        f.Controls.Add(note); f.Controls.Add(useOfficial); f.ShowDialog(this);
    }
}
