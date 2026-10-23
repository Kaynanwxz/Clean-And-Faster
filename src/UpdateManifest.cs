using System.Text.Json;
using System.Text.RegularExpressions;

namespace KayBrowser;

public record ReleaseUpdate(string Version, string ExeUrl, string? HashUrl = null, string? Sha256 = null, long Size = 0);
public static class UpdateManifest
{
    public static bool ValidFeed(string value) => Uri.TryCreate(value, UriKind.Absolute, out var u) && u.Scheme == "https" && string.IsNullOrEmpty(u.UserInfo) && string.IsNullOrEmpty(u.Fragment);
    public static ReleaseUpdate? Parse(string json, Uri feed, Version current)
    {
        using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        var versionText = root.GetProperty("version").GetString() ?? "";
        if (!Version.TryParse(versionText, out var version)) throw new IOException("Versão inválida no servidor de atualizações.");
        var hash = root.GetProperty("sha256").GetString() ?? "";
        if (!Regex.IsMatch(hash, @"\A[0-9a-fA-F]{64}\z")) throw new IOException("Checksum inválido na atualização.");
        var urlText = root.GetProperty("url").GetString() ?? "";
        if (!Uri.TryCreate(feed, urlText, out var binary) || !ValidFeed(binary.AbsoluteUri) || binary.Host != feed.Host || binary.Port != feed.Port) throw new IOException("O download deve vir do mesmo servidor HTTPS da atualização.");
        long size = root.GetProperty("size").GetInt64();
        if (size < 1024 || size > 300L * 1024 * 1024) throw new IOException("Tamanho da atualização inválido.");
        if (version <= current) return null;
        return new(versionText, binary.AbsoluteUri, null, hash, size);
    }
}
