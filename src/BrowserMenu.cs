using System.Runtime.InteropServices;
namespace KayBrowser;

public sealed record BrowserCommand(string Id,string Title,string Hint,string Glyph,int Group,Action Run);

public sealed class BrowserMenuPanel : Panel,IThemed
{
    readonly BrowserCommand[] commands;readonly Action<string> invoke;
    readonly List<ChoiceButton> categories=[];readonly Panel viewport=new(){Dock=DockStyle.Fill,AutoScroll=true};
    readonly TableLayoutPanel rows=new(){Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty};
    readonly SoftButton zoomOut=new(){Text="−",AccessibleName="Reduzir zoom",Dock=DockStyle.Fill},zoomIn=new(){Text="+",AccessibleName="Aumentar zoom",Dock=DockStyle.Fill},full=new(){Glyph="max",AccessibleName="Tela cheia",Dock=DockStyle.Fill};
    readonly Label zoom=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter};
    Palette palette=Palette.Create(true,Color.SteelBlue);int group;
    public event Action? CloseRequested;
    public BrowserMenuPanel(BrowserCommand[] commands,Action<string> invoke)
    {
        this.commands=commands;this.invoke=invoke;Padding=new Padding(14);AccessibleName="Menu do Clean Browser";DoubleBuffered=true;Visible=false;
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{50,40,0,44,40})body.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.Controls.Add(new Label{Text="Clean Browser",Font=new Font("Georgia",17),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);var close=new SoftButton{Glyph="close",Dock=DockStyle.Fill,AccessibleName="Fechar menu"};close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(close,1,0);
        var nav=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};nav.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(var (name,index) in new[]{("Navegar",0),("Organizar",1),("Mais",2)}){nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));var b=new ChoiceButton{Text=name,Dock=DockStyle.Fill,AccessibleName="Menu: "+name};b.Click+=(_,_)=>SelectGroup(index);categories.Add(b);nav.Controls.Add(b,index,0);}
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));viewport.Controls.Add(rows);
        var zoomBar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1};foreach(int w in new[]{0,38,58,38,38})zoomBar.ColumnStyles.Add(new ColumnStyle(w==0?SizeType.Percent:SizeType.Absolute,w==0?100:w));zoomBar.RowStyles.Add(new RowStyle(SizeType.Percent,100));zoomBar.Controls.Add(new Label{Text="Zoom da página",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);zoomBar.Controls.Add(zoomOut,1,0);zoomBar.Controls.Add(zoom,2,0);zoomBar.Controls.Add(zoomIn,3,0);zoomBar.Controls.Add(full,4,0);zoomOut.Click+=(_,_)=>invoke("zoom-out");zoomIn.Click+=(_,_)=>invoke("zoom-in");full.Click+=(_,_)=>invoke("fullscreen");
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(var (text,id,col,glyph) in new[]{("Aparência","appearance",0,"settings"),("Atualizar","update",1,"update")}){var b=new SoftButton{Text=text,Glyph=glyph,Dock=DockStyle.Fill,AccessibleName=text};b.Click+=(_,_)=>invoke(id);footer.Controls.Add(b,col,0);}
        body.Controls.Add(header,0,0);body.Controls.Add(nav,0,1);body.Controls.Add(viewport,0,2);body.Controls.Add(zoomBar,0,3);body.Controls.Add(footer,0,4);Controls.Add(body);SelectGroup(0);
    }
    internal void SelectGroup(int index)
    {
        group=index;rows.SuspendLayout();foreach(Control c in rows.Controls.Cast<Control>().ToArray())c.Dispose();rows.Controls.Clear();rows.RowStyles.Clear();rows.RowCount=0;
        foreach(var c in commands.Where(c=>c.Group==group))
        {int row=rows.RowCount++;rows.RowStyles.Add(new RowStyle(SizeType.Absolute,42));var b=new SoftButton{Text=c.Title+(c.Hint.Length>0?"  · "+c.Hint:""),Glyph=c.Glyph,Dock=DockStyle.Fill,AccessibleName=c.Id,Margin=new Padding(0,2,0,2)};b.Click+=(_,_)=>invoke(c.Id);rows.Controls.Add(b,0,row);}
        for(int i=0;i<categories.Count;i++){categories[i].Selected=i==group;categories[i].Invalidate();}rows.ResumeLayout(true);SetPalette(palette);
    }
    internal void FocusMenu()=>categories[group].Focus();
    internal void RefreshZoom(double factor){zoom.Text=$"{factor*100:0}%";}
    internal void ClickCommand(string id)
    {
        if(id=="zoom-in"){zoomIn.PerformClick();return;}if(id=="zoom-out"){zoomOut.PerformClick();return;}if(id=="fullscreen"){full.PerformClick();return;}
        var c=commands.First(c=>c.Id==id);SelectGroup(c.Group);var b=rows.Controls.OfType<SoftButton>().First(b=>b.AccessibleName==id);viewport.ScrollControlIntoView(b);b.PerformClick();
    }
    public void SetPalette(Palette p)
    {
        palette=p;void Theme(Control c){c.BackColor=p.Surface;c.ForeColor=p.Text;if(c is IThemed t&&c!=this)t.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}Theme(this);Invalidate();
    }
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);if(Width<2||Height<2)return;using var shape=Shapes.Round(new Rectangle(0,0,Width,Height),16*DeviceDpi/96f);var previous=Region;Region=new Region(shape);previous?.Dispose();
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using var p=new Pen(palette.Line);using var shape=Shapes.Round(new Rectangle(0,0,Width-1,Height-1),16*DeviceDpi/96f);e.Graphics.DrawPath(p,shape);}
}

public sealed partial class BrowserWindow
{
    readonly List<string> menuTrace=[];
    BrowserMenuPanel? browserMenu;Form? menuPopup;bool menuOpen;readonly Tween menuMotion=new();MenuDismissFilter? menuFilter;string? lastMenuCommand;
    int menuSession;
    bool menuPageBlocked,menuRestorePageFocus;
    readonly System.Windows.Forms.Timer menuInputRelease=new(){Interval=16};
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
    bool MenuVisible=>menuPopup is {IsDisposed:false,Visible:true};
    void RestoreMenuPageInput()
    {
        if(!menuPageBlocked)return;
        if(closing||IsDisposed||MenuVisible){menuInputRelease.Stop();return;}
        if(((GetAsyncKeyState(1)|GetAsyncKeyState(2)|GetAsyncKeyState(4))&0x8000)!=0){menuInputRelease.Start();return;}
        menuInputRelease.Stop();menuFilter?.ResetGesture();menuPageBlocked=false;tabStrip.Enabled=true;
        bool focus=menuRestorePageFocus;menuRestorePageFocus=false;
        if(focus&&Form.ActiveForm==this&&Current?.View is {IsDisposed:false} view)view.Focus();
    }
    void RememberMenuDismissalTarget()=>menuRestorePageFocus=tabStrip.RectangleToScreen(tabStrip.ClientRectangle).Contains(Cursor.Position);
    BrowserCommand[] MenuCommands()=>[
        new("new-tab","Nova aba","Ctrl+T","plus",0,()=>_ = AddTab("kay://new")),new("ai","Clean AI","Ctrl+Shift+I","spark",0,ToggleAssistant),new("history","Histórico","Ctrl+H","history",0,ShowHistory),new("bookmarks","Favoritos","","star",0,ShowBookmarks),new("downloads","Downloads","Ctrl+J","download",0,ShowDownloads),new("reading","Lista de leitura","","code",0,()=>ShowLibrary("reading")),
        new("workspaces","Espaços de trabalho","","globe",1,ManageWorkspaces),new("sources","Fontes salvas","","globe",1,()=>ShowLibrary("source")),new("notes","Documentos e notas","","code",1,()=>ShowLibrary("note")),new("memory","Memória e abas","","memory",1,ShowMemory),new("extensions","Extensões","","extension",1,()=>_ = ShowExtensions()),
        new("appearance","Personalizar aparência","","settings",2,ShowAppearance),new("focus","Modo foco","F8","leaf",2,ToggleFocus),new("settings","Configurações avançadas","","settings",2,ShowSettings),new("google","Conta Google","","user",2,()=>_ = LoginGoogle()),new("find","Buscar nesta página","Ctrl+F","search",2,Find),new("update","Verificar atualizações","","update",2,()=>_ = CheckUpdate()),new("about","Sobre o Clean Browser","","globe",2,()=>ShowNotice($"Clean Browser {Updater.CurrentVersion.ToString(3)}\nC# · .NET 10 · Microsoft Edge WebView2\n\nExtensões por pasta. A meta de RAM é flexível. Sem Chrome Sync.","Clean Browser")),
        new("zoom-out","Zoom −","","search",-1,()=>{if(Current?.View is {} v)v.ZoomFactor=Math.Max(.3,v.ZoomFactor-.1);}),new("zoom-in","Zoom +","","search",-1,()=>{if(Current?.View is {} v)v.ZoomFactor=Math.Min(3,v.ZoomFactor+.1);}),new("fullscreen","Tela cheia","F11","max",-1,Fullscreen)
    ];
    void ShowMenu()
    {
        if(menuOpen){CloseBrowserMenu();return;}
        if(browserMenu==null)
        {
            browserMenu=new(MenuCommands(),DispatchMenuCommand){Font=Font};browserMenu.CloseRequested+=()=>CloseBrowserMenu();
            menuPopup=new Form{Text="Menu do Clean Browser",FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,ControlBox=false,StartPosition=FormStartPosition.Manual,KeyPreview=true,AutoScaleMode=AutoScaleMode.Dpi};
            menuPopup.Controls.Add(browserMenu);browserMenu.Dock=DockStyle.Fill;
            // WM_ACTIVATE is sent before the outside mouse-down reaches the queue.
            // Keep the popup alive until activation finishes, then hide it on our UI queue.
            menuPopup.Deactivate+=(_,_)=>QueueBrowserMenuDismissal();
            menuPopup.FormClosing+=(_,e)=>{if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;CloseBrowserMenu(false);}};
            Resize+=(_,_)=>{if(menuOpen){menuMotion.Stop();PositionBrowserMenu();}};
            menuFilter=new MenuDismissFilter(this);Application.AddMessageFilter(menuFilter);
            menuInputRelease.Tick+=(_,_)=>RestoreMenuPageInput();
            FormClosed+=(_,_)=>{CloseBrowserMenu(false);Application.RemoveMessageFilter(menuFilter);menuMotion.Dispose();menuInputRelease.Dispose();menuPopup.Dispose();};
        }
        menuSession++;menuOpen=true;
        // WebView2 handles mouse input in another process, outside IMessageFilter.
        // Disable its native parent while the menu is visible so Windows routes the
        // dismissal gesture to this UI thread instead of the page underneath.
        menuInputRelease.Stop();menuRestorePageFocus=false;menuPageBlocked=true;tabStrip.Enabled=false;
        browserMenu.SetPalette(Palette.Create(store.Settings.Theme=="Claro",Accent));browserMenu.RefreshZoom(Current?.View?.ZoomFactor??1);PositionBrowserMenu();browserMenu.Visible=true;
        var target=MenuLocation();if(!menuPopup!.Visible)menuPopup.Show(this);else menuPopup.Activate();browserMenu.FocusMenu();
        menuMotion.Start(target.X+Ui(18),target.X,x=>{if(!menuPopup.IsDisposed)menuPopup.Left=(int)x;},230);
    }
    Point MenuLocation()=>PointToScreen(new Point(ClientSize.Width-Ui(366)-Ui(12),fullscreen?Ui(10):navigationRow!.Bottom+Ui(8)));
    void PositionBrowserMenu()
    {
        if(browserMenu==null||menuPopup==null)return;var point=MenuLocation();int height=Math.Min(Ui(502),Math.Max(Ui(220),ClientSize.Height-PointToClient(point).Y-Ui(12)));var size=new Size(Ui(366),height);
        menuPopup.ClientSize=size;browserMenu.Size=size;menuPopup.Location=point;menuPopup.BackColor=browserMenu.BackColor;
        using var shape=Shapes.Round(new Rectangle(Point.Empty,size),Ui(16));var previous=menuPopup.Region;menuPopup.Region=new Region(shape);previous?.Dispose();
    }
    void CloseBrowserMenu(bool animate=true)
    {
        menuSession++;menuOpen=false;
        if(!MenuVisible){RestoreMenuPageInput();return;}
        if(!animate){menuMotion.Stop();menuPopup!.Hide();browserMenu!.Visible=false;RestoreMenuPageInput();return;}
        menuMotion.Start(menuPopup!.Left,menuPopup.Left+Ui(18),x=>{if(!menuPopup.IsDisposed)menuPopup.Left=(int)x;},180,()=>{if(!menuOpen&&!menuPopup.IsDisposed){menuPopup.Hide();browserMenu!.Visible=false;RestoreMenuPageInput();}});
    }
    void QueueBrowserMenuDismissal()
    {
        if(!MenuVisible||closing||IsDisposed||!IsHandleCreated)return;
        int session=menuSession;
        BeginInvoke(()=>{if(!closing&&!IsDisposed&&session==menuSession)CloseBrowserMenu(false);});
    }
    void DispatchMenuCommand(string id)
    {
        if(!menuOpen)return;
        lastMenuCommand=id;
        if(id is "zoom-out" or "zoom-in"){MenuCommands().First(c=>c.Id==id).Run();browserMenu!.RefreshZoom(Current?.View?.ZoomFactor??1);return;}
        CloseBrowserMenu(false);
        BeginInvoke(()=>{if(!closing&&!IsDisposed)MenuCommands().First(c=>c.Id==id).Run();});
    }
    sealed class MenuDismissFilter(BrowserWindow owner):IMessageFilter
    {
        int suppressedRelease;
        internal void ResetGesture()=>suppressedRelease=0;
        internal void SuppressGesture(int down)=>suppressedRelease=down switch
        {
            0x201 or 0x203 or 0xA1 or 0xA3=>0x202,
            0x204 or 0x206 or 0xA4 or 0xA6=>0x205,
            0x207 or 0x209 or 0xA7 or 0xA9=>0x208,
            _=>0
        };
        public bool PreFilterMessage(ref Message m)
        {
            int release=m.Msg switch{0xA2=>0x202,0xA5=>0x205,0xA8=>0x208,_=>m.Msg};
            if(suppressedRelease!=0&&release==suppressedRelease){suppressedRelease=0;return true;}
            if(m.Msg is 0x201 or 0x204 or 0x207 or 0xA1 or 0xA4 or 0xA7)suppressedRelease=0;
            if(!owner.MenuVisible)return false;
            if(m.Msg==0x100&&(Keys)m.WParam==Keys.Escape){owner.CloseBrowserMenu(false);return true;}
            if(m.Msg is not(0x201 or 0x203 or 0x204 or 0x206 or 0x207 or 0x209 or 0xA1 or 0xA3 or 0xA4 or 0xA6 or 0xA7 or 0xA9))return false;
            if(owner.browserMenu?.RectangleToScreen(owner.browserMenu.ClientRectangle).Contains(Cursor.Position)==true)return false;
            SuppressGesture(m.Msg);
            owner.RememberMenuDismissalTarget();
            owner.CloseBrowserMenu(false);return true;
        }
    }

}
