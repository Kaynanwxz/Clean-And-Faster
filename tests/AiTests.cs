using KayBrowser;
using System.Net;
using System.Text;
using System.Text.Json;

static class AiTests
{
    sealed class Fake(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send) : HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>send(r,c);}
    static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text,Encoding.UTF8,"application/json")};
    const string Done="""{"status":"completed","output":[{"type":"message","role":"assistant","content":[{"type":"output_text","text":"Concluído"}]}]}""";
    const string Call="""{"status":"completed","output":[{"type":"function_call","call_id":"call_1","name":"open_tab","arguments":"{\"url\":\"https://example.com\"}"}]}""";
    public static async Task<int> Run()
    {
        int n=0;void Check(bool ok){if(!ok)throw new Exception("Falha de integração IA");n++;}
        Check(AiPolicy.ValidUrl("https://example.com"));
        foreach(var url in new[]{"file:///secret","javascript:alert(1)","https://user:pass@example.com","ftp://site"})Check(!AiPolicy.ValidUrl(url));
        foreach(var json in new[]{"{\"url\":\"file:///secret\"}","{\"url\":\"https://u:p@example.com\"}"})
        {
            using var args=JsonDocument.Parse(json);try{AiPolicy.ValidateAction("open_tab",args.RootElement);throw new Exception("URL perigosa aceita");}catch(InvalidOperationException){n++;}
        }
        using(var args=JsonDocument.Parse("{\"element\":\"1');alert(1)//\"}"))
        {try{AiPolicy.ValidateAction("click",args.RootElement);throw new Exception("ID inválido aceito");}catch(InvalidOperationException){n++;}}
        using(var args=JsonDocument.Parse("{}"))
        {try{AiPolicy.ValidateAction("eval",args.RootElement);throw new Exception("Ferramenta inválida aceita");}catch(InvalidOperationException){n++;}}
        int requests=0,actions=0;var messages=new List<string>();
        using(var http=new HttpClient(new Fake(async (r,c)=>{
            requests++;Check(r.RequestUri==AiClient.Endpoint);Check(r.Headers.Authorization?.Parameter=="test-key");
            using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(c));var root=body.RootElement;
            Check(root.GetProperty("include")[0].GetString()=="reasoning.encrypted_content");Check(!root.GetProperty("store").GetBoolean());Check(!root.GetProperty("parallel_tool_calls").GetBoolean());
            Check(root.GetProperty("max_output_tokens").GetInt32()==1800);Check(root.GetProperty("tools").GetArrayLength()==7);
            if(requests==2)Check(root.GetProperty("input").EnumerateArray().Any(e=>e.TryGetProperty("type",out var t)&&t.GetString()=="function_call_output"&&e.GetProperty("output").GetString()=="opened"));
            return Json(requests==1?Call:Done);
        })))
        {
            await new AiClient(http).Run("test-key","gpt-5.4-mini","Abra example","autorizado",(name,args,c)=>{actions++;Check(name=="open_tab");return Task.FromResult("opened");},messages.Add,CancellationToken.None);
            Check(actions==1&&requests==2&&messages.Single()=="Concluído");
        }
        int repeated=0;
        using(var http=new HttpClient(new Fake((r,c)=>{repeated++;return Task.FromResult(Json(Call));})))
        {await new AiClient(http).Run("key","gpt-5.4-mini","test","",(n,a,c)=>Task.FromResult("refused"),_=>{},CancellationToken.None);Check(repeated==AiPolicy.MaxRounds);}
        using(var http=new HttpClient(new Fake((r,c)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized){Content=new StringContent("SECRET_DO_NOT_DISPLAY")}))))
        {try{await new AiClient(http).Run("key","gpt-5.4-mini","test","",(n,a,c)=>Task.FromResult(""),_=>{},CancellationToken.None);throw new Exception("Erro HTTP ignorado");}catch(IOException e){Check(!e.Message.Contains("SECRET"));}}
        using(var cancel=new CancellationTokenSource())using(var http=new HttpClient(new Fake((r,c)=>throw new Exception("Não deveria chamar rede"))))
        {cancel.Cancel();try{await new AiClient(http).Run("key","gpt-5.4-mini","test","",(n,a,c)=>Task.FromResult(""),_=>{},cancel.Token);throw new Exception("Cancelamento ignorado");}catch(OperationCanceledException){n++;}}
        return n;
    }
}
