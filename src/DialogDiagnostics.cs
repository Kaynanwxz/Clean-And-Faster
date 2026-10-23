namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task<string[]> VerifyDialogFlows()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),(ok?"PASS ":"FAIL ")+text+Environment.NewLine);if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        Button ButtonIn(AppDialog f,string text)=>AppearancePanel.Descendants(f).OfType<Button>().Single(b=>b.Text==text);
        TextBox Input(AppDialog f)=>AppearancePanel.Descendants(f).OfType<TextBox>().First(t=>!t.ReadOnly);
        void Script(Action open,Action<AppDialog> step)
        {
            Exception? error=null;var deadline=DateTime.UtcNow.AddSeconds(12);
            using var driver=new System.Windows.Forms.Timer{Interval=25};
            driver.Tick+=(_,_)=>{
                try{if(DateTime.UtcNow>deadline)throw new TimeoutException("Diálogo não completou o cenário.");var top=Application.OpenForms.Cast<Form>().OfType<AppDialog>().LastOrDefault();if(top!=null){File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),"DIALOG "+top.Text+Environment.NewLine);step(top);}}
                catch(Exception e){error=e;driver.Stop();foreach(var f in Application.OpenForms.Cast<Form>().OfType<AppDialog>().Reverse().ToArray())f.Close();}
            };
            driver.Start();open();driver.Stop();if(error!=null)throw new InvalidOperationException("Teste de botões do diálogo",error);
        }
        Script(()=>ShortcutCommand("edit",null),f=>{var inputs=AppearancePanel.Descendants(f).OfType<TextBox>().ToArray();inputs[0].Text="Atalho de teste";inputs[1].Text="https://example.com/fixture";ButtonIn(f,"Salvar").PerformClick();});
        var link=store.Settings.Shortcuts.Single(s=>s.Name=="Atalho de teste");Check(new Storage().Settings.Shortcuts.Any(s=>s.Id==link.Id),"Adicionar atalho pelo botão Salvar persiste");
        Script(()=>ShortcutCommand("edit",link),f=>{Input(f).Text="Atalho editado";ButtonIn(f,"Salvar").PerformClick();});Check(store.Settings.Shortcuts.Single(s=>s.Id==link.Id).Name=="Atalho editado","Editar atalho pelo botão Salvar");
        Script(()=>ShortcutCommand("edit",link),f=>{Input(f).Text="Não salvar";f.Close();});Check(store.Settings.Shortcuts.Single(s=>s.Id==link.Id).Name=="Atalho editado","Cancelar edição mantém atalho");ShortcutCommand("remove",link);
        int stage=0;string? workspaceId=null;
        Script(ManageWorkspaces,f=>{
            if(f.Text=="Espaços de trabalho")
            {
                var list=AppearancePanel.Descendants(f).OfType<ListBox>().Single();
                if(stage==0){stage=1;ButtonIn(f,"Novo espaço").PerformClick();}
                else if(stage==2){var w=store.Settings.Workspaces.Single(w=>w.Name=="Espaço teste");workspaceId=w.Id;list.SelectedItem=w;stage=3;ButtonIn(f,"Renomear").PerformClick();}
                else if(stage==4){Check(new Storage().Settings.Workspaces.Single(w=>w.Id==workspaceId).Name=="Espaço renomeado","Renomear espaço pelo botão");list.SelectedItem=store.Settings.Workspaces.Single(w=>w.Id==workspaceId);stage=5;ButtonIn(f,"Excluir").PerformClick();}
                else if(stage==6){Check(!store.Settings.Workspaces.Any(w=>w.Id==workspaceId),"Excluir espaço confirmado");f.Close();}
            }
            else if(f.Text=="Novo espaço"){Input(f).Text="Espaço teste";stage=2;ButtonIn(f,"Salvar").PerformClick();}
            else if(f.Text=="Renomear espaço"){Input(f).Text="Espaço renomeado";stage=4;ButtonIn(f,"Salvar").PerformClick();}
            else if(f.Text=="Excluir espaço"){stage=6;ButtonIn(f,"Sim").PerformClick();}
        });Check(stage==6,"Criar, renomear e excluir espaço concluídos");
        string initial=WorkspaceId;
        Script(ManageWorkspaces,f=>{var list=AppearancePanel.Descendants(f).OfType<ListBox>().Single();list.SelectedItem=store.Settings.Workspaces.First(w=>w.Id!=initial);ButtonIn(f,"Abrir").PerformClick();});await Task.Delay(60);Check(WorkspaceId!=initial,"Abrir espaço pelo botão");await SwitchWorkspace(initial);
        stage=0;string? noteId=null;
        Script(()=>ShowLibrary("note"),f=>{
            if(f.Text.StartsWith("Biblioteca ·"))
            {
                var list=AppearancePanel.Descendants(f).OfType<ListView>().Single();
                if(stage==0){stage=1;ButtonIn(f,"Nova nota").PerformClick();}
                else if(stage==3){var note=store.Library.Single(n=>n.Title=="Nota de teste");noteId=note.Id;Check(new Storage().Library.Any(n=>n.Id==noteId),"Criar nota pelo botão persiste");var item=list.Items.Cast<ListViewItem>().Single(i=>i.Tag is LibraryEntry e&&e.Id==noteId);list.Focus();item.Selected=true;stage=4;ButtonIn(f,"Abrir / editar").PerformClick();}
                else if(stage==6){Check(store.Library.Single(n=>n.Id==noteId).Text=="Conteúdo editado","Editar nota pelos diálogos");list.Focus();list.Items.Cast<ListViewItem>().Single(i=>i.Tag is LibraryEntry e&&e.Id==noteId).Selected=true;stage=7;ButtonIn(f,"Remover").PerformClick();}
                else if(stage==8){Check(!new Storage().Library.Any(n=>n.Id==noteId),"Remover nota persiste");f.Close();}
            }
            else if(f.Text=="Documento ou nota"){Input(f).Text="Nota de teste";stage=stage==1?2:5;ButtonIn(f,"Salvar").PerformClick();}
            else if(f.Text=="Editar nota"){Input(f).Text=stage==2?"Conteúdo original":"Conteúdo editado";stage=stage==2?3:6;ButtonIn(f,"Salvar").PerformClick();}
            else if(f.Text=="Biblioteca"){stage=8;ButtonIn(f,"Sim").PerformClick();}
        });Check(stage==8,"Fluxo criar, editar e remover nota completo");
        stage=0;
        Script(()=>ShowLibrary("source"),f=>{
            if(f.Text.StartsWith("Biblioteca ·"))
            {
                if(stage==0){stage=1;ButtonIn(f,"Adicionar endereço").PerformClick();}
                else if(stage==3){Check(store.Library.Any(e=>e.Title=="Fonte teste"&&e.Url=="https://example.com/test"),"Salvar fonte por endereço");f.Close();}
            }
            else if(f.Text=="Salvar endereço"){Input(f).Text=stage==1?"Fonte teste":"https://example.com/test";stage++;ButtonIn(f,"Salvar").PerformClick();}
        });store.Library.RemoveAll(e=>e.Title=="Fonte teste");store.SaveLibrary();
        store.Bookmarks.Add(new("Favorito teste","https://example.com/test"));store.SaveBookmarks();
        Script(ShowBookmarks,f=>{var list=AppearancePanel.Descendants(f).OfType<ListView>().Single();list.Focus();list.Items[list.Items.Count-1].Selected=true;ButtonIn(f,"Remover selecionado").PerformClick();Check(!store.Bookmarks.Any(b=>b.Title=="Favorito teste"),"Remover favorito pelo botão");f.Close();});
        store.Record("Histórico teste","https://example.com/test");stage=0;
        Script(ShowHistory,f=>{
            if(f.Text=="Histórico"){if(stage==0){Input(f).Text="Histórico teste";var list=AppearancePanel.Descendants(f).OfType<ListView>().Single();Check(list.Items.Count==1,"Filtro do histórico");stage=1;ButtonIn(f,"Apagar histórico").PerformClick();}else if(stage==2){Check(store.History.Count==0&&new Storage().History.Count==0,"Apagar histórico confirmado");f.Close();}}
            else{stage=2;ButtonIn(f,"Sim").PerformClick();}
        });
        Script(ConfigureAssistant,f=>{var model=AppearancePanel.Descendants(f).OfType<TextBox>().Single(t=>t.AccessibleName=="Modelo de IA");model.Text="gpt-5.4-mini";ButtonIn(f,"Salvar").PerformClick();});Check(new Storage().Settings.AiModel=="gpt-5.4-mini","Salvar modelo de IA pelo botão");
        AiSecrets.Save("diagnostic-only-not-a-real-key");Script(ConfigureAssistant,f=>{ButtonIn(f,"Remover chave").PerformClick();f.Close();});Check(AiSecrets.Read()=="","Remover chave de IA pelo botão");
        stage=0;
        Script(ShowSettings,f=>{
            if(f.Text=="Configurações avançadas")
            {
                var inputs=AppearancePanel.Descendants(f).OfType<TextBox>().ToArray();
                if(stage==0){inputs.Single(t=>t.Text==store.Settings.Accent).Text="#INVALID";stage=1;ButtonIn(f,"Salvar personalização").PerformClick();}
                else if(stage==2){inputs[0].Text="#8FBBEF";ButtonIn(f,"Salvar personalização").PerformClick();}
            }
            else{stage=2;ButtonIn(f,"Entendi").PerformClick();}
        });Check(store.Settings.Accent=="#8FBBEF","Configurações rejeitam cor inválida e salvam correção");
        var background=WorkspaceTabs.First(t=>t!=Current);await CreateView(background,false);stage=0;
        Script(ShowMemory,f=>{
            if(f.Text=="Memória e abas")
            {
                var list=AppearancePanel.Descendants(f).OfType<ListView>().Single();void Select(){list.Focus();list.Items.Cast<ListViewItem>().Single(i=>ReferenceEquals(i.Tag,background)).Selected=true;}
                if(stage==0){Select();ButtonIn(f,"Proteger / desproteger").PerformClick();Check(background.Pinned,"Proteger aba pelo botão");stage=1;}
                else if(stage==1){Select();stage=2;ButtonIn(f,"Descarregar selecionada").PerformClick();}
                else if(stage==3){Check(background.View!=null,"Proteção impede descarregar aba");Select();ButtonIn(f,"Proteger / desproteger").PerformClick();stage=4;}
                else if(stage==4){Select();stage=5;ButtonIn(f,"Descarregar selecionada").PerformClick();}
                else if(stage==6){Check(background.View==null&&!background.Pinned,"Descarregar confirmado libera WebView");f.Close();}
            }
            else if(stage==2){stage=3;ButtonIn(f,"Entendi").PerformClick();}
            else if(stage==5){stage=6;ButtonIn(f,"Sim").PerformClick();}
        });
        using(var probe=new BrowserWindow(diagnostics:true))
        {
            probe.Show();await Task.Delay(50);
            Button WindowButton(string name)=>probe.windowButtons!.Controls.OfType<Button>().Single(b=>b.AccessibleName?.StartsWith(name)==true);
            WindowButton("Minimizar").PerformClick();await Task.Delay(60);Check(probe.WindowState==FormWindowState.Minimized,"Botão minimizar");probe.WindowState=FormWindowState.Normal;
            WindowButton("Maximizar").PerformClick();await Task.Delay(60);Check(probe.WindowState==FormWindowState.Maximized,"Botão maximizar");WindowButton("Maximizar").PerformClick();await Task.Delay(60);Check(probe.WindowState==FormWindowState.Normal,"Botão restaurar janela");WindowButton("Fechar navegador").PerformClick();Check(probe.IsDisposed,"Botão fechar encerra apenas janela de teste");
        }
        Activate();SaveSession();return checks.ToArray();
    }
}
