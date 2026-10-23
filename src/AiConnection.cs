namespace KayBrowser;

public sealed record AiConnection(string Provider,string Url,string Model)
{
    public const string OpenAiUrl="https://api.openai.com/v1/responses";
    public const string RouterUrl="https://openrouter.ai/api/v1/chat/completions";
    public bool Responses=>new Uri(Url).AbsolutePath.TrimEnd('/').EndsWith("/responses",StringComparison.OrdinalIgnoreCase);
    public static AiConnection From(Preferences p)=>Create(p.AiProvider,p.AiEndpoint,p.AiModel);
    public static AiConnection Create(string provider,string url,string model)
    {
        if(!Uri.TryCreate(url.Trim(),UriKind.Absolute,out var u)||u.UserInfo.Length>0||u.Query.Length>0||u.Fragment.Length>0||u.Scheme!="https"&&!(u.Scheme=="http"&&u.IsLoopback))
            throw new ArgumentException("Use o endereço HTTPS da API; HTTP só é permitido no próprio computador.");
        if(!u.AbsolutePath.TrimEnd('/').EndsWith("/responses",StringComparison.OrdinalIgnoreCase)&&!u.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions",StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Informe o endereço completo, terminando em /responses ou /chat/completions.");
        return new(provider,u.AbsoluteUri.TrimEnd('/'),AiPolicy.CleanModel(model));
    }
}
public sealed record AiTurn(string Role,string Text);
public sealed record AiAttachment(string Label,string DataUrl)
{
    public string ValidDataUrl()
    {
        if(DataUrl.Length>3_000_000||!DataUrl.StartsWith("data:image/jpeg;base64,",StringComparison.Ordinal))throw new ArgumentException("Imagem inválida ou acima do limite de envio.");
        try{Convert.FromBase64String(DataUrl[23..]);}catch(FormatException){throw new ArgumentException("Imagem inválida.");}
        return DataUrl;
    }
}
public sealed class AiConversation
{
    readonly List<AiTurn> turns=[];
    public AiTurn[] Snapshot()=>turns.ToArray();
    public void Complete(string question,string answer)
    {
        if(string.IsNullOrWhiteSpace(answer))return;
        turns.Add(new("user",question[..Math.Min(question.Length,4000)]));turns.Add(new("assistant",answer[..Math.Min(answer.Length,16000)]));
        while(turns.Count>12||turns.Sum(t=>t.Text.Length)>32000){turns.RemoveRange(0,2);}
    }
    public void Clear()=>turns.Clear();
}
