using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace KayBrowser;

public sealed class AssistantPanel : Panel,IThemed
{
    readonly ChatTranscript transcript=new(){Dock=DockStyle.Fill};
    readonly TextBox prompt=new(){Dock=DockStyle.Fill,Multiline=true,MaxLength=4000,PlaceholderText="Como posso ajudar?",BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.None,Font=new Font("Segoe UI",10)};
    readonly Label state=new(){Dock=DockStyle.Fill,Text="Sob seu controle · Ctrl+Enter para enviar",AutoEllipsis=true,Font=new Font("Segoe UI",8)};
    readonly SoftButton send=new(){Text="Enviar",Glyph="forward",Dock=DockStyle.Fill},stop=new(){Text="Parar",Dock=DockStyle.Fill,Enabled=false};
    readonly SoftButton setup=new(){Text="Configurar IA",Glyph="settings",Dock=DockStyle.Fill},clear=new(){Text="Nova tarefa",Dock=DockStyle.Fill};
    readonly CheckBox share=new(){Text="Compartilhar página e abas",Dock=DockStyle.Fill,AutoSize=false};
    readonly SoftButton like=new(){Text="Útil",Glyph="star",Width=86,Height=32},dislike=new(){Text="Rever",Glyph="reload",Width=92,Height=32};
    Palette colors=Palette.Create(false,Color.SteelBlue);
    public bool Share=>share.Checked;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public string Prompt{get=>prompt.Text;set=>prompt.Text=value;}
    public event Action? SendRequested,StopRequested,SetupRequested,CloseRequested,ExpandRequested;
    public AssistantPanel()
    {
        Dock=DockStyle.Fill;Padding=new Padding(18,18,18,12);AccessibleName="Clean AI · assistente integrado";DoubleBuffered=true;
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Margin=Padding.Empty};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(int h in new[]{72,0,36,26,32,80,40,40})grid.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var logo=new PictureBox{Image=BrandAssets.Logo,SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Top,Height=38};var title=new Label{Text="Clean AI",Font=new Font("Georgia",22),Dock=DockStyle.Fill,AutoEllipsis=true};
        var expand=new SoftButton{Glyph="max",AccessibleName="Expandir ou reduzir IA",Dock=DockStyle.Top,Height=28};var close=new SoftButton{Glyph="close",AccessibleName="Fechar IA",Dock=DockStyle.Top,Height=28};expand.Click+=(_,_)=>ExpandRequested?.Invoke();close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(logo,0,0);header.Controls.Add(title,1,0);header.Controls.Add(expand,2,0);header.Controls.Add(close,3,0);
        var feedback=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};var copy=new SoftButton{Text="Copiar",Glyph="code",Width=96,Height=32};
        copy.Click+=(_,_)=>{if(transcript.LastAnswer.Length>0)try{Clipboard.SetText(transcript.LastAnswer);state.Text="Resposta copiada";}catch(System.Runtime.InteropServices.ExternalException){state.Text="Área de transferência ocupada";}};
        like.Click+=(_,_)=>{transcript.Rate(1);state.Text="Marcada como útil · só nesta sessão";};dislike.Click+=(_,_)=>{transcript.Rate(-1);state.Text="Marcada para rever · só nesta sessão";};feedback.Controls.AddRange([copy,like,dislike]);
        var promptBox=new RoundedPanel{Dock=DockStyle.Fill,Radius=18,Padding=new Padding(14,12,14,10)};promptBox.Controls.Add(prompt);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));actions.Controls.Add(send,0,0);actions.Controls.Add(stop,1,0);
        var settings=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));settings.RowStyles.Add(new RowStyle(SizeType.Percent,100));settings.Controls.Add(setup,0,0);settings.Controls.Add(clear,1,0);
        grid.Controls.Add(header,0,0);grid.Controls.Add(transcript,0,1);grid.Controls.Add(feedback,0,2);grid.Controls.Add(state,0,3);grid.Controls.Add(share,0,4);grid.Controls.Add(promptBox,0,5);grid.Controls.Add(actions,0,6);grid.Controls.Add(settings,0,7);Controls.Add(grid);
        setup.Click+=(_,_)=>SetupRequested?.Invoke();stop.Click+=(_,_)=>StopRequested?.Invoke();send.Click+=(_,_)=>SendRequested?.Invoke();clear.Click+=(_,_)=>Reset();prompt.KeyDown+=(_,e)=>{if(e.Control&&e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(send.Enabled)SendRequested?.Invoke();}};
        Reset();SetPalette(colors);
    }
    public void Reset(){transcript.ClearMessages();transcript.Add("Clean AI","Entendimento. Contexto. Próximos passos.\n\nPosso resumir páginas e ajudar na navegação. Ative o compartilhamento quando quiser usar o conteúdo das abas. Cada pedido inicia uma tarefa nova.");}
    public void Add(string who,string text)=>transcript.Add(who,text);
    public void Busy(bool busy){send.Enabled=!busy;setup.Enabled=!busy;clear.Enabled=!busy;share.Enabled=!busy;stop.Enabled=busy;state.Text=busy?"Trabalhando… · você pode parar":"Sob seu controle · Ctrl+Enter para enviar";}
    public void SetPalette(Palette p)
    {
        colors=p;
        void Theme(Control c){c.BackColor=p.Surface;c.ForeColor=p.Text;if(c!=this&&c is IThemed themed)themed.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}
        Theme(this);state.ForeColor=p.Muted;transcript.SetPalette(p);prompt.BackColor=p.Surface;Invalidate();
    }
}
public sealed class ChatTranscript : Control,IThemed
{
    sealed record Message(string Who,string Text){public int Rating;}
    readonly List<Message> messages=[];Palette colors=Palette.Create(false,Color.SteelBlue);int scroll;bool bottom=true;
    public string LastAnswer=>messages.LastOrDefault(m=>m.Who=="Clean AI")?.Text??"";
    public ChatTranscript(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleRole=AccessibleRole.Text;AccessibleName="Conversa de IA · role para ler";TabStop=true;}
    public void Add(string who,string text){if(messages.Count>=24)messages.RemoveAt(0);messages.Add(new(who,text[..Math.Min(20000,text.Length)]));while(messages.Count>1&&messages.Sum(m=>m.Text.Length)>80000)messages.RemoveAt(0);bottom=true;Invalidate();}
    public void ClearMessages(){messages.Clear();scroll=0;bottom=true;Invalidate();}
    public void Rate(int rating){var last=messages.LastOrDefault(m=>m.Who=="Clean AI");if(last!=null){last.Rating=rating;Invalidate();}}
    public void SetPalette(Palette p){colors=p;BackColor=p.Surface;ForeColor=p.Text;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(colors.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
        if(Width<30||Height<1)return;using var font=new Font("Segoe UI",10);using var small=new Font("Segoe UI",8);
        int d=(int)Math.Round(DeviceDpi/96f*12),w=Math.Max(40,Width-d*3);var heights=messages.Select(m=>Math.Min(6000,TextRenderer.MeasureText(m.Text,font,new Size(Math.Max(30,w-d*2),0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height)+d*5).ToArray();
        int total=heights.Sum()+d,maxScroll=Math.Max(0,total-Height);if(bottom)scroll=maxScroll;else scroll=Math.Clamp(scroll,0,maxScroll);int y=d-scroll;
        for(int i=0;i<messages.Count;i++)
        {
            var m=messages[i];int h=heights[i]-d;bool user=m.Who=="Você";var r=new Rectangle(user?d*2:0,y,w,h);
            if(r.Bottom>0&&r.Top<Height)
            {using var path=Shapes.Round(r,14);using var fill=new SolidBrush(user?colors.Elevated:Color.FromArgb(12,46,80));using var line=new Pen(colors.Line);g.FillPath(fill,path);g.DrawPath(line,path);
                TextRenderer.DrawText(g,m.Who+(m.Rating==1?" · Útil":m.Rating==-1?" · Rever":""),small,new Rectangle(r.X+d,r.Y+d,r.Width-d*2,d*2),colors.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g,m.Text,font,new Rectangle(r.X+d,r.Y+d*3,r.Width-d*2,Math.Max(1,r.Height-d*4)),colors.Text,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
            y+=heights[i];
        }
        if(maxScroll>0){float ratio=Height/(float)total;using var b=new SolidBrush(colors.Line);g.FillRectangle(b,Width-3,(int)(scroll/(float)total*Height),3,Math.Max(16,Height*ratio));}
    }
    protected override void OnMouseWheel(MouseEventArgs e){bottom=false;scroll=Math.Max(0,scroll-e.Delta/3);Invalidate();base.OnMouseWheel(e);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.End){bottom=true;Invalidate();}else if(e.KeyCode is Keys.Up or Keys.PageUp){bottom=false;scroll=Math.Max(0,scroll-80);Invalidate();}else if(e.KeyCode is Keys.Down or Keys.PageDown){bottom=false;scroll+=80;Invalidate();}}
}
