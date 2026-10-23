using KayBrowser;
using System.Net;
using System.Text;
using System.Text.Json;
static class IntegratedAiTests
{
    sealed class Fake(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>send(r,c);}
    static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text,Encoding.UTF8,"application/json")};
    public static async Task<int> Run()
    {
        int count=0;void Check(bool ok){if(!ok)throw new Exception("Falha de chat multimodal integrado");count++;}
        Check(AiPolicy.CleanKey("  test-key  ")=="test-key");foreach(var badKey in new[]{"Bearer key","key\r\nX: value",new string('x',4097)}){try{AiPolicy.CleanKey(badKey);throw new Exception("Chave inválida aceita");}catch(ArgumentException){count++;}}
        Check(AiConnection.Create("OpenRouter",AiConnection.RouterUrl,"openai/gpt-4o-mini").Responses==false);
        Check(AiConnection.Create("Local","http://localhost:11434/v1/chat/completions","model:vision").Url.StartsWith("http://localhost"));
        foreach(var url in new[]{"http://example.com/v1/chat/completions","https://user:password@example.com/v1/responses","https://example.com/v1/chat/completions?key=SECRET","https://example.com/v1/chat/completions#fragment","file:///v1/responses","https://example.com/v1/models"}){try{AiConnection.Create("custom",url,"model");throw new Exception("Endereço inválido aceito");}catch(ArgumentException){count++;}}
        var history=new AiConversation();for(int i=0;i<40;i++)history.Complete("pergunta "+i,new string('x',3500));Check(history.Snapshot().Length<=12&&history.Snapshot().Sum(t=>t.Text.Length)<=32000);Check(history.Snapshot().First().Role=="user");history.Clear();Check(history.Snapshot().Length==0);
        var image=new AiAttachment("Captura de teste","data:image/jpeg;base64,"+Convert.ToBase64String(new byte[]{1,2,3}));Check(image.ValidDataUrl().StartsWith("data:image/jpeg;base64,"));
        foreach(var invalid in new[]{"https://remote.example/image.jpg","data:text/html;base64,AAAA","data:image/jpeg;base64,!"}){try{new AiAttachment("bad",invalid).ValidDataUrl();throw new Exception("Imagem inválida aceita");}catch(ArgumentException){count++;}}
        int requestCount=0;var answers=new List<string>();
        using(var http=new HttpClient(new Fake(async(r,c)=>{requestCount++;using var json=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(c));var b=json.RootElement;Check(r.RequestUri!.AbsoluteUri==AiConnection.OpenAiUrl);Check(b.GetProperty("input")[0].GetProperty("content").GetString()=="anterior");Check(b.GetProperty("input")[1].GetProperty("content").GetString()=="resposta anterior");var parts=b.GetProperty("input")[2].GetProperty("content");Check(parts[1].GetProperty("type").GetString()=="input_image"&&parts[1].GetProperty("image_url").GetString()==image.DataUrl);Check(!b.GetProperty("store").GetBoolean()&&b.GetProperty("tools").GetArrayLength()==0);return Json("""{"status":"completed","output":[{"type":"message","role":"assistant","content":[{"type":"output_text","text":"Vi sua imagem"}]}]}""");})))
        {await new AiClient(http).Run("fake","gpt-5.4-mini","veja","",(_,_,_)=>throw new Exception("Não deve executar ações"),answers.Add,CancellationToken.None,history:[new("user","anterior"),new("assistant","resposta anterior")],image:image,enableTools:false);Check(answers.Single()=="Vi sua imagem"&&requestCount==1);}
        var connection=AiConnection.Create("OpenRouter",AiConnection.RouterUrl,"openai/gpt-4o-mini");int steps=0,executed=0;
        using(var http=new HttpClient(new Fake(async(r,c)=>{steps++;Check(r.RequestUri!.AbsoluteUri==connection.Url);Check(r.Headers.Authorization!.Parameter=="router-key");using var doc=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(c));var b=doc.RootElement;Check(b.GetProperty("model").GetString()==connection.Model);Check(!b.GetProperty("stream").GetBoolean());Check(b.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString()=="list_tabs");
            if(steps==1){var parts=b.GetProperty("messages")[1].GetProperty("content");Check(parts[1].GetProperty("image_url").GetProperty("url").GetString()==image.DataUrl);return Json("""{"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"t1","type":"function","function":{"name":"open_tab","arguments":"{\"url\":\"https://example.com\"}"}}]},"finish_reason":"tool_calls"}]}""");}
            Check(b.GetProperty("messages")[3].GetProperty("role").GetString()=="tool");Check(b.GetProperty("messages")[3].GetProperty("content").GetString()=="opened");return Json("""{"choices":[{"message":{"role":"assistant","content":"Pronto"},"finish_reason":"stop"}]}""");})))
        {var messages=new List<string>();await new AiClient(http).Run("router-key",connection.Model,"abra","",(_,_,_)=>{executed++;return Task.FromResult("opened");},messages.Add,CancellationToken.None,connection,image:image);Check(steps==2&&executed==1&&messages.Single()=="Pronto");}
        using(var http=new HttpClient(new Fake(async(r,c)=>{using var doc=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(c));Check(!doc.RootElement.TryGetProperty("tools",out _));return new HttpResponseMessage(HttpStatusCode.BadRequest){Content=new StringContent("SECRET_DO_NOT_EXPOSE")};})))
        {try{await new AiClient(http).Run("k",connection.Model,"test","",(_,_,_)=>Task.FromResult(""),_=>{},CancellationToken.None,connection,enableTools:false);throw new Exception("HTTP ignorado");}catch(IOException ex){Check(!ex.Message.Contains("SECRET"));}}
        using(var cancel=new CancellationTokenSource())using(var http=new HttpClient(new Fake((r,c)=>throw new Exception("Rede não deveria ser chamada")))){cancel.Cancel();try{await new AiClient(http).Run("k",connection.Model,"test","",(_,_,_)=>Task.FromResult(""),_=>{},cancel.Token,connection);throw new Exception("Cancelamento ignorado");}catch(OperationCanceledException){count++;}}
        return count;
    }
}
