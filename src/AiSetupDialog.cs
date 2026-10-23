namespace KayBrowser;
public sealed class AiSetupDialog : AppDialog
{
    readonly Storage store;
    readonly ComboBox provider=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Provedor de IA"};
    readonly TextBox endpoint=new(){Dock=DockStyle.Fill,AccessibleName="Endereço da API",PlaceholderText="https://seu-provedor/v1/chat/completions"};
    readonly TextBox model=new(){Dock=DockStyle.Fill,AccessibleName="Modelo de IA"};
    readonly TextBox key=new(){Dock=DockStyle.Fill,UseSystemPasswordChar=true,MaxLength=4096,AccessibleName="Chave da API",PlaceholderText="Cole sua chave; vazio mantém a chave deste endereço"};
    readonly Label status=new(){Dock=DockStyle.Fill,AutoEllipsis=false,Text="Escolha o provedor, informe sua chave e salve."};
    readonly SoftButton test=new(){Text="Testar conexão",Dock=DockStyle.Fill,AccessibleName="Testar conexão de IA"},save=new(){Text="Salvar",Dock=DockStyle.Fill};
    CancellationTokenSource? testing;
    readonly HttpClient? diagnosticHttp;
    public AiSetupDialog(Storage data,Palette colors,HttpClient? http=null):base("Configurar Clean AI",colors,660,650)
    {
        store=data;diagnosticHttp=http;
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true,Padding=new Padding(22),RowCount=11};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(int h in new[]{76,25,36,25,36,25,36,25,36,62,44})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        var intro=new Label{Dock=DockStyle.Fill,Text="Conecte sua IA uma vez e converse aqui. Sua chave fica protegida pelo Windows. A API pode cobrar uso; assinatura do ChatGPT não inclui crédito de API. O teste envia apenas um pedido curto."};
        provider.Items.AddRange(["OpenAI","OpenRouter","Personalizada"]);provider.SelectedItem=provider.Items.Contains(data.Settings.AiProvider)?data.Settings.AiProvider:"OpenAI";endpoint.Text=data.Settings.AiEndpoint;model.Text=data.Settings.AiModel;
        void Row(int i,string text,Control input){grid.Controls.Add(new Label{Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft},0,i);grid.Controls.Add(input,0,i+1);}
        grid.Controls.Add(intro,0,0);Row(1,"1. Sua IA",provider);Row(3,"Endereço da API",endpoint);Row(5,"2. Modelo",model);Row(7,"3. Chave da API",key);grid.Controls.Add(status,0,9);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};for(int i=0;i<3;i++)actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var remove=new SoftButton{Text="Remover chave",Dock=DockStyle.Fill};foreach(var b in new[]{test,save,remove})b.SetPalette(colors);actions.Controls.Add(test,0,0);actions.Controls.Add(remove,1,0);actions.Controls.Add(save,2,0);grid.Controls.Add(actions,0,10);Controls.Add(grid);
        provider.SelectedIndexChanged+=(_,_)=>{key.Clear();if(provider.Text=="OpenAI"){endpoint.Text=AiConnection.OpenAiUrl;model.Text="gpt-5.4-mini";}else if(provider.Text=="OpenRouter"){endpoint.Text=AiConnection.RouterUrl;model.Text="openai/gpt-4o-mini";}else{endpoint.Text="";model.Text="";}endpoint.ReadOnly=provider.Text!="Personalizada";status.Text="A chave fica vinculada a este endereço e não é reutilizada em outro provedor.";};
        endpoint.ReadOnly=provider.Text!="Personalizada";
        save.Click+=(_,_)=>{
            try{var c=Chosen();string secret=AiPolicy.CleanKey(key.Text);if(secret.Length>0)AiSecrets.Save(secret,c.Url);store.Settings.AiProvider=c.Provider;store.Settings.AiEndpoint=c.Url;store.Settings.AiModel=c.Model;store.SaveSettings();DialogResult=DialogResult.OK;Close();}
            catch(Exception ex)when(ex is ArgumentException or IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException){status.Text=ex is ArgumentException?ex.Message:"Não foi possível salvar a conexão. Tente novamente.";}
        };
        remove.Click+=(_,_)=>{try{var c=Chosen();AiSecrets.Save("",c.Url);key.Clear();status.Text="Chave removida deste provedor.";}catch(Exception ex)when(ex is ArgumentException or IOException or UnauthorizedAccessException){status.Text="Confira o endereço da API para remover a chave.";}};
        test.Click+=async(_,_)=>await TestConnection();
        FormClosing+=(_,_)=>testing?.Cancel();
        void Theme(Control c){c.BackColor=colors.Background;c.ForeColor=colors.Text;if(c is SoftButton b)b.SetPalette(colors);foreach(Control child in c.Controls)Theme(child);}Theme(grid);status.ForeColor=colors.Muted;
    }
    AiConnection Chosen()=>AiConnection.Create(provider.Text,endpoint.Text,model.Text);
    internal async Task TestConnection()
    {
        if(testing!=null)return;
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(45));testing=cancel;test.Enabled=false;save.Enabled=false;provider.Enabled=false;endpoint.Enabled=false;model.Enabled=false;key.Enabled=false;status.Text="Conectando à sua IA…";
        HttpClient? owned=null;
        try
        {
            var c=Chosen();string secret=AiPolicy.CleanKey(key.Text);if(secret.Length==0)secret=AiSecrets.Read(c.Url);if(secret.Length==0&&!new Uri(c.Url).IsLoopback)throw new ArgumentException("Cole a chave da API do provedor escolhido.");
            var http=diagnosticHttp??(owned=CreateHttp());bool answered=false;
            await new AiClient(http).Run(secret,c.Model,"Responda apenas: conectado.","",(_,_,_)=>Task.FromResult("Ferramentas desativadas no teste."),_=>answered=true,cancel.Token,c,enableTools:false);
            if(!IsDisposed)status.Text=answered?"Conexão funcionando. Clique em Salvar para usar esta IA.":"A API respondeu sem texto. Confira o modelo.";
        }
        catch(OperationCanceledException){if(!IsDisposed)status.Text="Teste cancelado ou tempo esgotado.";}
        catch(Exception ex)when(ex is ArgumentException or IOException or HttpRequestException or System.Text.Json.JsonException or InvalidOperationException or UnauthorizedAccessException){if(!IsDisposed)status.Text=ex is HttpRequestException?"Não foi possível conectar. Confira o endereço e sua conexão.":ex is System.Text.Json.JsonException?"O endereço não respondeu no formato de uma API compatível.":ex.Message;}
        finally{owned?.Dispose();testing=null;if(!IsDisposed){test.Enabled=true;save.Enabled=true;provider.Enabled=true;endpoint.Enabled=true;model.Enabled=true;key.Enabled=true;}}
    }
    internal string ConnectionStatus=>status.Text;
    public static HttpClient CreateHttp()=>new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(100),MaxResponseContentBufferSize=2*1024*1024};
}
