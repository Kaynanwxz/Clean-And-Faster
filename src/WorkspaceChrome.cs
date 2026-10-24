namespace KayBrowser;

public sealed partial class BrowserWindow
{
    readonly TableLayoutPanel browserSurface=new BufferedTableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
    readonly TableLayoutPanel sidebar=new BufferedTableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};
    readonly ComboBox workspacePicker=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Espaço de trabalho"};
    readonly FlowLayoutPanel sideActions=new(){Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
    readonly Label brandCaption=new(){Text="Clean Browser",TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill,Padding=new Padding(46,0,0,0),Font=new Font("Georgia",14)};
    readonly Panel rightDock=new(){Dock=DockStyle.Fill,Margin=Padding.Empty};
    bool refreshingWorkspaces;
    bool RightOpen=>assistantOpen||appearanceOpen||appsOpen;
    string WorkspaceId=>store.Settings.ActiveWorkspace;
    IEnumerable<BrowserTab> WorkspaceTabs=>tabs.Where(t=>t.Page.WorkspaceId==WorkspaceId);
    void InitializeWorkspaceChrome()
    {
        browserSurface.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,0));browserSurface.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));browserSurface.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,42));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,0));sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
        var wsHeader=new BufferedTableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(6),Margin=Padding.Empty};wsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));wsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));wsHeader.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        wsHeader.Controls.Add(workspacePicker,0,0);var manage=new SoftButton{Glyph="settings",Dock=DockStyle.Fill,AccessibleName="Gerenciar espaços",Margin=Padding.Empty};manage.SetPalette(Colors);manage.Click+=(_,_)=>ManageWorkspaces();wsHeader.Controls.Add(manage,1,0);sidebar.Controls.Add(wsHeader,0,0);
        foreach(var (glyph,tip,action) in new (string,string,Action)[]{("history","Histórico",ShowHistory),("star","Biblioteca",()=>ShowLibrary("reading")),("globe","Espaços",ManageWorkspaces),("settings","Aparência",ShowAppearance)})
        {var b=new SoftButton{Glyph=glyph,AccessibleName=tip,Size=new Size(34,32),Margin=new Padding(2)};b.SetPalette(Colors);b.Click+=(_,_)=>action();chromeTips.SetToolTip(b,tip);sideActions.Controls.Add(b);}
        sidebar.Controls.Add(sideActions,0,3);browserSurface.Controls.Add(sidebar,0,0);browserSurface.Controls.Add(tabStrip,1,0);
        workspacePicker.SelectedIndexChanged+=async(_,_)=>{if(!refreshingWorkspaces&&workspacePicker.SelectedItem is WorkspaceChoice w&&w.Id!=WorkspaceId)await SwitchWorkspace(w.Id);};
        tabStrip.PageFilter=p=>p.WorkspaceId==WorkspaceId;
        brandCaption.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)NativeFrame.Drag(this);};
        brandCaption.Paint+=(_,e)=>e.Graphics.DrawImage(BrandAssets.Logo,new Rectangle(Ui(10),(brandCaption.Height-Ui(28))/2,Ui(28),Ui(28)));
        browserSurface.Resize+=(_,_)=>ApplyWorkspaceMetrics();RefreshWorkspacePicker();
    }
    sealed record WorkspaceChoice(string Id,string Name){public override string ToString()=>Name;}
    void RefreshWorkspacePicker()
    {
        refreshingWorkspaces=true;try{workspacePicker.Items.Clear();foreach(var w in store.Settings.Workspaces)workspacePicker.Items.Add(new WorkspaceChoice(w.Id,w.Name));workspacePicker.SelectedIndex=store.Settings.Workspaces.FindIndex(w=>w.Id==WorkspaceId);}finally{refreshingWorkspaces=false;}
    }
    void ApplyWorkspaceMetrics()
    {
        if(titleRow==null||browserSurface.ColumnStyles.Count<2)return;
        bool vertical=store.Settings.VerticalTabs&&!store.Settings.FocusMode&&!fullscreen;
        bool narrow=ClientSize.Width<Ui(RightOpen?1280:1000);
        int side=vertical?Ui(narrow?52:206):0;
        if(browserSurface.ColumnStyles[0].Width!=side)browserSurface.ColumnStyles[0].Width=side;
        sidebar.Visible=vertical;workspacePicker.Parent!.Visible=!narrow;sideActions.Visible=!narrow;
        sidebar.RowStyles[0].Height=narrow?0:Ui(44);sidebar.RowStyles[3].Height=narrow?0:Ui(44);
        if(vertical)
        {
            if(tabStrip.Header.Parent!=sidebar){tabStrip.Header.Parent?.Controls.Remove(tabStrip.Header);sidebar.Controls.Add(tabStrip.Header,0,2);}
            if(!titleRow.Controls.Contains(brandCaption))titleRow.Controls.Add(brandCaption,0,0);
        }
        else
        {
            if(tabStrip.Header.Parent!=titleRow){tabStrip.Header.Parent?.Controls.Remove(tabStrip.Header);titleRow.Controls.Remove(brandCaption);titleRow.Controls.Add(tabStrip.Header,0,0);}
        }
        tabStrip.Header.Vertical=vertical;tabStrip.Header.EnsureSelectedVisible();tabStrip.Header.Invalidate();
    }
    async Task SwitchWorkspace(string id)
    {
        if(!store.Settings.Workspaces.Any(w=>w.Id==id))return;
        SaveSession();aiCancellation?.Cancel();ResetSnapshot();store.Settings.ActiveWorkspace=id;
        var first=WorkspaceTabs.FirstOrDefault();
        if(first==null)
        {
            var urls=store.Settings.RestoreTabs?store.Settings.Workspaces.First(w=>w.Id==id).Urls.ToArray():[];
            if(urls.Length==0)await AddTab("kay://new");else foreach(var url in urls)await AddTab(url,WorkspaceTabs.Count()==0);
        }
        else {tabStrip.SelectedTab=first.Page;await Activate(first);}
        RefreshWorkspacePicker();tabStrip.Header.EnsureSelectedVisible();tabStrip.Header.Invalidate();store.SaveSettings();RefreshHomePages();
    }
    void RefreshHomePages()
    {
        foreach(var t in tabs.Where(t=>t.Url=="kay://new"&&t.View==null).ToArray()){if(t.Page.Controls.OfType<HomeBoard>().FirstOrDefault() is {} home)home.RefreshSettings(Colors);else ShowHome(t);}
    }
    void ManageWorkspaces()
    {
        using var f=Dialog("Espaços de trabalho",640,440);var list=new ListBox{Dock=DockStyle.Fill,DisplayMember="Name"};
        void Render(){list.Items.Clear();foreach(var w in store.Settings.Workspaces)list.Items.Add(w);}
        var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=92,Padding=new Padding(8)};
        bar.Controls.Add(Button("Novo espaço",()=>{if(store.Settings.Workspaces.Count>=12){ShowNotice("Limite de 12 espaços.");return;}string? name=AskText("Novo espaço","Nome do espaço","");if(string.IsNullOrWhiteSpace(name))return;store.Settings.Workspaces.Add(new(){Name=name[..Math.Min(40,name.Length)]});store.SaveSettings();Render();RefreshWorkspacePicker();RefreshHomePages();}));
        bar.Controls.Add(Button("Renomear",()=>{if(list.SelectedItem is not Workspace w)return;string? name=AskText("Renomear espaço","Nome",w.Name);if(string.IsNullOrWhiteSpace(name))return;w.Name=name[..Math.Min(40,name.Length)];store.SaveSettings();Render();RefreshWorkspacePicker();RefreshHomePages();}));
        bar.Controls.Add(Button("Abrir",()=>{if(list.SelectedItem is Workspace w){f.Close();_ = SwitchWorkspace(w.Id);}}));
        bar.Controls.Add(Button("Excluir",()=>{
            if(list.SelectedItem is not Workspace w)return;if(store.Settings.Workspaces.Count==1){ShowNotice("Mantenha pelo menos um espaço.");return;}
            if(ShowNotice("Excluir este espaço e fechar suas abas? Formulários não salvos serão perdidos.","Excluir espaço",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            if(w.Id==WorkspaceId)tabStrip.SelectedTab=null;
            foreach(var t in tabs.Where(t=>t.Page.WorkspaceId==w.Id).ToArray()){tabs.Remove(t);t.View?.Dispose();tabStrip.TabPages.Remove(t.Page);t.Page.Dispose();}
            store.Settings.Workspaces.Remove(w);if(w.Id==WorkspaceId){var id=store.Settings.Workspaces[0].Id;_ = SwitchWorkspace(id);}store.SaveSettings();Render();RefreshWorkspacePicker();RefreshHomePages();
        }));
        f.Controls.Add(list);f.Controls.Add(bar);ApplyTheme(f);Render();f.ShowDialog(this);
    }
    string? AskText(string title,string label,string initial,bool multiline=false)
    {
        using var f=Dialog(title,580,multiline?450:245);var body=new BufferedTableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(18)};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,32));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
        var text=new TextBox{Text=initial,Dock=DockStyle.Fill,Multiline=multiline,MaxLength=multiline?50000:4096,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None};var save=new SoftButton{Text="Salvar",Dock=DockStyle.Right,Width=130,DialogResult=DialogResult.OK};save.SetPalette(Colors);body.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill},0,0);body.Controls.Add(text,0,1);body.Controls.Add(save,0,2);f.Controls.Add(body);ApplyTheme(f);if(!multiline)f.AcceptButton=save;return f.ShowDialog(this)==DialogResult.OK?text.Text.Trim():null;
    }
}
