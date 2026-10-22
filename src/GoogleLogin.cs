using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KayBrowser;

public record GoogleProfile(string Name, string Email);
public static class GoogleLogin
{
    static readonly string ProfileFile = Path.Combine(Storage.Root, "google-profile.bin");
    public static GoogleProfile? ReadProfile()
    {
        try { return JsonSerializer.Deserialize<GoogleProfile>(ProtectedData.Unprotect(File.ReadAllBytes(ProfileFile), null, DataProtectionScope.CurrentUser)); }
        catch (Exception e) when (e is IOException or CryptographicException or JsonException) { return null; }
    }
    public static void SignOut() { if (File.Exists(ProfileFile)) File.Delete(ProfileFile); }
    static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static async Task<GoogleProfile> SignIn(string clientId, string clientSecret)
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var tcp = new TcpListener(IPAddress.Loopback, 0); tcp.Start(); int port = ((IPEndPoint)tcp.LocalEndpoint).Port; tcp.Stop();
        string redirect = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener(); listener.Prefixes.Add(redirect); listener.Start();
        var auth = "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", new Dictionary<string, string> {
            ["client_id"] = clientId, ["redirect_uri"] = redirect, ["response_type"] = "code", ["scope"] = "openid email profile", ["state"] = state,
            ["code_challenge_method"] = "S256", ["code_challenge"] = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
        }.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
        Process.Start(new ProcessStartInfo(auth) { UseShellExecute = true });
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
        var query = context.Request.QueryString;
        var valid = context.Request.HttpMethod == "GET" && query["state"] == state && !string.IsNullOrEmpty(query["code"]) && query["error"] == null;
        var bytes = Encoding.UTF8.GetBytes(valid ? "Login recebido. Pode voltar ao Kay Browser." : "Login recusado ou resposta invalida.");
        context.Response.ContentType = "text/plain; charset=utf-8"; context.Response.StatusCode = valid ? 200 : 400;
        await context.Response.OutputStream.WriteAsync(bytes, timeout.Token); context.Response.Close(); listener.Stop();
        if (!valid) throw new InvalidOperationException("Login cancelado ou resposta OAuth inválida.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var fields = new Dictionary<string, string> { ["client_id"] = clientId, ["code"] = query["code"]!, ["code_verifier"] = verifier, ["redirect_uri"] = redirect, ["grant_type"] = "authorization_code" };
        if (!string.IsNullOrEmpty(clientSecret)) fields["client_secret"] = clientSecret;
        using var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(fields), timeout.Token);
        if (!tokenResponse.IsSuccessStatusCode) throw new InvalidOperationException("O Google recusou a troca OAuth. Verifique o cadastro Desktop, ID, secret e usuários de teste.");
        using var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(timeout.Token));
        http.DefaultRequestHeaders.Authorization = new("Bearer", token.RootElement.GetProperty("access_token").GetString());
        using var infoResponse = await http.GetAsync("https://openidconnect.googleapis.com/v1/userinfo", timeout.Token); infoResponse.EnsureSuccessStatusCode();
        using var info = JsonDocument.Parse(await infoResponse.Content.ReadAsStringAsync(timeout.Token));
        if (!info.RootElement.GetProperty("email_verified").GetBoolean()) throw new InvalidOperationException("A conta precisa ter e-mail verificado.");
        var profile = new GoogleProfile(info.RootElement.GetProperty("name").GetString() ?? "Usuário", info.RootElement.GetProperty("email").GetString()!);
        File.WriteAllBytes(ProfileFile, ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(profile), null, DataProtectionScope.CurrentUser));
        // Access/refresh tokens are deliberately not persisted. The profile is local, not a cloud sync service.
        return profile;
    }
}
