namespace KayBrowser;
public sealed class AiCaptureDialog : AppDialog
{
    readonly ComboBox source=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Fonte da captura"};
    readonly AiCropPreview preview=new(){Dock=DockStyle.Fill,Margin=new Padding(0,8,0,8)};
    readonly Label status=new(){Dock=DockStyle.Fill,Text="Escolha uma aba, uma janela ou uma tela. Nada é enviado ao capturar.",AutoEllipsis=false};
    readonly SoftButton capture=new(){Text="Capturar",Glyph="camera",Dock=DockStyle.Fill,AccessibleName="Capturar fonte escolhida"},attach=new(){Text="Usar esta imagem",Dock=DockStyle.Fill,Enabled=false,AccessibleName="Usar imagem da prévia"};
    readonly Func<CancellationToken,Task<Bitmap>> tabCapture;readonly CancellationTokenSource lifetime=new();bool busy,disposed;string label="";
    public AiAttachment? Attachment{get;private set;}
    public AiCaptureDialog(Palette palette,Func<CancellationToken,Task<Bitmap>> captureTab,IReadOnlyList<AiCaptureSource>? diagnosticSources=null):base("Mostrar à IA",palette,760,620)
    {
        tabCapture=captureTab;var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20),Margin=Padding.Empty};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{42,0,54,38,42})grid.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
        var top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130));top.RowStyles.Add(new RowStyle(SizeType.Percent,100));top.Controls.Add(source,0,0);top.Controls.Add(capture,1,0);
        var hint=new Label{Text="Arraste na prévia para recortar. Depois, use a imagem e clique em Enviar na conversa. Capturas são pontuais, não vídeo ao vivo.",Dock=DockStyle.Fill};
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));var cancel=new SoftButton{Text="Cancelar",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel};actions.Controls.Add(cancel,0,0);actions.Controls.Add(attach,1,0);CancelButton=cancel;
        foreach(var item in diagnosticSources??AiCapture.Sources(true))source.Items.Add(item);if(source.Items.Count>0)source.SelectedIndex=0;
        source.SelectedIndexChanged+=(_,_)=>{attach.Enabled=false;status.Text="Clique em Capturar para ver a fonte selecionada.";};
        capture.Click+=async(_,_)=>await CaptureSelected();attach.Click+=(_,_)=>{try{using var image=preview.CopySelection();Attachment=AiCapture.Encode(image,label);DialogResult=DialogResult.OK;Close();}catch(IOException ex){status.Text=ex.Message;}};
        grid.Controls.Add(top,0,0);grid.Controls.Add(preview,0,1);grid.Controls.Add(hint,0,2);grid.Controls.Add(status,0,3);grid.Controls.Add(actions,0,4);Controls.Add(grid);FormClosing+=(_,_)=>{if(!disposed)lifetime.Cancel();};
        void Theme(Control c){c.BackColor=palette.Background;c.ForeColor=palette.Text;if(c is IThemed t)t.SetPalette(palette);foreach(Control child in c.Controls)Theme(child);}Theme(grid);status.ForeColor=palette.Muted;
    }
    internal async Task CaptureSelected()
    {
        if(busy||source.SelectedItem is not AiCaptureSource chosen)return;busy=true;capture.Enabled=false;source.Enabled=false;attach.Enabled=false;status.Text="Preparando a prévia…";Bitmap? bitmap=null;
        try
        {
            if(chosen.Kind=="tab")bitmap=await tabCapture(lifetime.Token);
            else if(chosen.Kind=="window")bitmap=await AiCapture.WindowAsync(chosen.Window,lifetime.Token);
            else if(chosen.Kind=="screen")
            {
                var owner=Owner;var previous=owner?.WindowState;Hide();
                try{if(owner!=null)owner.WindowState=FormWindowState.Minimized;await Task.Delay(240,lifetime.Token);bitmap=AiCapture.ScreenImage(chosen.Bounds);}
                finally{if(owner is {IsDisposed:false}&&previous.HasValue)owner.WindowState=previous.Value;if(!IsDisposed&&!lifetime.IsCancellationRequested)Show();}
            }
            else throw new IOException("Fonte de captura não reconhecida.");
            if(IsDisposed||lifetime.IsCancellationRequested){bitmap?.Dispose();return;}
            label=chosen.Title;preview.SetImage(bitmap);bitmap=null;attach.Enabled=true;status.Text="Prévia pronta. Confira o conteúdo antes de usar; você pode recortar.";
        }
        catch(OperationCanceledException){}
        catch(Exception ex)when(ex is IOException or TimeoutException or System.ComponentModel.Win32Exception or ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException){if(!IsDisposed)status.Text=ex is TimeoutException?"A janela não respondeu. Escolha uma tela ou a aba atual.":ex.Message;}
        finally{bitmap?.Dispose();busy=false;if(!IsDisposed){capture.Enabled=true;source.Enabled=true;}}
    }
    internal AiCropPreview Preview=>preview;
    protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;lifetime.Cancel();lifetime.Dispose();}base.Dispose(disposing);}
}
