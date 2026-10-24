using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    readonly TableLayoutPanel pageHost=new BufferedTableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=Padding.Empty};
    AssistantPanel? assistant;
    readonly Tween assistantTween=new();
    CancellationTokenSource? aiCancellation;
    HttpClient? aiHttp;
    readonly AiConversation aiConversation=new();
    bool assistantOpen,assistantWide;
    string snapshotToken="", snapshotSource="";
    BrowserTab? snapshotTab;
    readonly Dictionary<string,(string label,string kind)> snapshotElements=[];
    void InitializeAssistantHost()
    {
        pageHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pageHost.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,0));pageHost.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        InitializeWorkspaceChrome();
        pageHost.Controls.Add(browserSurface,0,0);pageHost.Controls.Add(rightDock,1,0);
        pageHost.Resize+=(_,_)=>{if(IsHandleCreated)SettleRightPanel();};
        rightDock.Resize+=(_,_)=>{foreach(Control p in rightDock.Controls)p.SetBounds(0,0,AssistantWidth,rightDock.ClientSize.Height);};
    }
    int AssistantWidth=>Math.Min(Ui(assistantWide&&assistantOpen?560:390),pageHost.ClientSize.Width/2);
    void ToggleAssistant()
    {
        CloseBrowserMenu(false);
        if(assistant==null)
        {
            assistant=new();assistant.SetPalette(Palette.Create(store.Settings.Theme=="Claro",Accent));rightDock.Controls.Add(assistant);
            assistant.SendRequested+=()=>_ = AskAssistant();assistant.StopRequested+=()=>aiCancellation?.Cancel();assistant.SetupRequested+=ConfigureAssistant;assistant.CaptureRequested+=()=>_ = ShowAiCapture();assistant.ResetRequested+=aiConversation.Clear;RefreshAiConnectionLabel();assistant.CloseRequested+=ToggleAssistant;assistant.ExpandRequested+=()=>{assistantWide=!assistantWide;AnimateRightPanel(assistant,true);};
        }
        if(appsOpen){appsOpen=false;apps!.Visible=false;}
        if(appearanceOpen){appearanceOpen=false;if(appearance!=null)appearance.Visible=false;}
        assistantOpen=!assistantOpen;
        if(!assistantOpen)aiCancellation?.Cancel();else {assistant.Visible=true;assistant.BringToFront();}
        AnimateRightPanel(assistant,assistantOpen);
    }
    void DisposeAssistant(){aiCancellation?.Cancel();assistantTween.Dispose();aiHttp?.Dispose();}
    void RefreshAiConnectionLabel()
    {
        try{var c=AiConnection.From(store.Settings);assistant?.ConnectionLabel(AiSecrets.Read(c.Url).Length>0||new Uri(c.Url).IsLoopback?c.Provider+" � conex�o":"Conectar IA");}catch(Exception ex)when(ex is ArgumentException or IOException or UnauthorizedAccessException){assistant?.ConnectionLabel("Conectar IA");}
    }
    void ConfigureAssistant()
    {
        using var dialog=new AiSetupDialog(store,Palette.Create(store.Settings.Theme=="Claro",Accent));
        if(dialog.ShowDialog(this)==DialogResult.OK){aiConversation.Clear();assistant?.Reset();}
        RefreshAiConnectionLabel();
    }
    async Task ShowAiCapture()
    {
        if(aiCancellation!=null||assistant==null)return;
        using var dialog=new AiCaptureDialog(Palette.Create(store.Settings.Theme=="Claro",Accent),CaptureAiTab);
        if(dialog.ShowDialog(this)==DialogResult.OK&&dialog.Attachment!=null)assistant.SetAttachment(dialog.Attachment);
        await Task.CompletedTask;
    }
    async Task<Bitmap> CaptureAiTab(CancellationToken cancellation)
    {
        var tab=Current??throw new IOException("Abra uma aba antes de capturar.");
        if(tab.View?.CoreWebView2 is {} core)
        {
            string url=core.Source;using var bytes=new MemoryStream();await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,bytes);cancellation.ThrowIfCancellationRequested();
            if(Current!=tab||core.Source!=url)throw new IOException("A aba mudou durante a captura. Capture novamente.");bytes.Position=0;using var image=Image.FromStream(bytes);if((long)image.Width*image.Height>24_000_000)throw new IOException("A aba excede o limite de captura.");return new Bitmap(image);
        }
        var home=tab.Page.Controls.OfType<HomeBoard>().FirstOrDefault()??throw new IOException("Esta aba ainda n�o est� pronta.");
        if(home.Width<1||home.Height<1||(long)home.Width*home.Height>24_000_000)throw new IOException("�rea de captura indispon�vel.");var bitmap=new Bitmap(home.Width,home.Height);try{home.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));return bitmap;}catch{bitmap.Dispose();throw;}
    }
    async Task AskAssistant()
    {
        if(assistant==null||aiCancellation!=null||string.IsNullOrWhiteSpace(assistant.Prompt)&&assistant.Attachment==null)return;
        AiConnection connection;string key;
        try{connection=AiConnection.From(store.Settings);key=AiSecrets.Read(connection.Url);}
        catch(Exception ex)when(ex is ArgumentException or IOException or UnauthorizedAccessException){ShowNotice("Confira sua conex�o de IA.");ConfigureAssistant();return;}
        if(key.Length==0&&!new Uri(connection.Url).IsLoopback){ConfigureAssistant();return;}
        string question=assistant.Prompt.Trim();if(question.Length==0)question="Analise a imagem compartilhada e me ajude a entend�-la.";
        bool share=assistant.Share,tools=assistant.BrowserActions;var image=assistant.Attachment;assistant.Prompt="";assistant.Add("Voc�",question+(image==null?"":"\n[Imagem: "+image.Label+"]"));assistant.Busy(true);
        using var cancel=new CancellationTokenSource(TimeSpan.FromMinutes(4));aiCancellation=cancel;
        aiHttp??=AiSetupDialog.CreateHttp();var answer=new System.Text.StringBuilder();
        try
        {
            string context=share?TabsContext()+"\nP�gina atual:\n"+await ReadAiPage(cancel.Token):"Compartilhamento de p�ginas e abas desativado.";
            await new AiClient(aiHttp).Run(key,connection.Model,question,context,
                (name,args,token)=>ExecuteAiTool(name,args,share,token),text=>{answer.AppendLine(text);if(!closing)assistant.Add("Clean AI",text);},cancel.Token,connection,aiConversation.Snapshot(),image,tools);
            if(answer.Length==0)throw new IOException("A IA n�o devolveu texto. Confira o modelo na conex�o.");
            aiConversation.Complete(question,answer.ToString());
            if(!closing)assistant.AttachmentSent();
        }
        catch(OperationCanceledException){if(!closing)assistant.Add("Clean AI","Conversa interrompida ou tempo esgotado. A imagem permanece anexada se precisar tentar novamente.");}
        catch(Exception)when(closing){}
        catch(Exception ex)when(ex is HttpRequestException or IOException or JsonException or InvalidOperationException or ArgumentException)
        {if(!closing){assistant.Add("Clean AI",ex is HttpRequestException?"N�o foi poss�vel conectar � sua IA. Confira sua conex�o e tente novamente.":ex is JsonException?"A API retornou um formato incompat�vel. Confira o endere�o em Conectar IA.":ex.Message);assistant.Prompt=question;}}
        finally{aiCancellation=null;key="";if(!closing)assistant.Busy(false);}
    }
    string TabsContext()=>JsonSerializer.Serialize(tabs.Select((t,i)=>new{index=i+1,title=t.Title,url=t.Url,active=t==Current,workspace=t.Page.WorkspaceId}).Where(t=>t.workspace==WorkspaceId).Take(30));
    void ResetSnapshot(){snapshotToken="";snapshotTab=null;snapshotSource="";snapshotElements.Clear();}
    async Task<string> ReadAiPage(CancellationToken cancel)
    {
        ResetSnapshot();
        var tab=Current;var core=tab?.View?.CoreWebView2;
        if(core==null)return "Aba inicial ou p�gina indispon�vel. Abra um site primeiro.";
        string source=core.Source;if(!AiPolicy.ValidUrl(source))return "P�gina n�o compartilh�vel.";
        string token=Guid.NewGuid().ToString("N");
        // Static extraction: never evaluate code supplied by the model; exclude form values.
        string script="""
        (()=>{
          const visible=e=>{const r=e.getBoundingClientRect();return r.width>0&&r.height>0&&getComputedStyle(e).visibility!=='hidden'};
          const sensitive=e=>e.closest('input,textarea,select,[contenteditable],script,style,noscript,[hidden],[aria-hidden="true"]');
          const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);let node,text='';
          while((node=walker.nextNode())&&text.length<12000){if(node.parentElement&&!sensitive(node.parentElement)&&visible(node.parentElement))text+=node.textContent.trim()+' ';}
          const elements=[];
          for(const e of document.querySelectorAll('a[href],button,input:not([type=password]):not([type=hidden]),textarea,select,[role=button]')){
            if(elements.length>=60)break;if(!visible(e)||e.disabled)continue;
            if(e.matches('input,textarea')&&(/pass|pwd|token|secret|otp|card|credit|cvv|senha|cpf|payment|auth/i.test([e.name,e.id,e.autocomplete,e.getAttribute('aria-label')].join(' '))))continue;
            const id=String(elements.length+1);e.setAttribute('data-clean-ai',TOKEN+':'+id);
            const field=e.matches('input,textarea,select');
            const label=e.getAttribute('aria-label')||(field?(e.getAttribute('placeholder')||e.name||e.tagName):(e.innerText||e.tagName));
            elements.push({id,label:label.slice(0,140),kind:e.tagName.toLowerCase()});
          }
          return {title:document.title,url:location.href,text:text.slice(0,12000),elements};
        })()
        """;
        script=script.Replace("TOKEN",JsonSerializer.Serialize(token));
        var raw=await core.ExecuteScriptAsync(script).WaitAsync(cancel);
        if(Current!=tab||core.Source!=source)throw new InvalidOperationException("P�gina mudou durante a leitura. Leia novamente.");
        using var page=JsonDocument.Parse(raw);
        if(page.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("P�gina n�o dispon�vel para leitura.");
        foreach(var e in page.RootElement.GetProperty("elements").EnumerateArray())snapshotElements[e.GetProperty("id").GetString()!]=(e.GetProperty("label").GetString()!,e.GetProperty("kind").GetString()!);
        snapshotTab=tab;snapshotToken=token;snapshotSource=source;
        return raw;
    }
    async Task<string> ExecuteAiTool(string name,JsonElement args,bool share,CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        if(name is "list_tabs" or "read_page")
        {
            if(!share)return "Compartilhamento desativado. Pe�a ao usu�rio para ativar antes de um novo pedido.";
            return name=="list_tabs"?TabsContext():await ReadAiPage(cancel);
        }
        AiPolicy.ValidateAction(name,args);
        string Str(string key)=>args.GetProperty(key).GetString()!;
        var tab=Current;var core=tab?.View?.CoreWebView2;
        string? selector=null;string description;
        if(name is "click" or "fill")
        {
            if(!share)throw new InvalidOperationException("Compartilhamento desativado.");
            if(tab!=snapshotTab||core==null||snapshotSource!=core.Source||!snapshotElements.TryGetValue(Str("element"),out var element))throw new InvalidOperationException("Elemento expirado. Use read_page novamente.");
            if(name=="fill" && element.kind is not ("input" or "textarea"))throw new InvalidOperationException("Elemento n�o � campo de texto.");
            selector="[data-clean-ai=\""+snapshotToken+":"+Str("element")+"\"]";
            description=name=="click"?"Clicar em: "+element.label:"Preencher: "+element.label+"\nTexto: "+Str("text");
            description+="\n\nP�gina: "+core.Source;
        }
        else description=name switch{"open_tab"=>"Abrir nova aba:\n"+Str("url"),"select_tab"=>"Selecionar aba "+args.GetProperty("index").GetInt32(),_=>"Rolar p�gina "+(Str("direction")=="down"?"para baixo":"para cima")};
        assistant?.Add("A��o proposta",description);
        if(ShowNotice(description+"\n\nAutorizar esta a��o?","A��o da IA",MessageBoxButtons.YesNo)!=DialogResult.Yes){cancel.ThrowIfCancellationRequested();throw new InvalidOperationException("Usu�rio recusou. N�o tente esta a��o novamente.");}
        cancel.ThrowIfCancellationRequested();
        if(name=="open_tab"){ResetSnapshot();await AddTab(Str("url"));return "Nova aba aberta. Aguarde carregar e use read_page para conferir.";}
        if(name=="select_tab")
        {
            int index=args.GetProperty("index").GetInt32()-1;
            if(index<0||index>=tabs.Count)throw new InvalidOperationException("Aba n�o existe.");
            ResetSnapshot();if(tabs[index].Page.WorkspaceId!=WorkspaceId)throw new InvalidOperationException("Aba fora do espa�o compartilhado.");tabStrip.SelectedTab=tabs[index].Page;return "Aba selecionada.";
        }
        if(core==null||tab!=Current)throw new InvalidOperationException("Aba mudou ou est� indispon�vel.");
        if(name=="scroll"){await core.ExecuteScriptAsync("window.scrollBy({top:"+(Str("direction")=="down"?"1":"-1")+"*window.innerHeight*0.75,behavior:'auto'})").WaitAsync(cancel);return "P�gina rolada.";}
        if(core.Source!=snapshotSource)throw new InvalidOperationException("P�gina mudou. Leia novamente.");
        string action=name=="fill"?"fill":"click";
        string js="""
        (()=>{const e=document.querySelector(SELECTOR);if(!e||!e.isConnected||e.disabled)return 'Elemento indispon�vel';
        const r=e.getBoundingClientRect();if(r.width<=0||r.height<=0)return 'Elemento oculto';
        if(ACTION==='fill'){
          if(!e.matches('input,textarea')||e.readOnly||/password|hidden|file/i.test(e.type)||/pass|pwd|token|secret|otp|card|credit|cvv|senha|cpf|payment|auth/i.test([e.name,e.id,e.autocomplete,e.getAttribute('aria-label')].join(' ')))return 'Campo protegido';
          const proto=e.tagName==='TEXTAREA'?HTMLTextAreaElement.prototype:HTMLInputElement.prototype;
          Object.getOwnPropertyDescriptor(proto,'value').set.call(e,VALUE);e.dispatchEvent(new Event('input',{bubbles:true}));e.dispatchEvent(new Event('change',{bubbles:true}));return 'Campo preenchido sem submeter';
        }
        e.click();return 'Clique executado. Leia novamente para conferir o resultado';})()
        """;
        js=js.Replace("SELECTOR",JsonSerializer.Serialize(selector)).Replace("ACTION",JsonSerializer.Serialize(action)).Replace("VALUE",JsonSerializer.Serialize(name=="fill"?Str("text"):""));
        ResetSnapshot();return await core.ExecuteScriptAsync(js).WaitAsync(cancel);
    }
}

