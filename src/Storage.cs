using System.Text.Json;

namespace KayBrowser;

public record Visit(string Title, string Url, DateTime At);
public record Bookmark(string Title, string Url);
public record DownloadRecord(string Name, string Path, string State);
public sealed class Preferences
{
    public List<WebAppLink> Apps { get; set; } = WebApps.Defaults();
    public bool Animations { get; set; } = true;
    public bool PointillistBackground { get; set; }
    public int HomeArtRevision { get; set; }
    public bool ShowBottomBar { get; set; } // Absent in existing profiles: hidden until explicitly enabled.
    public string AiModel { get; set; } = "gpt-5.4-mini";
    public string AiProvider { get; set; } = "OpenAI";
    public string AiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
    public int AppearanceRevision { get; set; }
    public string Visual { get; set; } = VisualPresets.DefaultName;
    public string HomeLayout { get; set; } = "Essencial";
    public string Background { get; set; } = "Arte";
    public string Wallpaper { get; set; } = "";
    public bool DarkChrome { get; set; }
    public bool VerticalTabs { get; set; }
    public bool Compact { get; set; } = true;
    public bool FocusMode { get; set; }
    public bool ShowShortcuts { get; set; } = true;
    public bool FrequentShortcuts { get; set; } = true;
    public string ActiveWorkspace { get; set; } = "";
    public List<Workspace> Workspaces { get; set; } = [];
    public List<ShortcutLink> Shortcuts { get; set; } = [
        new("google","Google","https://www.google.com/"),new("youtube","YouTube","https://www.youtube.com/"),new("github","GitHub","https://github.com/")];
    public string Theme { get; set; } = "Claro";
    public string Accent { get; set; } = "#8FBBEF";
    public string Search { get; set; } = "Google";
    public int IdleMinutes { get; set; } = 5;
    public int MemoryBudgetMb { get; set; } = 1500;
    public bool RestoreTabs { get; set; } = true;
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";
    public string UpdateFeedUrl { get; set; } = "";
    public string UpdateRepository { get; set; } = Distribution.DefaultRepository;
    public List<string> Session { get; set; } = [];
}

public sealed class Storage
{
    public static string Root { get; } = AppContext.GetData("KayBrowser.DiagnosticsRoot") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KayBrowser");
    public Preferences Settings { get; }
    public List<Visit> History { get; }
    public List<Bookmark> Bookmarks { get; }
    public List<LibraryEntry> Library { get; }
    public Storage()
    {
        Directory.CreateDirectory(Root);
        Settings = Read("settings.json", new Preferences());
        bool migrated=Settings.AppearanceRevision<VisualPresets.DefaultRevision;
        if(migrated){VisualPresets.ApplyDefault(Settings);Settings.AppearanceRevision=VisualPresets.DefaultRevision;}
        bool artMigrated=Settings.HomeArtRevision<1;
        if(artMigrated){Settings.PointillistBackground=false;Settings.HomeArtRevision=1;}
        Settings.Apps=WebApps.Sanitize(Settings.Apps);
        Settings.Shortcuts ??= [];
        Settings.Shortcuts=Settings.Shortcuts.Where(s=>s!=null&&Navigation.IsWebUrl(s.Url)&&!string.IsNullOrWhiteSpace(s.Name)).DistinctBy(s=>s.Id).Take(20).ToList();
        Settings.Workspaces ??= [];
        Settings.Workspaces=Settings.Workspaces.Where(w=>w!=null&&!string.IsNullOrWhiteSpace(w.Id)).DistinctBy(w=>w.Id).Take(12).ToList();
        if(Settings.Workspaces.Count==0){Settings.Workspaces=[new(){Name="Pessoal",Urls=Settings.Session??[]},new(){Name="Trabalho"},new(){Name="Estudo"}];}
        foreach(var w in Settings.Workspaces){w.Name=string.IsNullOrWhiteSpace(w.Name)?"Espaço":w.Name[..Math.Min(w.Name.Length,40)];w.Urls=(w.Urls??[]).Where(Navigation.IsWebUrl).Take(30).ToList();}
        if(!Settings.Workspaces.Any(w=>w.Id==Settings.ActiveWorkspace))Settings.ActiveWorkspace=Settings.Workspaces[0].Id;
        Settings.IdleMinutes = Math.Clamp(Settings.IdleMinutes, 1, 60);
        Settings.MemoryBudgetMb = Math.Clamp(Settings.MemoryBudgetMb, 256, 32768);
        History = Read<List<Visit>>("history.json", []);
        Bookmarks = Read<List<Bookmark>>("bookmarks.json", []);
        Library = Read<List<LibraryEntry>>("library.json", []).Where(e=>e!=null&&e.Kind is "reading" or "source" or "note").Take(500).ToList();
        if(migrated||artMigrated)SaveSettings();
    }
    static T Read<T>(string name, T fallback)
    {
        var file = Path.Combine(Root, name);
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(file)) ?? fallback; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            if (File.Exists(file)) try { File.Copy(file, file + ".damaged", true); } catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException) { }
            return fallback;
        }
    }
    readonly Dictionary<string,string> pendingWrites=[];
    public bool HasPendingWrites=>pendingWrites.Count>0;
    public string LastWriteError {get;private set;}="";
    public event Action<string>? WriteFailed;
    void Write<T>(string name,T value)
    {
        pendingWrites[name]=JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true});
        RetryPendingWrites();
    }
    public bool RetryPendingWrites()
    {
        foreach(var item in pendingWrites.ToArray())
        {
            var target=Path.Combine(Root,item.Key);
            try
            {
                File.WriteAllText(target+".tmp",item.Value);
                File.Move(target+".tmp",target,true);
                pendingWrites.Remove(item.Key);
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            {
                LastWriteError="Não foi possível salvar "+item.Key+". Os dados continuam na memória; uma nova tentativa será feita.";
                WriteFailed?.Invoke(LastWriteError);
            }
        }
        if(!HasPendingWrites)LastWriteError="";
        return !HasPendingWrites;
    }
    public void SaveLibrary() => Write("library.json", Library);
    public void SaveSettings() => Write("settings.json", Settings);
    public void SaveBookmarks() => Write("bookmarks.json", Bookmarks);
    public void SaveHistory() => Write("history.json", History);
    public void Record(string title, string url)
    {
        if (!Navigation.IsWebUrl(url)) return;
        History.Insert(0, new(title, url, DateTime.Now));
        if (History.Count > 10000) History.RemoveRange(10000, History.Count - 10000);
        SaveHistory();
    }
}

public static class Navigation
{
    public static bool IsAllowedExternal(string url) =>
        !string.IsNullOrWhiteSpace(url)&&url.Length<=4096&&!url.Any(char.IsControl)&&
        Uri.TryCreate(url,UriKind.Absolute,out var uri)&&(uri.Scheme=="mailto"||string.IsNullOrEmpty(uri.UserInfo))&&
        uri.Scheme is "mailto" or "tel" or "sms" or "spotify" or "discord";
    public static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "https" or "http";
    public static string Resolve(string input, string engine)
    {
        input = input.Trim();
        if (input == "" || input == "kay://new") return "kay://new";
        if (IsWebUrl(input)) return new Uri(input).AbsoluteUri;
        if (!input.Contains(' ') && !input.Contains("://") && (input.Contains('.') || input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            var candidate = (input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || input.StartsWith("127.0.0.1") ? "http://" : "https://") + input;
            if (IsWebUrl(candidate)) return new Uri(candidate).AbsoluteUri;
        }
        var prefix = engine switch { "DuckDuckGo" => "https://duckduckgo.com/?q=", "Bing" => "https://www.bing.com/search?q=", _ => "https://www.google.com/search?q=" };
        return prefix + Uri.EscapeDataString(input);
    }
}
