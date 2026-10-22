using System.Text.Json;

namespace KayBrowser;

public record Visit(string Title, string Url, DateTime At);
public record Bookmark(string Title, string Url);
public record DownloadRecord(string Name, string Path, string State);
public sealed class Preferences
{
    public bool Animations { get; set; } = true;
    public string AiModel { get; set; } = "gpt-5.4-mini";
    public string Theme { get; set; } = "Escuro";
    public string Accent { get; set; } = "#A8E6CF";
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
    public Storage()
    {
        Directory.CreateDirectory(Root);
        Settings = Read("settings.json", new Preferences());
        Settings.IdleMinutes = Math.Clamp(Settings.IdleMinutes, 1, 60);
        Settings.MemoryBudgetMb = Math.Clamp(Settings.MemoryBudgetMb, 256, 32768);
        History = Read<List<Visit>>("history.json", []);
        Bookmarks = Read<List<Bookmark>>("bookmarks.json", []);
    }
    static T Read<T>(string name, T fallback)
    {
        var file = Path.Combine(Root, name);
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(file)) ?? fallback; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            if (File.Exists(file)) try { File.Copy(file, file + ".damaged", true); } catch (IOException) { }
            return fallback;
        }
    }
    static void Write<T>(string name, T value)
    {
        var target = Path.Combine(Root, name);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(target + ".tmp", target, true);
    }
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
