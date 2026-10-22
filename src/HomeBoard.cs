using System.Drawing.Drawing2D;

namespace KayBrowser;

public sealed class HomeBoard : Panel,IThemed
{
    readonly Storage store;
    readonly RoundedPanel searchBox=new(){Radius=22};
    readonly TextBox search=new(){BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",12),PlaceholderText="Pesquisar na web ou digitar um endereço",AccessibleName="Pesquisar na web"};
    readonly SoftButton go=new(){Glyph="forward",AccessibleName="Pesquisar"};
    readonly SoftButton customize=new(){Text="Personalizar",Glyph="settings",AccessibleName="Personalizar o Clean Browser"};
    readonly SoftButton focus=new(){Text="Foco",Glyph="leaf",AccessibleName="Alternar modo foco"};
    readonly ShortcutGrid grid;
    readonly SoftButton explore=new(){Text="Explorar",Glyph="globe"},documents=new(){Text="Documentos",Glyph="code"},favorites=new(){Text="Favoritos",Glyph="star"};
    readonly FlowLayoutPanel spaces=new(){WrapContents=false,AutoScroll=false};
    readonly SoftButton memory=new(){Text="Memória",Glyph="memory"};
    Palette colors=Palette.Create(true,Color.SteelBlue);
    Image? wallpaper;string wallpaperFile="";
    int brandY,searchY,contentLeft,contentWidth;
    readonly Action<string> library;
    int D(int n)=>(int)Math.Round(n*DeviceDpi/96f);
    public HomeBoard(Storage data,Action<string> navigate,Action appearance,Action showMemory,Action<string,ShortcutLink?> shortcuts,Action<string> openLibrary,Action<string> workspace,Action toggleFocus)
    {
        store=data;library=openLibrary;Dock=DockStyle.Fill;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
        grid=new(store,navigate,shortcuts){AccessibleName="Atalhos · Enter abre; botão direito edita; arraste para reordenar"};
        search.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;navigate(search.Text);}};
        go.Click+=(_,_)=>navigate(search.Text);customize.Click+=(_,_)=>appearance();memory.Click+=(_,_)=>showMemory();focus.Click+=(_,_)=>toggleFocus();
        explore.Click+=(_,_)=>openLibrary("source");documents.Click+=(_,_)=>openLibrary("note");favorites.Click+=(_,_)=>openLibrary("favorites");
        searchBox.Controls.AddRange([search,go]);Controls.AddRange([searchBox,grid,customize,memory,focus,spaces,explore,documents,favorites]);
        foreach(var w in data.Settings.Workspaces)
        {
            var b=new SoftButton{Text=w.Name,Glyph="globe",Width=146,Height=48,Margin=new Padding(5),AccessibleName="Espaço "+w.Name};
            b.Click+=(_,_)=>workspace(w.Id);spaces.Controls.Add(b);
        }
        LoadWallpaper();LayoutContent();
    }
    void LoadWallpaper()
    {
        string path=store.Settings.Wallpaper;
        if(path==wallpaperFile)return;wallpaper?.Dispose();wallpaper=null;wallpaperFile=path;
        if(path.Length==0||!File.Exists(path))return;
        try{using var source=Image.FromFile(path);wallpaper=new Bitmap(source);}catch(Exception ex)when(ex is IOException or ArgumentException or OutOfMemoryException){wallpaperFile="";}
    }
    public void SetPalette(Palette unused)
    {
        Color accent;try{accent=ColorTranslator.FromHtml(store.Settings.Accent);}catch{accent=Color.SteelBlue;}
        colors=Palette.Create(store.Settings.Theme=="Claro",accent);BackColor=colors.Background;ForeColor=colors.Text;
        searchBox.SetPalette(colors);search.BackColor=colors.Surface;search.ForeColor=colors.Text;
        foreach(var b in new[]{go,customize,memory,focus,explore,documents,favorites})b.SetPalette(colors);
        foreach(SoftButton b in spaces.Controls)b.SetPalette(colors);
        spaces.BackColor=colors.Background;grid.SetPalette(colors);LayoutContent();Invalidate();
    }
    void LayoutContent()
    {
        if(grid==null)return;
        bool small=Width<D(650),minimal=store.Settings.FocusMode||store.Settings.HomeLayout=="Essencial";
        int max=store.Settings.HomeLayout=="Editorial"?650:760;
        contentWidth=Math.Min(D(max),Math.Max(D(230),Width-D(small?32:100)));contentLeft=store.Settings.HomeLayout=="Editorial"&&!small?D(48):(Width-contentWidth)/2;
        brandY=D(small?54:44);searchY=brandY+D(small?104:124);
        customize.SetBounds(Math.Max(0,Width-D(155)),D(9),D(145),D(34));customize.Visible=!store.Settings.FocusMode;
        searchBox.SetBounds(contentLeft,searchY,contentWidth,D(52));
        int h=search.PreferredHeight;search.SetBounds(D(18),Math.Max(0,(searchBox.Height-h)/2),Math.Max(D(120),searchBox.Width-D(70)),h);go.SetBounds(searchBox.Width-D(44),D(8),D(36),D(36));
        int gridY=searchY+D(73),footer=Height-D(42);
        int cardsHeight=!minimal&&Height>D(650)&&store.Settings.HomeLayout=="Clássico"?D(66):0;
        int gridH=Math.Min(D(store.Settings.HomeLayout=="Espaços"?112:216),Math.Max(D(100),footer-gridY-D(30)-cardsHeight));
        grid.SetBounds(contentLeft,gridY,contentWidth,gridH);grid.Visible=store.Settings.ShowShortcuts&&!store.Settings.FocusMode;
        bool workspaces=store.Settings.HomeLayout=="Espaços";
        spaces.SetBounds(contentLeft,gridY+gridH+D(8),contentWidth,D(60));spaces.Visible=workspaces&&!minimal&&spaces.Bottom<footer-D(5);
        int cardY=gridY+gridH+D(10),cardW=(contentWidth-D(16))/3;
        foreach(var (b,index) in new[]{(explore,0),(documents,1),(favorites,2)}){b.SetBounds(contentLeft+index*(cardW+D(8)),cardY,cardW,D(56));b.Visible=cardsHeight>0&&b.Bottom<footer-D(8);}
        memory.SetBounds(D(12),footer,D(122),D(34));focus.SetBounds(D(145),footer,D(100),D(34));memory.Visible=!store.Settings.FocusMode;
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);LayoutContent();}
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);LayoutContent();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        bool small=Width<D(650),minimal=store.Settings.FocusMode||store.Settings.HomeLayout=="Essencial";
        if(!store.Settings.FocusMode&&store.Settings.Background!="Sem arte")
        {
            Image art=wallpaper??BrandAssets.Sea;int w=Math.Min(Width,D(1200));int height=w*art.Height/art.Width;
            if(wallpaper!=null){BrandAssets.DrawArt(g,art,new(0,Height-height,Width,height),0.18f);}
            else BrandAssets.DrawArt(g,art,new(Width-w,Height-height,w,height),store.Settings.Background=="Suave"?.20f:small?.30f:.53f);
        }
        using var heading=new Font("Georgia",small?25:store.Settings.HomeLayout=="Editorial"?38:32,FontStyle.Regular);
        using var smallFont=new Font("Segoe UI",small?8:9,FontStyle.Regular);
        int icon=D(small?42:56);int titleW=small?D(262):D(350);int groupW=titleW+icon+D(16);int x=store.Settings.HomeLayout=="Editorial"&&!small?contentLeft:(Width-groupW)/2;
        g.DrawImage(BrandAssets.Logo,new Rectangle(x,brandY,icon,icon));
        TextRenderer.DrawText(g,"Clean Browser",heading,new Rectangle(x+icon+D(14),brandY-D(3),titleW,icon),colors.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis);
        string sub=store.Settings.HomeLayout switch{"Espaços"=>"SEU ESPAÇO, SEU RITMO", "Biblioteca"=>"PESQUISE, LEIA E ORGANIZE", "Essencial"=>"PARA ONDE VAMOS?",_=>"N A V E G A R   C O M   M A I S   C L A R E Z A"};
        TextRenderer.DrawText(g,sub,smallFont,new Rectangle(contentLeft,brandY+D(small?59:76),contentWidth,D(20)),colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
        if(store.Settings.HomeLayout=="Biblioteca"&&!minimal)
        {
            int y=grid.Bottom+D(8);using var f=new Font("Segoe UI",10);
            var entries=store.Library.Where(x=>x.Kind=="reading").Take(2).ToArray();
            if(entries.Length>0&&y+D(56)<Height-D(45))TextRenderer.DrawText(g,"Continue de onde parou: "+string.Join("  ·  ",entries.Select(x=>x.Title)),f,new Rectangle(contentLeft,y,contentWidth,D(50)),colors.Text,TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
        }
        if(Height>D(740)&&!minimal)
        {using var quote=new Font("Georgia",14,FontStyle.Italic);TextRenderer.DrawText(g,"Novos horizontes para as suas ideias.",quote,new Rectangle(contentLeft,Height-D(155),contentWidth,D(60)),colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}
    }
    protected override void Dispose(bool disposing){if(disposing)wallpaper?.Dispose();base.Dispose(disposing);}
}

public sealed class ShortcutGrid : Control,IThemed
{
    readonly Storage store;readonly Action<string> open;readonly Action<string,ShortcutLink?> command;
    Palette colors=Palette.Create(true,Color.SteelBlue);readonly ToolTip tips=new();
    int hover=-1,selected,firstRow,drag=-1;Point down;bool dragging;
    int D(int n)=>(int)Math.Round(n*DeviceDpi/96f);
    List<ShortcutLink> Links=>store.Settings.FrequentShortcuts?store.History.GroupBy(v=>new Uri(v.Url).Host).OrderByDescending(g=>g.Count()).Take(10).Select(g=>new ShortcutLink(g.Key,g.First().Title,g.First().Url)).ToList():store.Settings.Shortcuts;
    int Columns=>Math.Clamp((Width-D(12))/Math.Max(1,D(98)),1,6);
    int Rows=>Math.Max(1,(Height-D(28))/D(101));
    Rectangle Tile(int index)=>new((Width-Columns*D(98))/2+index%Columns*D(98),D(2)+(index/Columns-firstRow)*D(101),D(96),D(96));
    public ShortcutGrid(Storage data,Action<string> navigate,Action<string,ShortcutLink?> actions)
    {
        store=data;open=navigate;command=actions;TabStop=true;AccessibleRole=AccessibleRole.List;Cursor=Cursors.Hand;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
    }
    public void SetPalette(Palette p){colors=p;BackColor=p.Background;ForeColor=p.Text;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(colors.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        var links=Links;int count=links.Count+1;
        firstRow=Math.Clamp(firstRow,0,Math.Max(0,(count+Columns-1)/Columns-Rows));
        for(int i=firstRow*Columns;i<Math.Min(count,(firstRow+Rows)*Columns);i++)
        {
            var r=Tile(i);bool add=i==links.Count;
            if(i==hover||(Focused&&i==selected)){using var path=Shapes.Round(r,D(12));using var fill=new SolidBrush(colors.Elevated);g.FillPath(fill,path);}
            var circle=new Rectangle(r.X+D(24),r.Y+D(8),D(48),D(48));using(var b=new SolidBrush(colors.Elevated))g.FillEllipse(b,circle);
            if(add)Glyphs.Draw(g,"plus",Rectangle.Inflate(circle,-D(13),-D(13)),colors.Text);
            else
            {
                var cached=SiteIcons.Get(links[i].Url);
                if(cached!=null)g.DrawImage(cached,Rectangle.Inflate(circle,-D(10),-D(10)));
                else if(new Uri(links[i].Url).Host.Contains("youtube"))Glyphs.Draw(g,"play",Rectangle.Inflate(circle,-D(11),-D(11)),Color.FromArgb(218,53,75));
                else {using var f=new Font("Segoe UI",16,FontStyle.Bold);TextRenderer.DrawText(g,links[i].Name[..1].ToUpperInvariant(),f,circle,colors.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
            }
            using var font=new Font("Segoe UI",9);
            TextRenderer.DrawText(g,add?"Adicionar atalho":links[i].Name,font,new Rectangle(r.X+D(2),r.Y+D(65),r.Width-D(4),D(26)),colors.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        if((count+Columns-1)/Columns>Rows)
        {using var f=new Font("Segoe UI",8);TextRenderer.DrawText(g,"‹  Role para ver mais atalhos  ›",f,new Rectangle(0,Height-D(25),Width,D(22)),colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
    }
    int Hit(Point p){var links=Links;for(int i=firstRow*Columns;i<Math.Min(links.Count+1,(firstRow+Rows)*Columns);i++)if(Tile(i).Contains(p))return i;return -1;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int hit=Hit(e.Location);if(hover!=hit){hover=hit;var l=Links;tips.SetToolTip(this,hit>=0&&hit<l.Count?l[hit].Name+"\n"+l[hit].Url+"\nBotão direito: editar ou remover":"Adicionar atalho");Invalidate();}if(drag>=0&&Math.Abs(e.X-down.X)+Math.Abs(e.Y-down.Y)>D(8))dragging=true;}
    protected override void OnMouseLeave(EventArgs e){hover=-1;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();selected=Math.Max(0,Hit(e.Location));if(e.Button==MouseButtons.Left){drag=Hit(e.Location);down=e.Location;dragging=false;}}
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);int hit=Hit(e.Location);var links=Links;
        if(e.Button==MouseButtons.Left)
        {
            if(dragging&&drag>=0&&drag<links.Count&&hit>=0&&hit<links.Count&&!store.Settings.FrequentShortcuts){command("move:"+hit,links[drag]);}
            else if(hit>=0){if(hit==links.Count)command("edit",null);else open(links[hit].Url);}
            drag=-1;dragging=false;return;
        }
        if(e.Button!=MouseButtons.Right||hit<0)return;
        if(hit==links.Count){command("edit",null);return;}
        var link=links[hit];var menu=new ContextMenuStrip{Renderer=new KayMenuRenderer(colors),BackColor=colors.Surface,ForeColor=colors.Text};
        void Item(string title,string action)=>menu.Items.Add(title,null,(_,_)=>command(action,link));
        Item("Editar atalho","edit");Item("Remover","remove");Item("Mover para a esquerda","left");Item("Mover para a direita","right");menu.Closed+=(_,_)=>menu.Dispose();menu.Show(this,e.Location);
    }
    protected override void OnMouseWheel(MouseEventArgs e){firstRow=Math.Clamp(firstRow+(e.Delta<0?1:-1),0,Math.Max(0,(Links.Count+1+Columns-1)/Columns-Rows));Invalidate();base.OnMouseWheel(e);}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);var links=Links;
        if(e.KeyCode==Keys.Right)selected=Math.Min(links.Count,selected+1);else if(e.KeyCode==Keys.Left)selected=Math.Max(0,selected-1);
        else if(e.KeyCode==Keys.Down)selected=Math.Min(links.Count,selected+Columns);else if(e.KeyCode==Keys.Up)selected=Math.Max(0,selected-Columns);
        else if(e.KeyCode==Keys.Enter){if(selected==links.Count)command("edit",null);else if(selected<links.Count)open(links[selected].Url);}
        else if(e.KeyCode==Keys.Delete&&selected<links.Count)command("remove",links[selected]);
        else if(e.Control&&e.KeyCode==Keys.E&&selected<links.Count)command("edit",links[selected]);else return;
        firstRow=Math.Clamp(selected/Columns-Rows+1,0,Math.Max(0,selected/Columns));e.Handled=e.SuppressKeyPress=true;Invalidate();
    }
    protected override bool IsInputKey(Keys keyData)=>(keyData&Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Left or Keys.Right||base.IsInputKey(keyData);
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
}
