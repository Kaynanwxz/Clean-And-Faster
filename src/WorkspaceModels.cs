namespace KayBrowser;

public record ShortcutLink(string Id, string Name, string Url);
public sealed class Workspace
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Novo espaço";
    public List<string> Urls { get; set; } = [];
}
public record LibraryEntry(string Id, string Title, string Url, string Kind, string Text, DateTime Updated);
public sealed record VisualPreset(string Name,string Theme,bool Vertical,string Layout,string Background,bool Compact=false);
public static class VisualPresets
{
    public const string DefaultName="Cristal do Egeu";
    public const int DefaultRevision=2;
    public static void ApplyDefault(Preferences p)
    {
        Apply(p,DefaultName);p.Accent="#8FBBEF";p.ShowShortcuts=true;p.FrequentShortcuts=true;p.FocusMode=false;p.Animations=true;
    }
    public static readonly VisualPreset[] All = [
        new("Egeu Noturno","Escuro",false,"Editorial","Arte"),
        new("Mármore Claro","Claro",false,"Central","Suave"),
        new("Ágora Vertical","Claro",true,"Central","Suave"),
        new("Copiloto Azul","Claro",false,"Clássico","Arte"),
        new("Renascimento Editorial","Claro",false,"Editorial","Arte"),
        new("Horizonte Essencial","Claro",false,"Essencial","Sem arte",true),
        new("Arquipélago de Espaços","Claro",false,"Espaços","Suave"),
        new("Cristal do Egeu","Claro",false,"Essencial","Arte",true),
        new("Biblioteca Noturna","Escuro",true,"Biblioteca","Suave"),
        new("Atelier Personalizável","Claro",false,"Central","Arte")
    ];
    public static void Apply(Preferences p,string name)
    {
        var v=All.FirstOrDefault(x=>x.Name==name)??All[3];
        p.Visual=v.Name;p.DarkChrome=v.Name is not ("Mármore Claro" or "Cristal do Egeu" or "Horizonte Essencial");p.Theme=v.Theme;p.VerticalTabs=v.Vertical;p.HomeLayout=v.Layout;p.Background=v.Background;p.Compact=v.Compact;
    }
}
public static class WebLinks
{
    public static string Normalize(string value)
    {
        value=value.Trim();
        if(!value.Contains("://"))value="https://"+value;
        if(!Uri.TryCreate(value,UriKind.Absolute,out var u)||u.Scheme is not ("https" or "http")||u.UserInfo.Length>0||value.Length>4096)
            throw new ArgumentException("Use um endereço HTTP/HTTPS válido, sem usuário ou senha na URL.");
        return u.AbsoluteUri;
    }
}
