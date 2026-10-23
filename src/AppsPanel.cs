namespace KayBrowser;

public sealed class AppsPanel : Panel,IThemed
{
    readonly Storage store;readonly Action<WebAppLink> open;readonly Action add;readonly FlowLayoutPanel list=new(){Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};
    Palette colors=Palette.Create(true,Color.SteelBlue);
    public event Action? CloseRequested;
    public AppsPanel(Storage data,Action<WebAppLink> launch,Action create)
    {
        store=data;open=launch;add=create;AccessibleName="Apps do Clean Browser";Padding=new Padding(12);DoubleBuffered=true;
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(int h in new[]{42,62,0,42,66})body.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        header.Controls.Add(new Label{Text="Seus apps",Font=new Font("Georgia",20),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
        var close=new SoftButton{Glyph="close",AccessibleName="Fechar apps",Dock=DockStyle.Fill};close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(close,1,0);
        body.Controls.Add(header,0,0);body.Controls.Add(new Label{Text="Abra, entre na sua conta e use o app aqui. As sessões ficam no perfil do navegador.",Dock=DockStyle.Fill,Padding=new Padding(0,7,0,0)},0,1);body.Controls.Add(list,0,2);
        var addButton=new SoftButton{Text="Adicionar outro app",Glyph="plus",Dock=DockStyle.Fill,AccessibleName="Adicionar outro app"};addButton.Click+=(_,_)=>add();body.Controls.Add(addButton,0,3);
        body.Controls.Add(new Label{Text="Botão direito em um app para remover. Remover não apaga sua conta nem encerra o login. Reprodução protegida depende do serviço.",Dock=DockStyle.Fill,Font=new Font("Segoe UI",8.5f),Padding=new Padding(0,8,0,0)},0,4);Controls.Add(body);
        list.Resize+=(_,_)=>Fit();RefreshApps();
    }
    public void RefreshApps()
    {
        foreach(Control c in list.Controls.Cast<Control>().ToArray())c.Dispose();
        foreach(var app in store.Settings.Apps)
        {
            var b=new AppTile(app){Height=66,Margin=new Padding(0,0,0,6),AccessibleName="Abrir app "+app.Name};b.Click+=(_,_)=>open(app);
            var menu=new ContextMenuStrip();menu.Items.Add("Remover "+app.Name,null,(_,_)=>{store.Settings.Apps.RemoveAll(a=>a.Id==app.Id);store.SaveSettings();RefreshApps();});b.ContextMenuStrip=menu;b.Disposed+=(_,_)=>menu.Dispose();list.Controls.Add(b);
        }
        if(list.Controls.Count==0)list.Controls.Add(new Label{Text="Seus apps aparecem aqui. Use Adicionar outro app.",Height=64});Fit();SetPalette(colors);
    }
    void Fit(){foreach(Control c in list.Controls)c.Width=Math.Max(100,list.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-2);}
    public void SetPalette(Palette p)
    {
        colors=p;void Theme(Control c){c.BackColor=p.Background;c.ForeColor=p.Text;if(c is IThemed t&&c!=this)t.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}Theme(this);
        foreach(Control c in list.Controls)if(c.ContextMenuStrip is {} m){m.Renderer=new KayMenuRenderer(p);m.BackColor=p.Surface;m.ForeColor=p.Text;}
    }
    sealed class AppTile(WebAppLink app):SoftButton
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;var icon=new Rectangle(D(12),(Height-D(28))/2,D(28),D(28));if(!BrandMarks.Draw(g,app.Url,icon,Colors.Text))Glyphs.Draw(g,"globe",icon,Colors.Text);
            using var title=new Font("Segoe UI",10,FontStyle.Bold);using var host=new Font("Segoe UI",8.5f);
            TextRenderer.DrawText(g,app.Name,title,new Rectangle(D(54),D(10),Width-D(64),D(23)),Colors.Text,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g,new Uri(app.Url).IdnHost,host,new Rectangle(D(54),D(34),Width-D(64),D(21)),Colors.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
    }
}

public sealed class WebAppEditor : AppDialog
{
    public WebAppLink? App {get;private set;}
    public WebAppEditor(Palette colors):base("Adicionar app",colors,560,380)
    {
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(20)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{26,36,26,36,50,42})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        var name=new TextBox{Dock=DockStyle.Fill,MaxLength=40,AccessibleName="Nome do app"};var url=new TextBox{Dock=DockStyle.Fill,MaxLength=4096,PlaceholderText="https://",AccessibleName="Endereço do app"};var error=new Label{Dock=DockStyle.Fill,AutoEllipsis=true};var save=new SoftButton{Text="Adicionar",Dock=DockStyle.Right,Width=130,AccessibleName="Salvar app"};save.SetPalette(colors);
        grid.Controls.Add(new Label{Text="Nome",Dock=DockStyle.Fill},0,0);grid.Controls.Add(name,0,1);grid.Controls.Add(new Label{Text="Endereço HTTPS",Dock=DockStyle.Fill},0,2);grid.Controls.Add(url,0,3);grid.Controls.Add(error,0,4);grid.Controls.Add(save,0,5);
        save.Click+=(_,_)=>{try{App=WebApps.Create(name.Text,url.Text);DialogResult=DialogResult.OK;Close();}catch(ArgumentException ex){error.Text=ex.Message;}};Controls.Add(grid);AcceptButton=save;
        foreach(Control c in grid.Controls){if(c is not SoftButton){c.BackColor=colors.Background;c.ForeColor=colors.Text;}}
    }
}
