namespace KayBrowser;

// Every control commits one explicit setting immediately; there is no mixed draft/apply state.
public sealed class AppearancePanel : Panel,IThemed
{
    readonly Storage store;readonly Action changed,upload;readonly Label status=new(){Dock=DockStyle.Fill,Text="Salvo automaticamente",TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    readonly Panel viewport=new(){Dock=DockStyle.Fill,AutoScroll=true,Margin=Padding.Empty};
    readonly List<ChoiceButton> navigation=[];readonly List<(ChoiceButton Button,Func<bool> Selected)> choices=[];readonly List<TableLayoutPanel> pages=[];
    readonly CheckBox animations=new(){Text="Animações e transições",Dock=DockStyle.Fill,AccessibleName="Animações e transições"},focus=new(){Text="Modo foco",Dock=DockStyle.Fill,AccessibleName="Modo foco"};
    readonly CheckBox pointillism=new(){Text="Arte em pontinhos",Dock=DockStyle.Fill,AccessibleName="Arte em pontinhos"};
    readonly CheckBox bottomBar=new(){Text="Mostrar barra inferior",Dock=DockStyle.Fill,AccessibleName="Mostrar barra inferior"};
    readonly TextBox accent=new(){Dock=DockStyle.Fill,MaxLength=7,AccessibleName="Cor hexadecimal"};readonly Label wallpaper=new(){Dock=DockStyle.Fill,AutoEllipsis=true};
    Palette palette=Palette.Create(true,Color.SteelBlue);bool syncing;int activePage;readonly Tween pageMotion=new();
    public event Action? CloseRequested;
    public AppearancePanel(Storage data,Action apply,Action chooseWallpaper)
    {
        store=data;changed=apply;upload=chooseWallpaper;Dock=DockStyle.Fill;Padding=new Padding(18,12,18,10);AccessibleName="Aparência do Clean Browser";DoubleBuffered=true;
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{70,42,0,42})body.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.Controls.Add(new Label{Text="Aparência",Font=new Font("Georgia",22),TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill},0,0);var close=new SoftButton{Glyph="close",AccessibleName="Fechar aparência",Dock=DockStyle.Fill};close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(close,1,0);
        var nav=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};nav.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(var (name,index) in new[]{("Visuais",0),("Layout",1),("Fundo",2)}){nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));var b=new ChoiceButton{Text=name,Dock=DockStyle.Fill,AccessibleName="Aparência: "+name};b.Click+=(_,_)=>SelectPage(index);nav.Controls.Add(b,index,0);navigation.Add(b);}
        for(int i=0;i<3;i++){var p=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty,Padding=new Padding(0,8,8,12),Visible=false};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pages.Add(p);}
        var visual=pages[0];Heading(visual,"Tema da página");Options(visual,[("Claro",()=>store.Settings.Theme=="Claro",()=>store.Settings.Theme="Claro"),("Escuro",()=>store.Settings.Theme=="Escuro",()=>store.Settings.Theme="Escuro")]);Heading(visual,"10 visuais para explorar");
        var cards=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=5,Margin=Padding.Empty};cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        for(int i=0;i<VisualPresets.All.Length;i++){var v=VisualPresets.All[i];if(i%2==0)cards.RowStyles.Add(new RowStyle(SizeType.Absolute,94));var b=new PresetCard(v){Dock=DockStyle.Fill,AccessibleName="Visual: "+v.Name,Margin=new Padding(1)};choices.Add((b,()=>store.Settings.Visual==v.Name));b.Click+=(_,_)=>Commit(()=>VisualPresets.Apply(store.Settings,v.Name));cards.Controls.Add(b,i%2,i/2);}Row(visual,cards,470);
        var layout=pages[1];Heading(layout,"Abas");Options(layout,[("Superior",()=>!store.Settings.VerticalTabs,()=>store.Settings.VerticalTabs=false),("Lateral",()=>store.Settings.VerticalTabs,()=>store.Settings.VerticalTabs=true)]);
        Heading(layout,"Barra do navegador");Options(layout,[("Azul-marinho",()=>store.Settings.DarkChrome,()=>store.Settings.DarkChrome=true),("Clara",()=>!store.Settings.DarkChrome,()=>store.Settings.DarkChrome=false)]);
        Heading(layout,"Espaçamento");Options(layout,[("Confortável",()=>!store.Settings.Compact,()=>store.Settings.Compact=false),("Compacto",()=>store.Settings.Compact,()=>store.Settings.Compact=true)]);
        Heading(layout,"Página inicial");Options(layout,new[]{"Clássico","Central","Editorial","Essencial","Espaços","Cristal","Biblioteca"}.Select(v=>(v,(Func<bool>)(()=>store.Settings.HomeLayout==v),(Action)(()=>store.Settings.HomeLayout=v))).ToArray(),2);
        Heading(layout,"Atalhos da página inicial");Options(layout,[("Meus atalhos",()=>store.Settings.ShowShortcuts&&!store.Settings.FrequentShortcuts,()=>{store.Settings.ShowShortcuts=true;store.Settings.FrequentShortcuts=false;}),("Mais visitados",()=>store.Settings.ShowShortcuts&&store.Settings.FrequentShortcuts,()=>{store.Settings.ShowShortcuts=true;store.Settings.FrequentShortcuts=true;}),("Ocultar",()=>!store.Settings.ShowShortcuts,()=>store.Settings.ShowShortcuts=false)],2);
        Row(layout,bottomBar,38);bottomBar.CheckedChanged+=(_,_)=>{if(!syncing)Commit(()=>store.Settings.ShowBottomBar=bottomBar.Checked);};
        Row(layout,animations,38);Row(layout,focus,38);animations.CheckedChanged+=(_,_)=>{if(!syncing)Commit(()=>store.Settings.Animations=animations.Checked);};focus.CheckedChanged+=(_,_)=>{if(!syncing)Commit(()=>store.Settings.FocusMode=focus.Checked);};
        var bg=pages[2];Heading(bg,"Arte e plano de fundo");Options(bg,new[]{"Arte","Suave","Sem arte","Personalizado"}.Select(v=>(v,(Func<bool>)(()=>store.Settings.Background==v),(Action)(()=>{if(v=="Personalizado"&&string.IsNullOrEmpty(store.Settings.Wallpaper)){upload();return;}store.Settings.Background=v;}))).ToArray(),2);
        Row(bg,pointillism,38);pointillism.CheckedChanged+=(_,_)=>{if(!syncing)Commit(()=>store.Settings.PointillistBackground=pointillism.Checked);};
        Row(bg,wallpaper,40);var imageActions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};imageActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));imageActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));imageActions.RowStyles.Add(new RowStyle(SizeType.Percent,100));var add=new SoftButton{Text="Escolher imagem",Glyph="plus",Dock=DockStyle.Fill,AccessibleName="Escolher imagem"};add.Click+=(_,_)=>{upload();Sync();};var remove=new SoftButton{Text="Remover",Dock=DockStyle.Fill,AccessibleName="Remover imagem"};remove.Click+=(_,_)=>Commit(()=>{store.Settings.Wallpaper="";store.Settings.Background="Sem arte";});imageActions.Controls.Add(add,0,0);imageActions.Controls.Add(remove,1,0);Row(bg,imageActions,42);
        Heading(bg,"Cor de destaque");var swatches=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1};swatches.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(var (hex,index) in new[]{"#8FBBEF","#A9C9EF","#5D96D5","#C4D2E2","#F2F5FA"}.Select((s,i)=>(s,i))){swatches.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));var b=new AccentSwatch(hex){Dock=DockStyle.Fill,AccessibleName="Destaque "+hex};choices.Add((b,()=>store.Settings.Accent.Equals(hex,StringComparison.OrdinalIgnoreCase)));b.Click+=(_,_)=>Commit(()=>store.Settings.Accent=hex);swatches.Controls.Add(b,index,0);}Row(bg,swatches,46);Row(bg,accent,34);
        accent.TextChanged+=(_,_)=>{if(syncing)return;if(!IsHex(accent.Text)){status.Text="Use # seguido de 6 dígitos (ex.: #8FBBEF)";return;}Commit(()=>store.Settings.Accent=accent.Text,keepText:true);};
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));var reset=new SoftButton{Text="Restaurar",AccessibleName="Restaurar aparência",Dock=DockStyle.Fill};reset.Click+=(_,_)=>Commit(()=>{VisualPresets.ApplyDefault(store.Settings);store.Settings.Wallpaper="";});footer.Controls.Add(status,0,0);footer.Controls.Add(reset,1,0);
        body.Controls.Add(header,0,0);body.Controls.Add(nav,0,1);body.Controls.Add(viewport,0,2);body.Controls.Add(footer,0,3);Controls.Add(body);SelectPage(0,false);Sync();
        viewport.Resize+=(_,_)=>FitPage();
    }
    static bool IsHex(string value)=>value.Length==7&&value[0]=='#'&&value[1..].All(Uri.IsHexDigit);
    static void Row(TableLayoutPanel page,Control control,int height){int row=page.RowCount++;page.RowStyles.Add(new RowStyle(SizeType.Absolute,height));page.Controls.Add(control,0,row);control.Dock=DockStyle.Fill;}
    static void Heading(TableLayoutPanel page,string text)=>Row(page,new Label{Text=text,Font=new Font("Segoe UI",9.5f,FontStyle.Bold),TextAlign=ContentAlignment.BottomLeft,Padding=new Padding(0,0,0,7)},42);
    void Options(TableLayoutPanel page,(string Name,Func<bool> Selected,Action Change)[] items,int columns=2)
    {
        var grid=new TableLayoutPanel{ColumnCount=columns,RowCount=(items.Length+columns-1)/columns,Dock=DockStyle.Fill,Margin=Padding.Empty};for(int i=0;i<columns;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));for(int i=0;i<grid.RowCount;i++)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        for(int i=0;i<items.Length;i++){var item=items[i];var b=new ChoiceButton{Text=item.Name,Dock=DockStyle.Fill,AccessibleName=item.Name};b.Click+=(_,_)=>Commit(item.Change);choices.Add((b,item.Selected));grid.Controls.Add(b,i%columns,i/columns);}Row(page,grid,grid.RowCount*40);
    }
    void Commit(Action change,bool keepText=false)
    {
        if(syncing)return;
        try{string oldWallpaper=store.Settings.Wallpaper;change();store.SaveSettings();changed();Sync(keepText);status.Text="Salvo automaticamente";
            if(oldWallpaper!=store.Settings.Wallpaper&&oldWallpaper.StartsWith(Path.Combine(Storage.Root,"Appearance")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))try{File.Delete(oldWallpaper);}catch(IOException){}
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){status.Text="Não foi possível salvar. Tente novamente.";}
    }
    internal void Sync(bool keepText=false)
    {
        syncing=true;try{foreach(var (b,test) in choices){b.Selected=test();b.Invalidate();}pointillism.Checked=store.Settings.PointillistBackground;bottomBar.Checked=store.Settings.ShowBottomBar;animations.Checked=store.Settings.Animations;focus.Checked=store.Settings.FocusMode;if(!keepText)accent.Text=store.Settings.Accent;wallpaper.Text=string.IsNullOrEmpty(store.Settings.Wallpaper)?"Imagem do navegador ou fundo simples":Path.GetFileName(store.Settings.Wallpaper);}finally{syncing=false;}
    }
    internal void SelectPage(int page,bool animate=true)
    {
        pageMotion.Stop();if(viewport.Controls.Count>0){viewport.Controls[0].Visible=false;viewport.Controls.Clear();}activePage=page;var selected=pages[page];viewport.AutoScrollPosition=Point.Empty;viewport.Controls.Add(selected);selected.Visible=true;FitPage();for(int i=0;i<navigation.Count;i++){navigation[i].Selected=i==page;navigation[i].Invalidate();}
        if(animate){selected.Dock=DockStyle.None;int y=selected.Top;pageMotion.Start(18,0,v=>selected.Top=y+(int)v,190,()=>{selected.Dock=DockStyle.Top;FitPage();});}SetPalette(palette);
    }
    void FitPage(){var p=pages[activePage];if(p.Dock==DockStyle.None)p.Width=Math.Max(1,viewport.ClientSize.Width-SystemInformation.VerticalScrollBarWidth);}
    public void SetPalette(Palette unused)
    {
        palette=Palette.Create(store.Settings.Theme=="Claro",ColorTranslator.FromHtml(store.Settings.Accent));var p=palette;
        void Theme(Control c){c.BackColor=p.Background;c.ForeColor=p.Text;if(c is SoftButton b)b.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}Theme(this);foreach(var page in pages)if(page.Parent==null)Theme(page);status.ForeColor=p.Muted;Invalidate();
    }
    internal void ClickChoice(string name)
    {
        foreach(var page in pages){var button=Descendants(page).OfType<Button>().FirstOrDefault(b=>b.AccessibleName==name);if(button!=null){SelectPage(pages.IndexOf(page),false);button.PerformClick();return;}}
        throw new InvalidOperationException("Opção ausente: "+name);
    }
    internal static IEnumerable<Control> Descendants(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    internal void SetAccentForDiagnostic(string value)=>accent.Text=value;
    protected override void Dispose(bool disposing){if(disposing){pageMotion.Dispose();foreach(var p in pages)if(p.Parent==null)p.Dispose();}base.Dispose(disposing);}
}
public sealed class AccentSwatch(string hex):ChoiceButton
{
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var b=new SolidBrush(ColorTranslator.FromHtml(hex));e.Graphics.FillEllipse(b,new Rectangle(Width/2-D(12),Height/2-D(12),D(24),D(24)));}
}
