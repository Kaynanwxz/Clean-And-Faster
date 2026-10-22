using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace KayBrowser;

public sealed class ShortcutTile : SoftButton
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TileGlyph {get;set;}="globe";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption {get;set;}="";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Brand {get;set;}=Color.MediumAquamarine;
    public ShortcutTile(){Size=new(112,104);Margin=new Padding(7);AccessibleRole=AccessibleRole.PushButton;}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??Colors.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=Shapes.Round(new(2,1,Width-4,Height-3),16))using(var fill=new SolidBrush(Motion.Blend(Colors.Surface,Colors.Elevated,HoverAmount)))using(var line=new Pen(Motion.Blend(Colors.Line,Colors.Accent,HoverAmount))){g.FillPath(fill,path);g.DrawPath(line,path);}
        var r=new Rectangle((Width-D(40))/2,D(17),D(40),D(40));
        using(var path=Shapes.Round(r,12))using(var fill=new SolidBrush(Color.FromArgb(24,Brand)))g.FillPath(fill,path);
        Glyphs.Draw(g,TileGlyph,new(r.X+D(9),r.Y+D(9),D(22),D(22)),Brand);
        using var font=new Font("Segoe UI",9.5f);
        TextRenderer.DrawText(g,Caption,font,new Rectangle(D(7),D(67),Width-D(14),D(24)),Colors.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
}
public sealed class HomeBoard : Panel,IThemed
{
    Palette colors=Palette.Create(false,Color.MediumAquamarine);
    readonly RoundedPanel searchBox=new(){Radius=18};
    readonly TextBox search=new(){BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",14),PlaceholderText="Pesquise ou digite um endereço",AccessibleName="Pesquisar na web"};
    readonly SoftButton searchIcon=new(){Glyph="search",TabStop=false,Enabled=false};
    readonly SoftButton go=new(){Glyph="forward",AccessibleName="Pesquisar"};
    readonly FlowLayoutPanel shortcuts=new(){WrapContents=true,AutoScroll=true};
    readonly SoftButton customize=new(){Text="Personalizar",AccessibleName="Personalizar o Clean Browser"};
    readonly SoftButton memory=new(){Text="Economia de memória",AccessibleName="Controle de memória"};
    readonly string engine;
    int left,top,contentWidth;
    int D(int value) => (int)Math.Round(value * DeviceDpi / 96f);
    public HomeBoard(string engineName,IEnumerable<Bookmark> bookmarks,Action<string> navigate,Action settings,Action showMemory)
    {
        engine=engineName;Dock=DockStyle.Fill;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
        search.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;navigate(search.Text);}};
        go.Click+=(_,_)=>navigate(search.Text);customize.Click+=(_,_)=>settings();memory.Click+=(_,_)=>showMemory();
        searchBox.Controls.AddRange([search,searchIcon,go]);Controls.AddRange([searchBox,shortcuts,customize,memory]);
        var links=new[]{new Bookmark("Google","https://www.google.com"),new Bookmark("YouTube","https://www.youtube.com"),new Bookmark("GitHub","https://github.com")}.Concat(bookmarks).DistinctBy(x=>x.Url).Take(6);
        foreach(var link in links)
        {
            var tile=new ShortcutTile{Caption=link.Title,AccessibleName=link.Title};
            if(link.Url.Contains("youtube.com")){tile.TileGlyph="play";tile.Brand=Color.FromArgb(255,123,135);}
            else if(link.Url.Contains("github.com")){tile.TileGlyph="code";tile.Brand=Color.FromArgb(184,166,247);}
            else if(link.Url.Contains("google.com")){tile.TileGlyph="search";tile.Brand=Color.FromArgb(131,185,255);}
            else {tile.TileGlyph="globe";tile.Brand=Color.FromArgb(166,228,203);}
            tile.Click+=(_,_)=>navigate(link.Url);shortcuts.Controls.Add(tile);
        }
        LayoutContent();
    }
    public void SetPalette(Palette p)
    {
        colors=p;BackColor=p.Background;ForeColor=p.Text;shortcuts.BackColor=p.Background;
        searchBox.SetPalette(p);search.BackColor=p.Surface;search.ForeColor=p.Text;
        foreach(var c in new[]{searchIcon,go,customize,memory})c.SetPalette(p);
        foreach(ShortcutTile c in shortcuts.Controls)c.SetPalette(p);
        Invalidate();
    }
    void LayoutContent()
    {
        if (searchBox == null || shortcuts == null) return;
        contentWidth=Math.Min(D(790),Math.Max(D(300),Width-D(80)));left=(Width-contentWidth)/2;
        top=Math.Clamp((Height-D(430))/3,D(16),D(112));
        searchBox.SetBounds(left,top+D(143),contentWidth,D(64));
        searchIcon.SetBounds(D(15),D(16),D(32),D(32));go.SetBounds(contentWidth-D(49),D(14),D(36),D(36));
        int searchHeight=search.PreferredHeight;
        search.SetBounds(D(58),Math.Max(0,(D(64)-searchHeight)/2),Math.Max(D(100),contentWidth-D(120)),searchHeight);
        foreach(ShortcutTile tile in shortcuts.Controls){tile.Size=new Size(D(112),D(104));tile.Margin=new Padding(D(7));}
        int cols=Math.Max(1,Math.Min(shortcuts.Controls.Count,(Width-D(70))/Math.Max(1,D(126)))),w=cols*D(126);
        int rows=(shortcuts.Controls.Count+cols-1)/cols;
        int available=Math.Max(D(100),Height-top-D(309));
        w+=rows*D(118)>available?SystemInformation.VerticalScrollBarWidth:D(2);
        shortcuts.SetBounds((Width-w)/2,top+D(231),w,Math.Min(Math.Max(1,rows)*D(118)+D(2),available));
        memory.SetBounds(left,Height-D(61),D(215),D(36));customize.SetBounds(left+contentWidth-D(140),Height-D(61),D(140),D(36));
    }
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);LayoutContent();Invalidate();}
    protected override void OnResize(EventArgs e){base.OnResize(e);LayoutContent();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var glow=new GraphicsPath())
        {
            glow.AddEllipse(left-D(140),top-D(90),contentWidth+D(280),D(330));
            using var brush=new PathGradientBrush(glow){CenterColor=Color.FromArgb(14,colors.Accent),SurroundColors=[Color.FromArgb(0,colors.Accent)]};g.FillPath(brush,glow);
        }
        using var small=new Font("Segoe UI",9,FontStyle.Bold);using var large=new Font("Segoe UI",Math.Clamp(contentWidth/(29f*DeviceDpi/96f),18,31),FontStyle.Bold);using var normal=new Font("Segoe UI",11);
        TextRenderer.DrawText(g,"C L E A N   B R O W S E R",small,new Rectangle(left,top,contentWidth,D(25)),colors.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g,"Explore com mais leveza.",large,new Rectangle(left,top+D(36),contentWidth,D(62)),colors.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g,"Seu próximo destino começa aqui.",normal,new Rectangle(left,top+D(101),contentWidth,D(28)),colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g,"Pesquisar com "+engine,small,new Rectangle(left,top+D(214),contentWidth,D(20)),colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);
        using var line=new Pen(colors.Line);g.DrawLine(line,left,Height-D(80),left+contentWidth,Height-D(80));
    }
}
