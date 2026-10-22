namespace KayBrowser;

public sealed class AppearancePanel : Panel,IThemed
{
    readonly Storage store;readonly ComboBox preset,theme,tabs,layout,background,shortcuts,density,chrome;
    readonly CheckBox animate,focus;readonly TextBox accent;
    readonly Action changed;readonly Action upload;bool loading;
    public event Action? CloseRequested;
    public AppearancePanel(Storage data,Action apply,Action chooseWallpaper)
    {
        store=data;changed=apply;upload=chooseWallpaper;Dock=DockStyle.Fill;Padding=new Padding(18);AccessibleName="Personalização do navegador";
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,76));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        header.Controls.Add(new Label{Text="Seu navegador,\ndo seu jeito",Font=new Font("Georgia",18),Dock=DockStyle.Fill},0,0);var close=new SoftButton{Glyph="close",AccessibleName="Fechar aparência",Dock=DockStyle.Top,Height=32};close.Click+=(_,_)=>CloseRequested?.Invoke();header.Controls.Add(close,1,0);
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};var grid=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Padding=new Padding(0,0,8,8)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(grid);
        ComboBox Choice(string label,string[] options)
        {
            var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName=label};c.Items.AddRange(options);
            Row(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft},26);Row(c,35);return c;
        }
        void Row(Control c,int height){int row=grid.RowCount++;grid.RowStyles.Add(new RowStyle(SizeType.Absolute,height));grid.Controls.Add(c,0,row);}
        preset=Choice("Visual",VisualPresets.All.Select(v=>v.Name).ToArray());theme=Choice("Tema",["Claro","Escuro"]);chrome=Choice("Barra do navegador",["Azul-marinho","Clara"]);tabs=Choice("Posição das abas",["Superior","Lateral"]);layout=Choice("Página inicial",["Clássico","Central","Editorial","Essencial","Espaços","Cristal","Biblioteca"]);
        var swatches=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};foreach(string hex in new[]{"#8FBBEF","#A9C9EF","#5D96D5","#C4D2E2","#F2F5FA"}){var b=new Button{Width=37,Height=28,BackColor=ColorTranslator.FromHtml(hex),FlatStyle=FlatStyle.Flat,AccessibleName="Destaque "+hex};b.Click+=(_,_)=>{if(accent!=null)accent.Text=hex;};swatches.Controls.Add(b);}Row(new Label{Text="Cor de destaque",Dock=DockStyle.Fill},26);Row(swatches,36);accent=new TextBox{Dock=DockStyle.Fill,MaxLength=16,AccessibleName="Cor hexadecimal"};Row(accent,32);
        background=Choice("Plano de fundo",["Arte","Suave","Sem arte","Personalizado"]);var imageActions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var add=new SoftButton{Text="Escolher imagem",Width=150,Height=32};add.Click+=(_,_)=>{upload();background.SelectedItem=store.Settings.Background;};var remove=new SoftButton{Text="Remover",Width=100,Height=32};remove.Click+=(_,_)=>{store.Settings.Wallpaper="";background.SelectedItem="Sem arte";};imageActions.Controls.AddRange([add,remove]);Row(imageActions,38);
        shortcuts=Choice("Atalhos",["Meus atalhos","Mais visitados","Ocultar"]);density=Choice("Densidade",["Confortável","Compacta"]);
        animate=new CheckBox{Text="Animações suaves",Dock=DockStyle.Fill};focus=new CheckBox{Text="Modo foco",Dock=DockStyle.Fill};Row(animate,32);Row(focus,32);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));var reset=new SoftButton{Text="Restaurar padrão",Dock=DockStyle.Fill};var save=new SoftButton{Text="Aplicar",Dock=DockStyle.Fill};actions.Controls.Add(reset,0,0);actions.Controls.Add(save,1,0);
        save.Click+=(_,_)=>Apply();reset.Click+=(_,_)=>{VisualPresets.Apply(store.Settings,"Copiloto Azul");store.Settings.Accent="#8FBBEF";store.Settings.Wallpaper="";store.Settings.FocusMode=false;store.Settings.Animations=true;store.Settings.ShowShortcuts=true;LoadValues();Apply();};
        preset.SelectedIndexChanged+=(_,_)=>{if(!loading&&preset.SelectedItem is string value){VisualPresets.Apply(store.Settings,value);LoadValues();Apply();}};
        body.Controls.Add(header,0,0);body.Controls.Add(scroll,0,1);body.Controls.Add(actions,0,2);Controls.Add(body);LoadValues();SetPalette(Palette.Create(true,Color.SteelBlue));
    }
    void LoadValues(){loading=true;try{var p=store.Settings;preset.SelectedItem=p.Visual;theme.SelectedItem=p.Theme;chrome.SelectedIndex=p.DarkChrome?0:1;tabs.SelectedItem=p.VerticalTabs?"Lateral":"Superior";layout.SelectedItem=p.HomeLayout;background.SelectedItem=p.Background;accent.Text=p.Accent;shortcuts.SelectedItem=!p.ShowShortcuts?"Ocultar":p.FrequentShortcuts?"Mais visitados":"Meus atalhos";density.SelectedIndex=p.Compact?1:0;animate.Checked=p.Animations;focus.Checked=p.FocusMode;}finally{loading=false;}}
    void Apply()
    {
        try{if(accent.Text.Length!=7||accent.Text[0]!='#')throw new ArgumentException();_=ColorTranslator.FromHtml(accent.Text);}catch{System.Media.SystemSounds.Beep.Play();accent.Focus();return;}
        var p=store.Settings;p.Theme=theme.Text;p.DarkChrome=chrome.SelectedIndex==0;p.VerticalTabs=tabs.SelectedIndex==1;p.HomeLayout=layout.Text;p.Background=background.Text;p.Accent=accent.Text;p.ShowShortcuts=shortcuts.SelectedIndex!=2;p.FrequentShortcuts=shortcuts.SelectedIndex==1;p.Compact=density.SelectedIndex==1;p.Animations=animate.Checked;p.FocusMode=focus.Checked;
        store.SaveSettings();changed();
    }
    public void SetPalette(Palette unused)
    {
        var p=Palette.Create(true,Color.SteelBlue);
        void Theme(Control c){if(c is Button b&&b is not SoftButton)return;c.BackColor=p.Background;c.ForeColor=p.Text;if(c is SoftButton soft)soft.SetPalette(p);foreach(Control child in c.Controls)Theme(child);}
        Theme(this);
    }
}
