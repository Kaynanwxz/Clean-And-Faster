using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task VerifyAgentDom()
    {
        string fixture=Path.Combine(Storage.Root,"agent-fixture");Directory.CreateDirectory(fixture);
        File.WriteAllText(Path.Combine(fixture,"index.html"),"""
        <!doctype html><html><head><meta charset="utf-8"></head><body><h1>Teste Clean AI</h1><p>Conteúdo autorizado da página.</p>
        <input id="ordinary" placeholder="Nome comum" value="FORM_VALUE_DO_NOT_READ">
        <input type="password" id="password" value="PASSWORD_DO_NOT_READ">
        <textarea id="secret-token">TOKEN_DO_NOT_READ</textarea>
        <textarea id="notes" placeholder="Anotações">TEXTAREA_VALUE_DO_NOT_READ</textarea>
        <button id="increment" onclick="document.querySelector('h1').textContent='Clique confirmado'">Contar</button>
        <p hidden>HIDDEN_DO_NOT_READ</p></body></html>
        """);
        environment=await CoreWebView2Environment.CreateAsync(null,Path.Combine(Storage.Root,"WebView"));
        var tab=Current??throw new InvalidOperationException("Aba de diagnóstico ausente.");
        await CreateView(tab,false);
        var core=tab.View?.CoreWebView2??throw new InvalidOperationException("WebView de diagnóstico indisponível.");
        core.SetVirtualHostNameToFolderMapping("clean-ai.test",fixture,CoreWebView2HostResourceAccessKind.DenyCors);
        var ready=new TaskCompletionSource<bool>();
        void Loaded(object? s,CoreWebView2NavigationCompletedEventArgs e){if(e.IsSuccess)ready.TrySetResult(true);else ready.TrySetException(new IOException("Fixture não carregou: "+e.WebErrorStatus));}
        core.NavigationCompleted+=Loaded;
        try{core.Navigate("https://clean-ai.test/index.html");await ready.Task.WaitAsync(TimeSpan.FromSeconds(25));}
        finally{core.NavigationCompleted-=Loaded;}
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var text=await ReadAiPage(deadline.Token);
        using var extracted=JsonDocument.Parse(text);
        if(!extracted.RootElement.GetProperty("text").GetString()!.Contains("Conteúdo autorizado")||text.Contains("DO_NOT_READ"))throw new InvalidOperationException("Extração da fixture falhou: "+text);
        string field=snapshotElements.Single(e=>e.Value.label=="Nome comum").Key;
        using var fill=JsonDocument.Parse(JsonSerializer.Serialize(new{element=field,text="Kaynan"}));
        // Exercise the real confirmation dialog on this isolated fixture, never on user pages.
        using var approve=new System.Windows.Forms.Timer{Interval=50};
        approve.Tick+=(_,_)=>{foreach(var d in Application.OpenForms.Cast<Form>().OfType<AppDialog>().ToArray())if(d.Text=="Ação da IA")d.AcceptButton?.PerformClick();};
        approve.Start();
        await ExecuteAiTool("fill",fill.RootElement,true,deadline.Token);
        string value=await core.ExecuteScriptAsync("document.querySelector('#ordinary').value");
        if(value!="\"Kaynan\"")throw new InvalidOperationException("Preenchimento não atingiu campo de teste.");
        await ReadAiPage(deadline.Token);
        string button=snapshotElements.Single(e=>e.Value.label=="Contar").Key;
        using var click=JsonDocument.Parse(JsonSerializer.Serialize(new{element=button}));
        await ExecuteAiTool("click",click.RootElement,true,deadline.Token);
        string result=await core.ExecuteScriptAsync("document.querySelector('h1').textContent");
        if(result!="\"Clique confirmado\"")throw new InvalidOperationException("Clique não atingiu botão de teste.");
        bool staleBlocked=false;
        try{await ExecuteAiTool("click",click.RootElement,true,deadline.Token);}catch(InvalidOperationException){staleBlocked=true;}
        if(!staleBlocked)throw new InvalidOperationException("Referência de elemento expirada foi aceita.");
        approve.Stop();
        AiSecrets.Save("diagnostic-only-not-an-api-key");
        if(AiSecrets.Read()!="diagnostic-only-not-an-api-key")throw new InvalidOperationException("Chave protegida não voltou íntegra.");
        AiSecrets.Save("");
        if(AiSecrets.Read()!="")throw new InvalidOperationException("Remoção de chave falhou.");
        tab.View.Dispose();tab.View=null;tab.Url="kay://new";ShowHome(tab);
    }
}
