using System.ComponentModel;

namespace KayBrowser;

public sealed class AssistantPanel : Panel, IThemed
{
    readonly Label title=new(){Text="Clean AI",Font=new Font("Segoe UI",14,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    readonly Label note=new(){Text="Seu copiloto, sob seu controle.",Dock=DockStyle.Fill,AutoEllipsis=true};
    readonly TextBox transcript=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.None,Font=new Font("Segoe UI",10),TabStop=false};
    readonly TextBox prompt=new(){Dock=DockStyle.Fill,Multiline=true,MaxLength=4000,PlaceholderText="Peça um resumo ou uma tarefa…",BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.None};
    readonly Label state=new(){Dock=DockStyle.Fill,Text="Pronto · conecta somente ao enviar",AutoEllipsis=true};
    readonly SoftButton send=new(){Text="Enviar",Glyph="spark",Dock=DockStyle.Fill};
    readonly SoftButton stop=new(){Text="Parar",Dock=DockStyle.Fill,Enabled=false};
    readonly SoftButton setup=new(){Text="Configurar IA",Dock=DockStyle.Fill};
    readonly SoftButton clear=new(){Text="Limpar",Dock=DockStyle.Fill};
    readonly SoftButton close=new(){Glyph="close",AccessibleName="Fechar assistente",Dock=DockStyle.Fill};
    readonly CheckBox share=new(){Text="Compartilhar página e abas nesta tarefa",Dock=DockStyle.Fill,AutoSize=false};
    public bool Share => share.Checked;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Prompt { get=>prompt.Text; set=>prompt.Text=value; }
    public event Action? SendRequested, StopRequested, SetupRequested, CloseRequested;
    public AssistantPanel()
    {
        Dock=DockStyle.Fill;Padding=new Padding(16,12,16,12);AccessibleName="Assistente de IA";
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Margin=Padding.Empty};
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(var height in new[]{42,28,0,42,30,86,44,40})grid.RowStyles.Add(new RowStyle(height==0?SizeType.Percent:SizeType.Absolute,height==0?100:height));
        var head=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,34));head.RowStyles.Add(new RowStyle(SizeType.Percent,100));head.Controls.Add(title,0,0);head.Controls.Add(close,1,0);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));actions.Controls.Add(send,0,0);actions.Controls.Add(stop,1,0);
        var settings=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));settings.RowStyles.Add(new RowStyle(SizeType.Percent,100));settings.Controls.Add(setup,0,0);settings.Controls.Add(clear,1,0);
        var promptBox=new RoundedPanel { Dock=DockStyle.Fill,Padding=new Padding(12,10,12,10),Radius=12 };promptBox.Controls.Add(prompt);
        grid.Controls.Add(head,0,0);grid.Controls.Add(note,0,1);grid.Controls.Add(transcript,0,2);grid.Controls.Add(share,0,3);grid.Controls.Add(state,0,4);grid.Controls.Add(promptBox,0,5);grid.Controls.Add(actions,0,6);grid.Controls.Add(settings,0,7);Controls.Add(grid);
        close.Click+=(_,_)=>CloseRequested?.Invoke();setup.Click+=(_,_)=>SetupRequested?.Invoke();stop.Click+=(_,_)=>StopRequested?.Invoke();send.Click+=(_,_)=>SendRequested?.Invoke();clear.Click+=(_,_)=>Reset();
        prompt.KeyDown+=(_,e)=>{if(e.Control&&e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(send.Enabled)SendRequested?.Invoke();}};
        Reset();
    }
    public void Reset(){transcript.Clear();Add("Clean AI","Posso resumir, explicar e ajudar a navegar. Ative o compartilhamento se quiser usar o conteúdo das páginas. Cada pedido começa uma tarefa nova; as conversas ficam apenas nesta sessão.");}
    public void Add(string who,string text)
    {
        if(transcript.TextLength>28000)transcript.Clear();
        transcript.SelectionStart=transcript.TextLength;transcript.AppendText(who+"\n"+text[..Math.Min(text.Length,20000)]+"\n\n");transcript.ScrollToCaret();
    }
    public void Busy(bool busy){send.Enabled=!busy;setup.Enabled=!busy;clear.Enabled=!busy;share.Enabled=!busy;stop.Enabled=busy;state.Text=busy?"Trabalhando… · você pode parar":"Pronto · conecta somente ao enviar";}
    public void SetPalette(Palette p)
    {
        void Theme(Control c){c.BackColor=p.Surface;c.ForeColor=p.Text;if(c!=this && c is IThemed themed)themed.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}
        Theme(this);note.ForeColor=p.Muted;state.ForeColor=p.Muted;prompt.BackColor=p.Surface;
    }
}
