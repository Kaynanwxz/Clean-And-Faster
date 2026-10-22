namespace KayBrowser;

public sealed partial class BrowserWindow
{
    void ShowLibrary(string initial)
    {
        using var f=Dialog("Biblioteca · leitura, fontes e notas",940,640);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(16)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,94));
        var categories=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};categories.Items.AddRange(new[]{"Lista de leitura","Fontes salvas","Documentos e notas"});categories.SelectedIndex=initial=="source"?1:initial=="note"?2:0;
        var filter=new TextBox{Dock=DockStyle.Fill,PlaceholderText="Filtrar sua biblioteca…"};var list=List("Título","Endereço","Atualizado");
        string Kind()=>categories.SelectedIndex==0?"reading":categories.SelectedIndex==1?"source":"note";
        void Render(){list.Items.Clear();foreach(var e in store.Library.Where(e=>e.Kind==Kind()&&(e.Title+e.Url+e.Text).Contains(filter.Text,StringComparison.OrdinalIgnoreCase)).OrderByDescending(e=>e.Updated))list.Items.Add(new ListViewItem([e.Title,e.Kind=="note"?"Nota local":e.Url,e.Updated.ToLocalTime().ToString("g")]){Tag=e});}
        void Save(LibraryEntry entry){int i=store.Library.FindIndex(e=>e.Id==entry.Id);if(i>=0)store.Library[i]=entry;else{if(store.Library.Count>=500){ShowNotice("Limite de 500 itens na biblioteca.");return;}store.Library.Add(entry);}store.SaveLibrary();Render();RefreshHomePages();}
        void EditNote(LibraryEntry? entry)
        {
            string? title=AskText("Documento ou nota","Título",entry?.Title??"Nova nota");if(string.IsNullOrWhiteSpace(title))return;string? text=AskText("Editar nota","Texto salvo apenas neste computador",entry?.Text??"",true);if(text==null)return;Save(new(entry?.Id??Guid.NewGuid().ToString("N"),title[..Math.Min(120,title.Length)],"","note",text,DateTime.UtcNow));
        }
        void Open(){if(list.SelectedItems.Count==0)return;var e=(LibraryEntry)list.SelectedItems[0].Tag!;if(e.Kind=="note")EditNote(e);else{NavigateCurrent(e.Url);f.Close();}}
        list.DoubleClick+=(_,_)=>Open();filter.TextChanged+=(_,_)=>Render();categories.SelectedIndexChanged+=(_,_)=>Render();
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
        actions.Controls.Add(Button("Salvar página atual",()=>{if(Current is not{} t||!Navigation.IsWebUrl(t.Url)){ShowNotice("Abra um site para salvar.");return;}string kind=Kind()=="note"?"reading":Kind();var old=store.Library.FirstOrDefault(e=>e.Kind==kind&&e.Url==t.Url);Save(new(old?.Id??Guid.NewGuid().ToString("N"),t.Title,t.Url,kind,"",DateTime.UtcNow));}));
        actions.Controls.Add(Button("Adicionar endereço",()=>{var title=AskText("Salvar endereço","Título","");if(string.IsNullOrWhiteSpace(title))return;var url=AskText("Salvar endereço","Endereço HTTPS","");if(url==null)return;try{Save(new(Guid.NewGuid().ToString("N"),title[..Math.Min(120,title.Length)],WebLinks.Normalize(url),Kind()=="note"?"reading":Kind(),"",DateTime.UtcNow));}catch(ArgumentException ex){ShowNotice(ex.Message);}}));
        actions.Controls.Add(Button("Nova nota",()=>{categories.SelectedIndex=2;EditNote(null);}));actions.Controls.Add(Button("Abrir / editar",Open));
        actions.Controls.Add(Button("Remover",()=>{if(list.SelectedItems.Count==0)return;var e=(LibraryEntry)list.SelectedItems[0].Tag!;if(ShowNotice("Remover “"+e.Title+"”?","Biblioteca",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;store.Library.Remove(e);store.SaveLibrary();Render();RefreshHomePages();}));
        actions.Controls.Add(Button("Importar TXT / MD",()=>{
            using var picker=new OpenFileDialog{Filter="Texto ou Markdown|*.txt;*.md",CheckFileExists=true};if(picker.ShowDialog(f)!=DialogResult.OK)return;
            try{if(new FileInfo(picker.FileName).Length>200000)throw new IOException("Importe até 200 KB de texto.");var text=File.ReadAllText(picker.FileName);if(text.Length>50000)throw new IOException("Importe até 50 mil caracteres.");categories.SelectedIndex=2;Save(new(Guid.NewGuid().ToString("N"),Path.GetFileNameWithoutExtension(picker.FileName),"","note",text,DateTime.UtcNow));}catch(IOException ex){ShowNotice(ex.Message);}
        }));
        actions.Controls.Add(Button("Exportar nota",()=>{if(list.SelectedItems.Count==0||list.SelectedItems[0].Tag is not LibraryEntry e||e.Kind!="note")return;using var picker=new SaveFileDialog{Filter="Texto|*.txt",FileName="Nota.txt"};if(picker.ShowDialog(f)==DialogResult.OK)try{File.WriteAllText(picker.FileName,e.Text);}catch(IOException ex){ShowNotice(ex.Message);}}));
        layout.Controls.Add(categories,0,0);layout.Controls.Add(filter,0,1);layout.Controls.Add(list,0,2);layout.Controls.Add(actions,0,3);f.Controls.Add(layout);ApplyTheme(f);Render();f.ShowDialog(this);
    }
}
