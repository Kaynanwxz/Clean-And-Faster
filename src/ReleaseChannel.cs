namespace KayBrowser;

public static class Distribution
{
    public const string DefaultRepository = "Kaynanwxz/Clean-And-Faster";
    public const string DefaultFeedUrl = "";
    public static string Repository(string? configured) => Updater.ValidRepository(configured ?? "") ? configured! : DefaultRepository;
}
