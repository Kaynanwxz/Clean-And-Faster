using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Net;

namespace KayBrowser;

public sealed class BrowserTab
{
    public string Url = "kay://new";
    public string Title = "Nova aba";
    public DateTime LastActive = DateTime.UtcNow;
    public bool Suspended;
    public bool Pinned;
    public WebView2? View;
    public Task? Loading;
    public BrowserPage Page = new() { Caption = "Nova aba" };
}

public sealed partial class BrowserWindow : Form
{
    readonly Storage store = new();
    readonly List<BrowserTab> tabs = [];
    readonly List<DownloadRecord> downloads = [];
    readonly BrowserTabs tabStrip = new() { Dock = DockStyle.Fill };
    readonly TextBox address = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12), Margin = new Padding(12) };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 27, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
    readonly System.Windows.Forms.Timer memoryTimer = new() { Interval = 15000 };
    CoreWebView2Environment? environment;
    bool closing, memoryBusy;
    BrowserTab? Current => tabs.FirstOrDefault(x => x.Page == tabStrip.SelectedTab);
    Color Back => store.Settings.Theme == "Claro" ? Color.FromArgb(244, 247, 249) : Color.FromArgb(15, 20, 25);
    Color Ink => store.Settings.Theme == "Claro" ? Color.FromArgb(28, 34, 42) : Color.FromArgb(228, 234, 240);
    Color Accent { get { try { return ColorTranslator.FromHtml(store.Settings.Accent); } catch { return Color.MediumAquamarine; } } }

    public BrowserWindow(bool diagnostics = false)
    {
        Text = "Clean Browser"; Width = 1320; Height = 860; MinimumSize = new Size(900, 600);
        if (Environment.ProcessPath is { } exe) Icon = Icon.ExtractAssociatedIcon(exe);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10); KeyPreview = true;
        DoubleBuffered = true;
        InitializeChrome();
        address.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; NavigateCurrent(address.Text); } };
        tabStrip.SelectedIndexChanged += (_, _) => { if (Current is { } t) _ = Activate(t); };
        tabStrip.NewRequested += () => _ = AddTab("kay://new");
        tabStrip.CloseRequested += page => CloseTab(tabs.FirstOrDefault(t => t.Page == page));
        KeyDown += Shortcut;
        if (!diagnostics) Shown += async (_, _) => await Start();
        FormClosing += (_, _) => { closing = true; memoryTimer.Stop(); DisposeAssistant(); SaveSession(); foreach (var t in tabs) t.View?.Dispose(); };
        memoryTimer.Tick += async (_, _) => await ManageMemory();
        Motion.Enabled = store.Settings.Animations;
        ApplyTheme(this);
    }

    Palette Colors => Palette.Create(store.Settings.Theme == "Claro", Accent);
    public Button Button(string text, Action action)
    {
        var b = new SoftButton { Text = text, AutoSize = true, Padding = new Padding(12, 7, 12, 7), AccessibleName = text };
        b.SetPalette(Colors); b.Click += (_, _) => action(); return b;
    }
    void ApplyTheme(Control c)
    {
        if (c is AssistantPanel ai) { ai.SetPalette(Colors); return; }
        if (c is HomeBoard home) { home.SetPalette(Colors); return; }
        if (c is IThemed themed) themed.SetPalette(Colors);
        else { c.BackColor = c.Tag as string == "surface" ? Colors.Surface : Colors.Background; c.ForeColor = Colors.Text; }
        foreach (Control child in c.Controls) if (child is not WebView2) ApplyTheme(child);
        if (c == this)
        {
            address.BackColor = Colors.Surface; address.ForeColor = Colors.Text;
            if (address.Parent is { } box) box.BackColor = Colors.Surface;
            status.ForeColor = Colors.Muted;
            SetNativeTheme();
        }
    }
    async Task Start()
    {
        try
        {
            environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Storage.Root, "WebView"), new CoreWebView2EnvironmentOptions { AreBrowserExtensionsEnabled = true });
            var session = store.Settings.RestoreTabs ? store.Settings.Session.Where(Navigation.IsWebUrl).Take(30).ToList() : [];
            if (session.Count == 0) await AddTab("kay://new");
            else { foreach (var url in session) await AddTab(url, tabs.Count == 0); }
            memoryTimer.Start();
        }
        catch (Exception ex)
        {
            ShowNotice("Não foi possível iniciar o motor WebView2. Instale ou atualize o Microsoft Edge WebView2 Runtime.\n\n" + ex.Message, "Clean Browser");
            Process.Start(new ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });
        }
    }
    async Task AddTab(string url, bool activate = true)
    {
        if (environment == null || closing) return;
        var t = new BrowserTab { Url = Navigation.Resolve(url, store.Settings.Search) };
        tabs.Add(t); tabStrip.TabPages.Add(t.Page);
        if (activate) { tabStrip.SelectedTab = t.Page; await Activate(t); }
        else { t.Page.Caption = "○ " + new Uri(url).Host; }
        SaveSession();
    }
    async Task Activate(BrowserTab t)
    {
        t.LastActive = DateTime.UtcNow;
        if (t.Url == "kay://new" && t.View == null) { ShowHome(t); address.Text = ""; return; }
        if (t.View == null || t.Loading is { IsCompleted: false }) await CreateView(t);
        else { t.View.Visible = true; t.View.CoreWebView2?.Resume(); t.Suspended = false; }
        UpdateChrome(t);
    }
    Task CreateView(BrowserTab t, bool navigate = true)
    {
        if(t.Loading is { IsCompleted: false })return t.Loading;
        return t.Loading=CreateViewCore(t,navigate);
    }
    async Task CreateViewCore(BrowserTab t, bool navigate)
    {
        var view = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Back };
        t.View = view; t.Page.Controls.Clear(); t.Page.Controls.Add(view);
        try
        {
            await view.EnsureCoreWebView2Async(environment);
            if (closing || !tabs.Contains(t)) return;
            var core = view.CoreWebView2;
            view.KeyDown += Shortcut;
            core.Settings.AreDevToolsEnabled = true;
            core.Profile.PreferredColorScheme = store.Settings.Theme == "Claro" ? CoreWebView2PreferredColorScheme.Light : CoreWebView2PreferredColorScheme.Dark;
            core.NavigationStarting += (_, e) =>
            {
                if (t == snapshotTab) ResetSnapshot();
                if (!Navigation.IsWebUrl(e.Uri) && e.Uri != "about:blank") { e.Cancel = true; ShowNotice("Link externo bloqueado: " + e.Uri[..Math.Min(e.Uri.Length, 180)], "Clean Browser"); return; }
                status.Text = "Carregando… " + new Uri(e.Uri).Host;
            };
            core.SourceChanged += (_, _) => { if (Navigation.IsWebUrl(core.Source)) t.Url = core.Source; UpdateChrome(t); };
            core.DocumentTitleChanged += (_, _) => { if (!string.IsNullOrWhiteSpace(core.DocumentTitle)) t.Title = core.DocumentTitle; UpdateChrome(t); };
            core.NavigationCompleted += (_, e) =>
            {
                if (e.IsSuccess) { store.Record(t.Title, t.Url); SaveSession(); }
                else status.Text = "Falha de navegação: " + e.WebErrorStatus;
                UpdateChrome(t);
            };
            core.NewWindowRequested += async (_, e) =>
            {
                e.Handled = true;
                using var deferral = e.GetDeferral();
                if (!Navigation.IsWebUrl(e.Uri) && e.Uri != "about:blank") return;
                await AddTab("kay://new");
                if (Current is { } child)
                {
                    if (child.View == null) await CreateView(child, false);
                    child.Url = e.Uri;
                    e.NewWindow = child.View!.CoreWebView2;
                }
            };
            core.PermissionRequested += (_, e) =>
            {
                e.State = ShowNotice($"{e.Uri}\n\nPermitir {e.PermissionKind}?", "Permissão do site", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
                e.SavesInProfile = false;
            };
            core.DownloadStarting += (_, e) =>
            {
                e.Handled = true;
                using var dlg = new SaveFileDialog { FileName = Path.GetFileName(e.ResultFilePath), InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") };
                if (dlg.ShowDialog(this) != DialogResult.OK) { e.Cancel = true; return; }
                e.ResultFilePath = dlg.FileName;
                var op = e.DownloadOperation;
                var record = new DownloadRecord(Path.GetFileName(dlg.FileName), dlg.FileName, "Em andamento"); downloads.Add(record);
                op.StateChanged += (_, _) => { int idx = downloads.IndexOf(record); if (idx >= 0) { var updated = record with { State = op.State.ToString() }; downloads[idx] = updated; record = updated; } };
                status.Text = "Download: " + record.Name;
            };
            core.ProcessFailed += (_, e) => { status.Text = "Motor interrompido: " + e.ProcessFailedKind + ". Reabra esta aba pelo menu de memória."; };
            if (navigate)
            {
                if (Navigation.IsWebUrl(t.Url)) core.Navigate(t.Url);
                else core.Navigate("about:blank");
            }
        }
        catch (Exception ex) { view.Dispose(); t.View = null; status.Text = "Falha ao criar aba: " + ex.Message; }
    }
    void UpdateChrome(BrowserTab t)
    {
        if (closing || !tabs.Contains(t)) return;
        tabStrip.Header.Invalidate();
        t.Page.Caption = (t.Pinned ? "◆ " : t.Suspended ? "◌ " : "") + (t.Title.Length > 24 ? t.Title[..24] + "…" : t.Title);
        if (Current != t) return;
        address.Text = t.Url == "kay://new" ? "" : t.Url;
        Text = t.Title + " · Clean Browser";
    }
    void NavigateCurrent(string input)
    {
        if (Current is not { } t) return;
        var url = Navigation.Resolve(input, store.Settings.Search);
        if (url == "kay://new") { t.View?.Dispose(); t.View = null; t.Url = url; ShowHome(t); SaveSession(); return; }
        t.Url = url;
        if (t.View == null) _ = CreateView(t); else if (t.View.CoreWebView2 is { } core) { core.Resume(); core.Navigate(url); }
    }
    void CloseTab(BrowserTab? t)
    {
        if (t == null) return;
        tabs.Remove(t); t.View?.Dispose(); tabStrip.TabPages.Remove(t.Page); t.Page.Dispose();
        if (tabs.Count == 0) _ = AddTab("kay://new");
        SaveSession();
    }
    void SaveSession()
    {
        store.Settings.Session = tabs.Select(x => x.Url).Where(Navigation.IsWebUrl).ToList(); store.SaveSettings();
    }
    void ShowHome(BrowserTab t)
    {
        t.Title = "Nova aba";
        foreach (Control control in t.Page.Controls.Cast<Control>().ToArray()) control.Dispose();
        t.Page.Controls.Clear();
        var home = new HomeBoard(store.Settings.Search, store.Bookmarks, NavigateCurrent, ShowSettings, ShowMemory);
        home.SetPalette(Colors); t.Page.Controls.Add(home); UpdateChrome(t);
    }
    void BookmarkCurrent()
    {
        if (Current is not { } t || !Navigation.IsWebUrl(t.Url)) return;
        var old = store.Bookmarks.FirstOrDefault(x => x.Url == t.Url);
        if (old != null) { store.Bookmarks.Remove(old); status.Text = "Favorito removido"; }
        else { store.Bookmarks.Add(new(t.Title, t.Url)); status.Text = "Favorito salvo"; }
        store.SaveBookmarks();
    }
    void ShowMenu()
    {
        var menu = new ContextMenuStrip { BackColor = Colors.Surface, ForeColor = Colors.Text, ShowImageMargin = true, Padding = new Padding(6), Font = new Font("Segoe UI", 10), Renderer = new KayMenuRenderer(Colors) };
        void Item(string s, Action a)
        {
            var icon = s.StartsWith("Histórico") ? "history" : s.StartsWith("Favoritos") ? "star" : s.StartsWith("Downloads") ? "download" : s.StartsWith("Memória") ? "memory" : s.StartsWith("Extensões") ? "extension" : s.StartsWith("Personalizar") ? "settings" : s.StartsWith("Conta") ? "user" : s.StartsWith("Buscar") ? "search" : s.StartsWith("Verificar") ? "update" : s.StartsWith("Tela") ? "max" : s.StartsWith("Zoom") ? "search" : "globe";
            var item = menu.Items.Add(s, Glyphs.Bitmap(icon, Colors.Accent), (_, _) => a()); item.Padding = new Padding(9, 7, 9, 7);
        }
        menu.Closed += (_, _) => { foreach (ToolStripItem item in menu.Items) item.Image?.Dispose(); menu.Dispose(); };
        Item("Assistente IA · Ctrl+Shift+I", ToggleAssistant); Item("Histórico  ·  Ctrl+H", ShowHistory); Item("Favoritos", ShowBookmarks); Item("Downloads", ShowDownloads);
        Item("Memória e abas", ShowMemory); Item("Extensões", () => _ = ShowExtensions()); Item("Personalizar", ShowSettings);
        Item("Conta Google", () => _ = LoginGoogle()); Item("Buscar na página  ·  Ctrl+F", Find);
        Item("Zoom +", () => { if (Current?.View is { } v) v.ZoomFactor = Math.Min(3, v.ZoomFactor + .1); });
        Item("Zoom −", () => { if (Current?.View is { } v) v.ZoomFactor = Math.Max(.3, v.ZoomFactor - .1); });
        Item("Tela cheia  ·  F11", Fullscreen); Item("Verificar atualizações", () => _ = CheckUpdate());
        Item("Sobre o Clean Browser", () => ShowNotice("Clean Browser 0.6.4 · prévia para Windows x64\nC# / .NET 10 / Microsoft Edge WebView2\n\nExtensões por pasta. Sem Chrome Sync.\nO limite de RAM é uma meta, não um limite rígido.", "Clean Browser"));
        menu.Show(Cursor.Position);
    }
    AppDialog Dialog(string title, int w = 820, int h = 570) => new(title, Colors, w, h);
    DialogResult ShowNotice(string text, string title = "Clean Browser", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None) => UiMessage.Show(this, Colors, text, title, buttons);
    ListView List(params string[] columns)
    {
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Back, ForeColor = Ink };
        foreach (var c in columns) list.Columns.Add(c, c == "Endereço" ? 440 : 220); return list;
    }
    void ShowHistory()
    {
        using var f = Dialog("Histórico"); var list = List("Página", "Endereço", "Data"); var filter = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Filtrar histórico…" };
        void Render() { list.Items.Clear(); foreach (var v in store.History.Where(v => (v.Title + v.Url).Contains(filter.Text, StringComparison.OrdinalIgnoreCase)).Take(1000)) list.Items.Add(new ListViewItem([v.Title, v.Url, v.At.ToString("g")]) { Tag = v.Url }); }
        filter.TextChanged += (_, _) => Render(); list.DoubleClick += (_, _) => { if (list.SelectedItems.Count > 0) { NavigateCurrent((string)list.SelectedItems[0].Tag!); f.Close(); } };
        var clear = Button("Apagar histórico", () => { if (ShowNotice("Apagar todo o histórico?", "Clean Browser", MessageBoxButtons.YesNo) == DialogResult.Yes) { store.History.Clear(); store.SaveHistory(); Render(); } }); clear.Dock = DockStyle.Bottom;
        f.Controls.Add(list); f.Controls.Add(filter); f.Controls.Add(clear); Render(); f.ShowDialog(this);
    }
    void ShowBookmarks()
    {
        using var f = Dialog("Favoritos"); var list = List("Página", "Endereço");
        void Render() { list.Items.Clear(); foreach (var b in store.Bookmarks) list.Items.Add(new ListViewItem([b.Title, b.Url]) { Tag = b }); }
        list.DoubleClick += (_, _) => { if (list.SelectedItems.Count > 0) { NavigateCurrent(((Bookmark)list.SelectedItems[0].Tag!).Url); f.Close(); } };
        var remove = Button("Remover selecionado", () => { if (list.SelectedItems.Count > 0) { store.Bookmarks.Remove((Bookmark)list.SelectedItems[0].Tag!); store.SaveBookmarks(); Render(); } }); remove.Dock = DockStyle.Bottom;
        f.Controls.Add(list); f.Controls.Add(remove); Render(); f.ShowDialog(this);
    }
    void ShowDownloads()
    {
        using var f = Dialog("Downloads desta sessão"); var list = List("Arquivo", "Local", "Estado");
        foreach (var d in downloads) list.Items.Add(new ListViewItem([d.Name, d.Path, d.State]) { Tag = d.Path });
        var open = Button("Mostrar na pasta", () => { if (list.SelectedItems.Count > 0) { var p = (string)list.SelectedItems[0].Tag!; if (File.Exists(p)) Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + p + "\"") { UseShellExecute = true }); } }); open.Dock = DockStyle.Bottom;
        f.Controls.Add(list); f.Controls.Add(open); f.ShowDialog(this);
    }
    void Find()
    {
        using var f = Dialog("Buscar nesta página", 460, 160); var text = new TextBox { Dock = DockStyle.Top };
        var find = Button("Buscar", () => { if (Current?.View?.CoreWebView2 is { } c && text.Text.Length > 0) _ = c.ExecuteScriptAsync("window.find(" + System.Text.Json.JsonSerializer.Serialize(text.Text) + ")"); }); find.Dock = DockStyle.Bottom;
        f.Controls.Add(text); f.Controls.Add(find); f.AcceptButton = find; f.ShowDialog(this);
    }
    void Fullscreen()
    {
        fullscreen = !fullscreen;
        ApplyChromeMetrics();
        MaximizedBounds = fullscreen ? Screen.FromControl(this).Bounds : Screen.FromControl(this).WorkingArea;
        WindowState = FormWindowState.Normal; WindowState = FormWindowState.Maximized;
    }
    void Shortcut(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.Shift && e.KeyCode == Keys.I) { ToggleAssistant(); e.SuppressKeyPress = true; return; }
        if (e.Control) switch (e.KeyCode)
        {
            case Keys.L: address.Focus(); address.SelectAll(); break;
            case Keys.T: _ = AddTab("kay://new"); break;
            case Keys.W: CloseTab(Current); break;
            case Keys.H: ShowHistory(); break;
            case Keys.J: ShowDownloads(); break;
            case Keys.D: BookmarkCurrent(); break;
            case Keys.F: Find(); break;
            case Keys.R: Current?.View?.Reload(); break;
            case Keys.Tab: if (tabStrip.TabCount > 0) tabStrip.SelectedIndex = (tabStrip.SelectedIndex + 1) % tabStrip.TabCount; break;
            default: return;
        }
        else if (e.KeyCode == Keys.F11) Fullscreen();
        else if (e.Alt && e.KeyCode == Keys.Left) { if (Current?.View?.CanGoBack == true) Current.View.GoBack(); }
        else if (e.Alt && e.KeyCode == Keys.Right) { if (Current?.View?.CanGoForward == true) Current.View.GoForward(); }
        else return;
        e.SuppressKeyPress = true;
    }
    long MemoryMb()
    {
        long bytes = Process.GetCurrentProcess().WorkingSet64;
        if (environment != null) try { foreach (var info in environment.GetProcessInfos()) try { using var p = Process.GetProcessById(info.ProcessId); bytes += p.WorkingSet64; } catch (ArgumentException) { } } catch (Exception) { }
        return bytes / 1024 / 1024;
    }
    async Task ManageMemory()
    {
        if (memoryBusy || closing) return;
        memoryBusy = true;
        try
        {
            var mb = MemoryMb();
            foreach (var t in tabs.Where(t => t != Current && !t.Pinned && t.View != null).OrderBy(t => t.LastActive).ToArray())
            {
                if (t.View?.CoreWebView2 is not { } core || core.IsDocumentPlayingAudio) continue;
                if (DateTime.UtcNow - t.LastActive < TimeSpan.FromMinutes(store.Settings.IdleMinutes) && mb < store.Settings.MemoryBudgetMb) continue;
                t.View.Visible = false;
                t.Suspended = await core.TrySuspendAsync();
                UpdateChrome(t);
            }
            status.Text = $"{MemoryMb():N0} MB em uso · meta {store.Settings.MemoryBudgetMb:N0} MB · {tabs.Count(t => t.Suspended)} abas suspensas · {tabs.Count} abas";
        }
        catch (Exception ex) { status.Text = "Memória: " + ex.Message; }
        finally { memoryBusy = false; }
    }
    void ShowMemory()
    {
        using var f = Dialog("Memória e abas"); var list = List("Aba", "Estado", "Último uso");
        var note = new Label { Dock = DockStyle.Top, Height = 64, Text = $"RAM total estimada: {MemoryMb()} MB · meta: {store.Settings.MemoryBudgetMb} MB\nAbas com áudio e abas protegidas não são suspensas automaticamente. Descarregar perde formulários não salvos." };
        void Render() { list.Items.Clear(); foreach (var t in tabs) list.Items.Add(new ListViewItem([t.Title, t.Pinned ? "Protegida" : t.View == null ? "Descarregada" : t.Suspended ? "Suspensa" : "Ativa", t.LastActive.ToLocalTime().ToString("t")]) { Tag = t }); }
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55 };
        actions.Controls.Add(Button("Proteger / desproteger", () => { if (list.SelectedItems.Count > 0) { var t = (BrowserTab)list.SelectedItems[0].Tag!; t.Pinned = !t.Pinned; UpdateChrome(t); Render(); } }));
        actions.Controls.Add(Button("Descarregar selecionada", () =>
        {
            if (list.SelectedItems.Count == 0) return;
            var t = (BrowserTab)list.SelectedItems[0].Tag!;
            if (t == Current || t.Pinned) { ShowNotice("Selecione uma aba em segundo plano e sem proteção."); return; }
            if (ShowNotice("A página será recarregada ao voltar. Descartar formulários e estado não salvos?", "Clean Browser", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            t.View?.Dispose(); t.View = null; t.Suspended = false; t.Page.Controls.Clear(); Render();
        }));
        f.Controls.Add(list); f.Controls.Add(note); f.Controls.Add(actions); Render(); f.ShowDialog(this);
    }
    async Task ShowExtensions()
    {
        if (Current == null) return;
        if (Current.View == null) await CreateView(Current);
        var profile = Current?.View?.CoreWebView2?.Profile;
        if (profile == null) return;
        using var f = Dialog("Extensões · por pasta"); var list = List("Nome", "ID", "Estado");
        var help = new Label { Dock = DockStyle.Top, Height = 64, Text = "Instale extensões descompactadas com manifest.json. Compatibilidade depende das APIs do WebView2.\nNão há instalação direta pela Chrome Web Store; extensões com botão/popup podem não funcionar." };
        async Task Render()
        {
            list.Items.Clear(); foreach (var x in await profile.GetBrowserExtensionsAsync()) list.Items.Add(new ListViewItem([x.Name, x.Id, x.IsEnabled ? "Ativa" : "Desativada"]) { Tag = x });
        }
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55 };
        var add = Button("Instalar pasta…", () => { }); add.Click += async (_, _) =>
        {
            using var picker = new FolderBrowserDialog { Description = "Selecione a pasta da extensão com manifest.json" };
            if (picker.ShowDialog(f) != DialogResult.OK) return;
            try
            {
                if (!File.Exists(Path.Combine(picker.SelectedPath, "manifest.json"))) throw new IOException("A pasta não contém manifest.json.");
                if (ShowNotice("Extensões podem acessar os sites visitados. Instalar esta extensão?", "Clean Browser", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                // Keep a durable app-owned copy: WebView2 references the installed folder.
                var target = Path.Combine(Storage.Root, "Extensions", Guid.NewGuid().ToString("N"));
                CopyExtension(picker.SelectedPath, target);
                await profile.AddBrowserExtensionAsync(target); await Render();
            }
            catch (Exception ex) { ShowNotice(ex.Message, "Extensão não instalada"); }
        };
        var toggle = Button("Ativar / desativar", () => { }); toggle.Click += async (_, _) => { try { if (list.SelectedItems.Count > 0) { var x = (CoreWebView2BrowserExtension)list.SelectedItems[0].Tag!; await x.EnableAsync(!x.IsEnabled); await Render(); } } catch (Exception ex) { ShowNotice(ex.Message); } };
        var remove = Button("Remover", () => { }); remove.Click += async (_, _) => { try { if (list.SelectedItems.Count > 0) { var x = (CoreWebView2BrowserExtension)list.SelectedItems[0].Tag!; await x.RemoveAsync(); await Render(); } } catch (Exception ex) { ShowNotice(ex.Message); } };
        actions.Controls.AddRange([add, toggle, remove]); f.Controls.Add(list); f.Controls.Add(help); f.Controls.Add(actions);
        try { await Render(); f.ShowDialog(this); } catch (Exception ex) { ShowNotice(ex.Message); }
    }
    static void CopyExtension(string source, string destination)
    {
        if (Path.GetFullPath(destination).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(source)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Escolha somente a pasta específica da extensão, fora da pasta de dados do Clean Browser.");
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source)) if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var dir in Directory.EnumerateDirectories(source)) if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0) CopyExtension(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }
    void ShowSettings()
    {
        using var f = Dialog("Personalizar o Clean Browser", 720, 650);
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20), AutoScroll = true }; grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(string label, Control c) { int row = grid.RowCount++; grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); grid.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, row); c.Dock = DockStyle.Fill; grid.Controls.Add(c, 1, row); }
        ComboBox Choice(string[] options, string value) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }; c.Items.AddRange(options); c.SelectedItem = value; if (c.SelectedIndex < 0) c.SelectedIndex = 0; return c; }
        var theme = Choice(["Escuro", "Claro"], store.Settings.Theme); Row("Tema", theme);
        var animations = new CheckBox { Checked = store.Settings.Animations, Text = "Transições suaves" }; Row("Animações", animations);
        var accent = new TextBox { Text = store.Settings.Accent }; Row("Cor de destaque (#A8E6CF)", accent);
        var engine = Choice(["Google", "DuckDuckGo", "Bing"], store.Settings.Search); Row("Buscador", engine);
        var idle = new NumericUpDown { Minimum = 1, Maximum = 60, Value = store.Settings.IdleMinutes }; Row("Suspender após (minutos)", idle);
        var budget = new NumericUpDown { Minimum = 256, Maximum = 32768, Increment = 128, Value = store.Settings.MemoryBudgetMb }; Row("Meta de RAM total (MB)", budget);
        var restore = new CheckBox { Checked = store.Settings.RestoreTabs, Text = "Restaurar endereços ao abrir" }; Row("Sessão", restore);
        var client = new TextBox { Text = store.Settings.GoogleClientId }; Row("Google OAuth Client ID (Desktop)", client);
        var secret = new TextBox { Text = store.Settings.GoogleClientSecret, UseSystemPasswordChar = true }; Row("Google OAuth Client secret", secret);
        var repo = new TextBox { Text = store.Settings.UpdateRepository, PlaceholderText = "seu-usuario/seu-repositorio" }; Row("Atualizações pelo GitHub", repo);
        Row("Dados locais", Button("Abrir pasta de dados", () => Process.Start(new ProcessStartInfo(Storage.Root) { UseShellExecute = true })));
        var save = Button("Salvar personalização", () =>
        {
            try { _ = ColorTranslator.FromHtml(accent.Text); } catch { ShowNotice("Use uma cor válida, como #A8E6CF."); return; }
            if (repo.Text.Length > 0 && !Updater.ValidRepository(repo.Text)) { ShowNotice("Repositório deve ser usuario/nome."); return; }
            var p = store.Settings; p.Animations = animations.Checked; Motion.Enabled = p.Animations; p.Theme = theme.Text; p.Accent = accent.Text; p.Search = engine.Text; p.IdleMinutes = (int)idle.Value; p.MemoryBudgetMb = (int)budget.Value; p.RestoreTabs = restore.Checked; p.GoogleClientId = client.Text.Trim(); p.GoogleClientSecret = secret.Text.Trim(); p.UpdateRepository = repo.Text.Trim(); store.SaveSettings();
            ApplyTheme(this); foreach (var t in tabs) { if (t.Url == "kay://new" && t.View == null) ShowHome(t); if (t.View?.CoreWebView2 is { } c) c.Profile.PreferredColorScheme = p.Theme == "Claro" ? CoreWebView2PreferredColorScheme.Light : CoreWebView2PreferredColorScheme.Dark; } f.Close();
        }); save.Dock = DockStyle.Bottom; f.Controls.Add(grid); f.Controls.Add(save); f.ShowDialog(this);
    }
    async Task LoginGoogle()
    {
        try
        {
            var user = GoogleLogin.ReadProfile();
            if (user != null)
            {
                if (ShowNotice($"Conectado como {user.Name}\n{user.Email}\n\nSair da conta local? Isso não encerra sessões dos sites.", "Conta Google", MessageBoxButtons.YesNo) == DialogResult.Yes) GoogleLogin.SignOut();
                return;
            }
            if (string.IsNullOrWhiteSpace(store.Settings.GoogleClientId)) { ShowNotice("Cadastre um OAuth Client do tipo Desktop no Google Cloud e informe o ID e o secret em Personalizar.\n\nO login identifica seu perfil local. Não sincroniza senhas, favoritos ou sessões do Chrome.", "Configuração necessária"); ShowSettings(); return; }
            status.Text = "Aguardando login no navegador padrão…";
            user = await GoogleLogin.SignIn(store.Settings.GoogleClientId, store.Settings.GoogleClientSecret);
            status.Text = "Olá, " + user.Name; ShowNotice("Conta conectada: " + user.Email, "Clean Browser");
        }
        catch (Exception ex) { ShowNotice(ex.Message, "Login Google"); }
    }
    Task CheckUpdate()
    {
        using var center = new UpdateCenter(store, Colors);
        if (center.ShowDialog(this) == DialogResult.OK) Close();
        return Task.CompletedTask;
    }
}
