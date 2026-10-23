namespace KayBrowser;

public sealed record WebAppLink(string Id,string Name,string Url);
public static class WebApps
{
    public static List<WebAppLink> Defaults()=>[
        new("spotify","Spotify","https://open.spotify.com/"),new("youtube","YouTube","https://www.youtube.com/"),
        new("notion","Notion","https://www.notion.com/login"),new("gmail","Gmail","https://mail.google.com/"),new("github","GitHub","https://github.com/")];
    public static WebAppLink Create(string name,string url,string? id=null)
    {
        name=(name??"").Trim();if(name.Length is <1 or >40||name.Any(char.IsControl))throw new ArgumentException("Use um nome de 1 a 40 caracteres.");
        url=WebLinks.Normalize(url);var uri=new Uri(url);
        if(uri.Scheme!="https")throw new ArgumentException("Use HTTPS para o aplicativo.");
        return new(id??Guid.NewGuid().ToString("N"),name,url);
    }
    public static List<WebAppLink> Sanitize(IEnumerable<WebAppLink>? apps)=>(apps??[]).Where(a=>a!=null&&!string.IsNullOrWhiteSpace(a.Id)).Select(a=>{try{return Create(a.Name,a.Url,a.Id);}catch(ArgumentException){return null;}}).OfType<WebAppLink>().DistinctBy(a=>a.Id).Take(20).ToList();
    public static bool SameSite(string url,string entry)=>Uri.TryCreate(url,UriKind.Absolute,out var a)&&Uri.TryCreate(entry,UriKind.Absolute,out var b)&&a.Scheme==b.Scheme&&a.IdnHost.Equals(b.IdnHost,StringComparison.OrdinalIgnoreCase)&&a.Port==b.Port;
}
