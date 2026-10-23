using System.ComponentModel;
namespace KayBrowser;
public sealed class AssistantPanel : Panel,IThemed
{
    readonly ChatTranscript transcript=new(){Dock=DockStyle.Fill};
    readonly TextBox prompt=new(){Dock=DockStyle.Fill,Multiline=true,MaxLength=4000,PlaceholderText="Converse com sua IA…",BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",10)};
    readonly Label state=new(){Dock=DockStyle.Fill,Text="Ctrl+Enter para enviar",AutoEllipsis=true,Font=new Font("Segoe UI",8)};
    readonly SoftButton send=new(){Text="Enviar",Glyph="forward",Dock=DockStyle.Fill},stop=new(){Text="Parar",Dock=DockStyle.Fill,Enabled=false};
    readonly SoftButton setup=new(){Text="Conectar IA",Glyph="settings",Dock=DockStyle.Fill},clear=new(){Text="Nova conversa",Dock=DockStyle.Fill};
    readonly SoftButton context=new(){Text="Contexto",Glyph="globe",Dock=DockStyle.Fill,AccessibleName="Contexto e ações da IA"};
    readonly SoftButton capture=new(){Text="Mostrar tela ou janela",Glyph="camera",Dock=DockStyle.Fill,AccessibleName="Mostrar tela ou janela à IA"};
    readonly CheckBox share=new(){Text="Usar página e abas neste pedido",Dock=DockStyle.Fill,AccessibleName="Compartilhar página e abas"};
    readonly CheckBox actions=new(){Text="Ações no navegador, com aprovação",Dock=DockStyle.Fill,AccessibleName="Permitir ações da IA no navegador"};
    readonly TableLayoutPanel attachmentBox=new(){Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};
    readonly PictureBox thumbnail=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};readonly Label attachmentLabel=new(){Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};readonly SoftButton remove=new(){Glyph="close",Dock=DockStyle.Fill,AccessibleName="Remover imagem antes de enviar"};
    readonly TableLayoutPanel grid=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=11,Margin=Padding.Empty};
    AiAttachment? attachment;Palette colors=Palette.Create(true,Color.SteelBlue);bool busy;
    public bool Share=>share.Checked;public bool BrowserActions=>actions.Checked&&share.Checked;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public string Prompt{get=>prompt.Text;set=>prompt.Text=value;}
    public AiAttachment? Attachment=>attachment;
    public event Action? SendRequested,StopRequested,SetupRequested,CloseRequested,ExpandRequested,CaptureRequested,ResetRequested;
    public AssistantPanel()
    {
        Dock=DockStyle.Fill;Padding=new Padding(16,12,16,10);AccessibleName="Clean AI · assistente integrado";DoubleBuffered=true;grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(int h in new[]{48,0,30,0,0,34,0,26,64,36,34})grid.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));foreach(int row in new[]{3,4,6}){grid.RowStyles[row].SizeType=SizeType.Absolute;grid.RowStyles[row].Height=0;}
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var logo=new PictureBox{Image=BrandAssets.Logo,SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Top,Height=34};var title=new Label{Text="Clean AI",Font=new Font("Georgia",20),Dock=DockStyle.Fill,AutoEllipsis=true};
        var expand=new SoftButton{Glyph="max",AccessibleName="Expandir ou reduzir IA",Dock=DockStyle.Top,Height=28};var close=new SoftButton{Glyph="close",AccessibleName="Fechar IA",Dock=DockStyle.Top,Height=28};expand.Click+=(_,_)=>ExpandRequested?.Invoke();close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(logo,0,0);header.Controls.Add(title,1,0);header.Controls.Add(expand,2,0);header.Controls.Add(close,3,0);
        var copy=new SoftButton{Text="Copiar",Glyph="code",Dock=DockStyle.Fill};copy.Click+=(_,_)=>{if(transcript.LastAnswer.Length>0)try{Clipboard.SetText(transcript.LastAnswer);state.Text="Resposta copiada";}catch(System.Runtime.InteropServices.ExternalException){state.Text="Área de transferência ocupada";}};
        var promptBox=new RoundedPanel{Dock=DockStyle.Fill,Radius=18,Padding=new Padding(12,10,12,8)};promptBox.Controls.Add(prompt);
        var commands=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};commands.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));commands.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));commands.RowStyles.Add(new RowStyle(SizeType.Percent,100));commands.Controls.Add(send,0,0);commands.Controls.Add(stop,1,0);
        var settings=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,53));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,47));settings.RowStyles.Add(new RowStyle(SizeType.Percent,100));settings.Controls.Add(setup,0,0);settings.Controls.Add(clear,1,0);
        attachmentBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,66));attachmentBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));attachmentBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));attachmentBox.RowStyles.Add(new RowStyle(SizeType.Percent,100));attachmentBox.Controls.Add(thumbnail,0,0);attachmentBox.Controls.Add(attachmentLabel,1,0);attachmentBox.Controls.Add(remove,2,0);
        grid.Controls.Add(header,0,0);grid.Controls.Add(transcript,0,1);context.Click+=(_,_)=>{ConfigureContext();context.Text=Share?"Contexto ativo":"Contexto";};
        var contextBar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};contextBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));contextBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));contextBar.RowStyles.Add(new RowStyle(SizeType.Percent,100));contextBar.Controls.Add(copy,0,0);contextBar.Controls.Add(context,1,0);grid.Controls.Add(contextBar,0,2);grid.Controls.Add(capture,0,5);grid.Controls.Add(attachmentBox,0,6);grid.Controls.Add(state,0,7);grid.Controls.Add(promptBox,0,8);grid.Controls.Add(commands,0,9);grid.Controls.Add(settings,0,10);Controls.Add(grid);
        setup.Click+=(_,_)=>SetupRequested?.Invoke();capture.Click+=(_,_)=>CaptureRequested?.Invoke();remove.Click+=(_,_)=>SetAttachment(null);stop.Click+=(_,_)=>StopRequested?.Invoke();send.Click+=(_,_)=>SendRequested?.Invoke();clear.Click+=(_,_)=>Reset();prompt.KeyDown+=(_,e)=>{if(e.Control&&e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(send.Enabled)SendRequested?.Invoke();}};
        actions.CheckedChanged+=(_,_)=>{if(actions.Checked)state.Text=share.Checked?"Cada ação será confirmada por você.":"Ative Usar página e abas para permitir ações.";};Reset();SetPalette(colors);
    }
    void ConfigureContext()
    {
        using var dialog=new AppDialog("Contexto da IA",colors,570,310);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(20)};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{64,38,38,42})body.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        var note=new Label{Text="A imagem escolhida é independente destas opções. Ative contexto para resumir páginas e ações para a IA propor navegação, sempre com sua confirmação.",Dock=DockStyle.Fill};
        var read=new CheckBox{Text="Usar texto da página e lista de abas",Checked=share.Checked,Dock=DockStyle.Fill};var operate=new CheckBox{Text="Permitir ações aprovadas no navegador",Checked=actions.Checked,Dock=DockStyle.Fill};
        var apply=new SoftButton{Text="Aplicar",Dock=DockStyle.Right,Width=140};apply.SetPalette(colors);apply.Click+=(_,_)=>{share.Checked=read.Checked||operate.Checked;actions.Checked=operate.Checked;dialog.DialogResult=DialogResult.OK;dialog.Close();};body.Controls.Add(note,0,0);body.Controls.Add(read,0,1);body.Controls.Add(operate,0,2);body.Controls.Add(apply,0,3);dialog.Controls.Add(body);foreach(Control c in body.Controls){c.BackColor=colors.Background;c.ForeColor=colors.Text;}dialog.ShowDialog(FindForm());
    }
    public void Reset(){if(busy)return;transcript.ClearMessages();Prompt="";SetAttachment(null);ResetRequested?.Invoke();transcript.Add("Clean AI","Converse aqui, sem abrir outro aplicativo.\n\nConecte sua IA, escreva uma pergunta ou mostre uma aba, janela ou tela. A conversa mantém o contexto nesta sessão.\n\nSó envio uma captura quando você clicar em Enviar.");}
    public void Add(string who,string text)=>transcript.Add(who,text);
    public void SetAttachment(AiAttachment? image)
    {
        image?.ValidDataUrl();attachment=image;thumbnail.Image?.Dispose();thumbnail.Image=null;attachmentLabel.Text=image==null?"":image.Label+" · pronta para enviar";remove.Enabled=image!=null;
        if(image!=null){using var bytes=new MemoryStream(Convert.FromBase64String(image.DataUrl[23..]));using var original=Image.FromStream(bytes);thumbnail.Image=new Bitmap(original,new Size(96,60));}
        grid.RowStyles[6].Height=image==null?0:(int)Math.Round(60*DeviceDpi/96f);attachmentBox.Visible=image!=null;state.Text=image==null?"Ctrl+Enter para enviar":"Confira a imagem; Enviar compartilha com sua IA.";
    }
    public void AttachmentSent(){if(attachment!=null)attachmentLabel.Text=attachment.Label+" · mantida nesta conversa; remova quando quiser";}
    public void ConnectionLabel(string label)=>setup.Text=label;
    public void Busy(bool value){busy=value;send.Enabled=!value;setup.Enabled=!value;clear.Enabled=!value;capture.Enabled=!value;context.Enabled=!value;prompt.Enabled=!value;share.Enabled=!value;actions.Enabled=!value;remove.Enabled=!value&&attachment!=null;stop.Enabled=value;state.Text=value?"Conversando com sua IA… você pode parar":"Ctrl+Enter para enviar";}
    public void SetPalette(Palette p)
    {
        colors=p;void Theme(Control c){c.BackColor=p.Surface;c.ForeColor=p.Text;if(c!=this&&c is IThemed themed)themed.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}Theme(this);state.ForeColor=p.Muted;transcript.SetPalette(p);prompt.BackColor=p.Surface;Invalidate();
    }
    protected override void Dispose(bool disposing){if(disposing){thumbnail.Image?.Dispose();share.Dispose();actions.Dispose();}base.Dispose(disposing);}
}
public sealed class ChatTranscript : RichTextBox,IThemed
{
    readonly List<(string Who,string Text)> messages=[];Palette colors=Palette.Create(true,Color.SteelBlue);
    public string LastAnswer=>messages.LastOrDefault(m=>m.Who=="Clean AI").Text??"";
    public ChatTranscript(){ReadOnly=true;BorderStyle=BorderStyle.None;DetectUrls=false;ScrollBars=RichTextBoxScrollBars.Vertical;Font=new Font("Segoe UI",10);AccessibleName="Conversa de IA; texto selecionável";}
    public void Add(string who,string text){if(messages.Count>=24)messages.RemoveAt(0);messages.Add((who,text[..Math.Min(text.Length,20000)]));while(messages.Count>1&&messages.Sum(m=>m.Text.Length)>80000)messages.RemoveAt(0);Render();}
    public void ClearMessages(){messages.Clear();Clear();}
    void Render(){Clear();foreach(var m in messages){SelectionStart=TextLength;SelectionColor=colors.Muted;using(var heading=new Font(Font,FontStyle.Bold)){SelectionFont=heading;AppendText(m.Who+"\n");}SelectionFont=Font;SelectionColor=colors.Text;AppendText(m.Text+"\n\n");}SelectionStart=TextLength;ScrollToCaret();}
    public void SetPalette(Palette p){colors=p;BackColor=p.Surface;ForeColor=p.Text;Render();}
}
