using System.Net;
using System.Text;
using System.Text.Json;
namespace KayBrowser;
public sealed partial class BrowserWindow
{
    sealed class AiDiagnosticHandler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>send(r,c);}
    internal async Task<int> VerifyIntegratedAi()
    {
        int checks=0;void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
        var palette=Palette.Create(true,Accent);if(!assistantOpen)ToggleAssistant();await Task.Delay(40);assistant!.Reset();
        Check(!assistant.Share&&!assistant.BrowserActions,"IA compartilhou contexto sem escolha");
        using(var bitmap=new Bitmap(3840,2160))
        {
            using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Coral);
            var attachment=AiCapture.Encode(bitmap,"Imagem de diagnóstico");using var bytes=new MemoryStream(Convert.FromBase64String(attachment.DataUrl[23..]));using var decoded=Image.FromStream(bytes);Check(decoded.Width==1920&&decoded.Height==1080,"Captura não foi limitada proporcionalmente a 1920 px");assistant.SetAttachment(attachment);Check(assistant.Attachment==attachment,"Anexo não chegou ao painel");
            Check(AppearancePanel.Descendants(assistant).OfType<RichTextBox>().Single().Height>=100,"Conversa perdeu espaço útil em janela pequena");
            foreach(bool light in new[]{true,false}){var p=Palette.Create(light,Accent);assistant.SetPalette(p);Check(assistant.BackColor==p.Surface,"IA divergiu do tema");var text=AppearancePanel.Descendants(assistant).OfType<RichTextBox>().Single();Check(text.BackColor==p.Surface&&text.ForeColor==p.Text&&text.ReadOnly,"Conversa não acompanhou o tema");}
            assistant.SetPalette(palette);SaveChromeScreenshot("layout-ai-attachment.png");assistant.SetAttachment(null);Check(assistant.Attachment==null,"Remover imagem não removeu anexo");
        }
        var display=AiCropPreview.Fit(new Size(400,200),new Rectangle(0,0,300,300));Check(display==new Rectangle(0,75,300,150),"Prévia distorceu imagem");Check(AiCropPreview.MapCrop(new Point(150,75),new Point(300,225),display,new Size(400,200))==new Rectangle(200,0,200,200),"Recorte não mapeou os pixels corretos");
        Check(AiCropPreview.MapCrop(new Point(-100,-100),new Point(500,500),display,new Size(400,200))==new Rectangle(0,0,400,200),"Recorte vazou da imagem");
        // Only this explicitly created fixture window is captured; no desktop or user window.
        using var fixture=new Form{Text="Clean AI · janela sintética de teste",BackColor=Color.Magenta,Size=new Size(300,200),StartPosition=FormStartPosition.Manual,Location=new Point(10,10)};
        fixture.Show(this);await Task.Delay(100);
        var chosen=new AiCaptureSource("window",fixture.Text,fixture.Handle,fixture.Bounds);
        AiAttachment captured;
        using(var picker=new AiCaptureDialog(palette,_=>throw new Exception("Fonte errada"),[chosen]))
        {
            picker.Show(this);Check(!picker.Preview.HasImage&&picker.Attachment==null,"Seletor capturou antes de clicar");await picker.CaptureSelected();Check(picker.Preview.HasImage,"PrintWindow não capturou a janela escolhida");using(var image=picker.Preview.CopySelection()){Check(image.Width==fixture.Width&&image.Height==fixture.Height,"Captura incluiu outra região");var pixel=image.GetPixel(image.Width/2,image.Height/2);Check(pixel.R>200&&pixel.B>200&&pixel.G<80,"Captura não pertence à janela sintética escolhida");}
            AppearancePanel.Descendants(picker).OfType<Button>().Single(b=>b.AccessibleName=="Usar imagem da prévia").PerformClick();captured=picker.Attachment??throw new Exception("Imagem confirmada ausente");
        }
        fixture.Close();
        int unexpectedCapture=0;using(var canceled=new AiCaptureDialog(palette,_=>{unexpectedCapture++;throw new Exception("Não deveria capturar");},[new("tab","Aba de teste",IntPtr.Zero,Rectangle.Empty)])){canceled.Show(this);canceled.Close();Check(canceled.Attachment==null&&unexpectedCapture==0,"Cancelar captura leu pixels");}
        AiSecrets.Save("openai-test-key");AiSecrets.Save("router-test-key",AiConnection.RouterUrl);Check(AiSecrets.Read()=="openai-test-key"&&AiSecrets.Read(AiConnection.RouterUrl)=="router-test-key","Chave vazou entre provedores");AiSecrets.Save("",AiConnection.RouterUrl);
        const string done="""{"status":"completed","output":[{"type":"message","role":"assistant","content":[{"type":"output_text","text":"Conectado"}]}]}""";
        int requests=0;using(var http=new HttpClient(new AiDiagnosticHandler((r,c)=>{requests++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(done,Encoding.UTF8,"application/json")});})))
        using(var setup=new AiSetupDialog(store,palette,http))
        {
            setup.Show(this);await setup.TestConnection();Check(requests==1&&setup.ConnectionStatus.StartsWith("Conexão funcionando"),"Testar conexão não confirmou resposta");setup.Close();
        }
        var oldHttp=aiHttp;int turns=0;
        using(var http=new HttpClient(new AiDiagnosticHandler(async(r,c)=>{
            turns++;using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(c));var input=body.RootElement.GetProperty("input");Check(input[input.GetArrayLength()-1].GetProperty("content").ValueKind==JsonValueKind.Array,"Chat não enviou a imagem selecionada");if(turns==2)Check(input.GetArrayLength()==3&&input[0].GetProperty("role").GetString()=="user"&&input[1].GetProperty("role").GetString()=="assistant","Segundo envio perdeu o contexto da conversa");return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(done,Encoding.UTF8,"application/json")};
        })))
        {
            aiHttp=http;assistant.Reset();assistant.SetAttachment(captured);assistant.Prompt="Explique esta janela";await AskAssistant();Check(aiConversation.Snapshot().Length==2&&assistant.Attachment!=null,"Conversa perdeu contexto ou imagem após envio");assistant.Prompt="Continue a explicação";await AskAssistant();Check(turns==2&&aiConversation.Snapshot().Length==4,"Conversa contínua não foi preservada");assistant.Reset();Check(aiConversation.Snapshot().Length==0&&assistant.Attachment==null,"Nova conversa não limpou contexto e imagem");
        }
        aiHttp=oldHttp;AiSecrets.Save("");RefreshAiConnectionLabel();SaveChromeScreenshot("layout-ai-integrated.png");if(assistantOpen)ToggleAssistant();return checks;
    }
}
