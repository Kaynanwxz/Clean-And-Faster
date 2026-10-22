using System.Runtime.InteropServices;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    readonly TableLayoutPanel chromeLayout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty, Padding = Padding.Empty };
    readonly ToolTip chromeTips = new() { ShowAlways = true };
    bool fullscreen;
    TableLayoutPanel? titleRow, windowButtons, navigationRow;
    RoundedPanel? locationBox;
    SoftButton? connectionIcon;
    void InitializeChrome()
    {
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Padding = new Padding(1);
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
        chromeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        chromeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        chromeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,64));
        chromeLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        chromeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
        var title = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
        titleRow = title; title.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        title.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));title.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138));
        title.Controls.Add(tabStrip.Header,0,0);tabStrip.Header.Margin=Padding.Empty;
        var windows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(0,4,4,4) };
        windowButtons = windows; windows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for(int i=0;i<3;i++)windows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));
        SoftButton IconButton(string glyph,string tip,Action action)
        {
            var b = new SoftButton { Glyph = glyph, AccessibleName = tip, Dock = DockStyle.Fill, Margin = new Padding(2), TabStop = false };
            b.SetPalette(Colors); b.Click += (_,_) => action(); chromeTips.SetToolTip(b,tip); return b;
        }
        windows.Controls.Add(IconButton("min","Minimizar",()=>WindowState=FormWindowState.Minimized),0,0);
        var maximize=IconButton("max","Maximizar / restaurar",()=>WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized);
        windows.Controls.Add(maximize,1,0);
        var close=IconButton("close","Fechar navegador",Close);close.Danger=true;windows.Controls.Add(close,2,0);
        title.Controls.Add(windows,1,0);
        Resize+=(_,_)=>{maximize.Glyph=WindowState==FormWindowState.Maximized?"restore":"max";maximize.Invalidate();};
        var bar=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=11, RowCount=1, Margin=Padding.Empty, Padding=new Padding(12,8,12,10), Tag="surface" };
        navigationRow = bar; bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for(int i=0;i<11;i++)bar.ColumnStyles.Add(new ColumnStyle(i==4?SizeType.Percent:SizeType.Absolute,i==4?100:42));
        void Add(string glyph,int col,string tip,Action action)=>bar.Controls.Add(IconButton(glyph,tip,action),col,0);
        Add("back",0,"Voltar · Alt+←",()=>{if(Current?.View?.CanGoBack==true)Current.View.GoBack();});
        Add("forward",1,"Avançar · Alt+→",()=>{if(Current?.View?.CanGoForward==true)Current.View.GoForward();});
        Add("reload",2,"Recarregar · Ctrl+R",()=>Current?.View?.Reload());
        Add("home",3,"Página inicial",()=>NavigateCurrent("kay://new"));
        var addressBox=new RoundedPanel { Dock=DockStyle.Fill, Radius=13, Margin=new Padding(10,0,12,0), Padding=new Padding(43,13,14,8) };
        locationBox = addressBox;
        addressBox.SetPalette(Colors);
        address.PlaceholderText="Pesquisar ou inserir endereço";address.Font=new Font("Segoe UI",11);address.Margin=Padding.Empty;address.Tag="surface";
        var shield=new SoftButton { Glyph="globe",Size=new Size(28,28),Location=new Point(8,9),TabStop=false,AccessibleName="Informações da conexão" };
        connectionIcon = shield;
        shield.SetPalette(Colors);shield.Click+=(_,_)=>
        {
            string url=Current?.Url??"kay://new";
            ShowNotice(url.StartsWith("https://",StringComparison.OrdinalIgnoreCase)?"Conexão HTTPS. Confira o domínio na barra antes de informar seus dados.\n\n"+url:url.StartsWith("http://",StringComparison.OrdinalIgnoreCase)?"Conexão HTTP sem criptografia.\n\n"+url:"Página local do Clean Browser.","Conexão");
        };
        addressBox.Controls.Add(address);addressBox.Controls.Add(shield);shield.BringToFront();bar.Controls.Add(addressBox,4,0);
        Add("star",5,"Salvar ou remover favorito · Ctrl+D",BookmarkCurrent);
        Add("memory",6,"Controle de memória",ShowMemory);
        Add("download",7,"Downloads · Ctrl+J",ShowDownloads);
        Add("menu",8,"Menu do Clean Browser",ShowMenu);
        Add("spark",10,"Assistente IA · Ctrl+Shift+I",ToggleAssistant);
        bar.ColumnStyles[9].Width = 112;
        var updateButton = IconButton("update", "Atualizar o navegador", () => _ = CheckUpdate());
        updateButton.Text = "Atualizar"; updateButton.TabStop = true;
        bar.Controls.Add(updateButton, 9, 0);
        tabStrip.Margin=Padding.Empty;
        status.Dock=DockStyle.Fill;status.Margin=Padding.Empty;status.Font=new Font("Segoe UI",8.5f);status.Text="Clean Browser 0.6.2   ·   Pronto para explorar";
        chromeLayout.Controls.Add(title,0,0);chromeLayout.Controls.Add(bar,0,1);InitializeAssistantHost();
        chromeLayout.Controls.Add(pageHost,0,2);chromeLayout.Controls.Add(status,0,3);
        Controls.Add(chromeLayout);
        Shown += (_, _) => ApplyChromeMetrics();
        ResizeEnd += (_, _) => ApplyChromeMetrics();
    }
    int Ui(int value) => (int)Math.Round(value * DeviceDpi / 96f);
    void ApplyChromeMetrics()
    {
        if (IsDisposed || titleRow == null || navigationRow == null || windowButtons == null || locationBox == null || connectionIcon == null) return;
        SuspendLayout(); chromeLayout.SuspendLayout();
        try
        {
            chromeLayout.RowStyles[0].Height = fullscreen ? 0 : Ui(48);
            chromeLayout.RowStyles[1].Height = fullscreen ? 0 : Ui(62);
            chromeLayout.RowStyles[3].Height = fullscreen ? 0 : Ui(26);
            titleRow.ColumnStyles[1].Width = Ui(138);
            windowButtons.Padding = new Padding(0, Ui(4), Ui(4), Ui(4));
            navigationRow.Padding = new Padding(Ui(10), Ui(8), Ui(10), Ui(8));
            for (int i = 0; i < navigationRow.ColumnCount; i++)
                if (i != 4) navigationRow.ColumnStyles[i].Width = Ui(i == 9 ? 124 : 36);
            foreach (Control control in navigationRow.Controls) control.Margin = new Padding(Ui(2));
            locationBox.Margin = new Padding(Ui(8), Ui(2), Ui(8), Ui(2));
            locationBox.Padding = new Padding(Ui(43), 0, Ui(12), 0);
            address.Dock = DockStyle.None;
            void PositionAddress()
            {
                int h = address.PreferredHeight;
                address.SetBounds(Ui(42), Math.Max(Ui(4), (locationBox.ClientSize.Height - h) / 2), Math.Max(Ui(80), locationBox.ClientSize.Width - Ui(56)), h);
                connectionIcon.SetBounds(Ui(7), Math.Max(0, (locationBox.ClientSize.Height - Ui(28)) / 2), Ui(28), Ui(28));
            }
            positionAddress = PositionAddress;
            locationBox.Resize -= OnLocationResize; locationBox.Resize += OnLocationResize;
            PositionAddress();
            MinimumSize = new Size(Ui(900), Ui(600));
            tabStrip.Header.EnsureSelectedVisible(); tabStrip.Header.Invalidate();
        }
        finally { chromeLayout.ResumeLayout(true); ResumeLayout(true); }
    }
    Action? positionAddress;
    void OnLocationResize(object? sender, EventArgs e) => positionAddress?.Invoke();
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (IsHandleCreated) BeginInvoke(ApplyChromeMetrics);
    }
    void SetNativeTheme()
    {
        if(!IsHandleCreated)return;
        int dark=store.Settings.Theme=="Claro"?0:1,round=2;
        try { NativeFrame.DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));NativeFrame.DwmSetWindowAttribute(Handle,33,ref round,sizeof(int)); } catch(DllNotFoundException) { }
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SetNativeTheme();}
    protected override CreateParams CreateParams
    {
        get { var cp=base.CreateParams;cp.Style|=0x000F0000;return cp; }
    }
    protected override void WndProc(ref Message m)
    {
        const int WM_NCCALCSIZE=0x83,WM_NCHITTEST=0x84;
        if(m.Msg==WM_NCCALCSIZE&&m.WParam!=IntPtr.Zero){m.Result=IntPtr.Zero;return;}
        if(m.Msg==WM_NCHITTEST&&WindowState!=FormWindowState.Maximized&&!fullscreen)
        {
            long xy=m.LParam.ToInt64();var point=PointToClient(new Point(unchecked((short)(xy&0xffff)),unchecked((short)((xy>>16)&0xffff))));
            int grip=Math.Max(6,DeviceDpi*6/96);bool left=point.X<grip,right=point.X>=Width-grip,top=point.Y<grip,bottom=point.Y>=Height-grip;
            int hit=top&&left?13:top&&right?14:bottom&&left?16:bottom&&right?17:left?10:right?11:top?12:bottom?15:0;
            if(hit!=0){m.Result=new IntPtr(hit);return;}
        }
        // Let Windows update activation without painting its own caption over our chrome.
        if (m.Msg == 0x86) // WM_NCACTIVATE
        {
            m.LParam = new IntPtr(-1); DefWndProc(ref m); Invalidate(); return;
        }
        if (m.Msg is 0xAE or 0xAF) { m.Result = IntPtr.Zero; return; }
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var line=new Pen(Colors.Line);e.Graphics.DrawRectangle(line,0,0,Width-1,Height-1);
    }
}

public sealed class KayMenuRenderer(Palette colors):ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)=>e.Graphics.Clear(colors.Surface);
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if(!e.Item.Selected)return;
        e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path=Shapes.Round(new Rectangle(2,1,e.Item.Width-4,e.Item.Height-2),7);using var brush=new SolidBrush(colors.Elevated);e.Graphics.FillPath(brush,path);
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=colors.Text;base.OnRenderItemText(e);}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using var pen=new Pen(colors.Line);e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
}
