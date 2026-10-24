using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KayBrowser;


public static class Updater
{
    public static Version CurrentVersion => typeof(Updater).Assembly.GetName().Version ?? new Version(0, 1, 0);
    public static bool ValidRepository(string repo)
    {
        if (!Regex.IsMatch(repo, @"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?/[A-Za-z0-9_.-]{1,100}\z")) return false;
        return repo.Split('/')[1] is not ("." or "..");
    }
    static HttpClient Client()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) }; c.DefaultRequestHeaders.UserAgent.ParseAdd("CleanBrowser/" + CurrentVersion.ToString(3)); c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json"); return c;
    }
    public static async Task<ReleaseUpdate?> Check(string repo, CancellationToken cancellation = default, Version? installedVersion = null)
    {
        if (!ValidRepository(repo)) throw new ArgumentException("Reposit�rio inv�lido.");
        using var http = Client();
        using var response = await http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest", cancellation);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw new IOException("Este reposit�rio n�o tem uma release p�blica dispon�vel.");
        response.EnsureSuccessStatusCode(); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        string version = json.RootElement.GetProperty("tag_name").GetString()!.TrimStart('v');
        if (!System.Version.TryParse(version, out var next)) throw new IOException("Vers�o da release inv�lida.");
        if (next <= (installedVersion ?? CurrentVersion)) return null;
        string? exe = null, hash = null; long size = 0;
        foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
        {
            var url = asset.GetProperty("browser_download_url").GetString()!;
            if (!url.StartsWith($"https://github.com/{repo}/releases/download/", StringComparison.OrdinalIgnoreCase)) continue;
            if (asset.GetProperty("name").GetString() == "KayBrowser.exe") { exe = url; size = asset.GetProperty("size").GetInt64(); }
            if (asset.GetProperty("name").GetString() == "KayBrowser.exe.sha256") hash = url;
        }
        if (exe == null || hash == null) throw new IOException("Release sem KayBrowser.exe e/ou KayBrowser.exe.sha256.");
        if (size < 1024 || size > 300L * 1024 * 1024) throw new IOException("Tamanho da atualiza��o inv�lido.");
        return new(version, exe, hash, null, size);
    }
    public static async Task<ReleaseUpdate?> CheckFeed(string url, CancellationToken cancellation = default)
    {
        if (!UpdateManifest.ValidFeed(url)) throw new IOException("Configure o endere�o HTTPS do servidor de atualiza��es.");
        using var http = Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        using var response = await http.SendAsync(request, cancellation);
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri?.Host != new Uri(url).Host) throw new IOException("O servidor redirecionou a atualiza��o para outro dom�nio.");
        var json = await response.Content.ReadAsStringAsync(cancellation);
        if (json.Length > 65536) throw new IOException("Resposta de atualiza��o inv�lida.");
        return UpdateManifest.Parse(json, new Uri(url), CurrentVersion);
    }
    public static void CleanupOldDownloads()
    {
        var root=Path.Combine(Storage.Root,"Updates");
        if(!Directory.Exists(root))return;
        try
        {
            foreach(var dir in new DirectoryInfo(root).GetDirectories())
            {
                if(!Guid.TryParseExact(dir.Name,"N",out _)||
                   (dir.Attributes&FileAttributes.ReparsePoint)!=0||
                   dir.LastWriteTimeUtc>DateTime.UtcNow.AddDays(-7))continue;
                try
                {
                    // Delete only files owned by the updater, never recurse through links.
                    foreach(var file in dir.GetFiles())
                        if(file.Name=="KayBrowser.exe"&&(file.Attributes&FileAttributes.ReparsePoint)==0)file.Delete();
                    if(!dir.EnumerateFileSystemInfos().Any())dir.Delete();
                }
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
            }
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
    }
    public static async Task Install(ReleaseUpdate update, IProgress<int>? progress = null, CancellationToken cancellation = default, bool diagnostics = false)
    {
        using var http = Client();
        string expected = update.Sha256 ?? (await http.GetStringAsync(update.HashUrl!, cancellation)).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        if (!Regex.IsMatch(expected, @"\A[0-9a-fA-F]{64}\z")) throw new IOException("SHA-256 da release inv�lido.");
        CleanupOldDownloads();
        var dir = Path.Combine(Storage.Root, "Updates", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        string downloaded = Path.Combine(dir, "KayBrowser.exe");
        using (var response = await http.GetAsync(update.ExeUrl, HttpCompletionOption.ResponseHeadersRead, cancellation))
        {
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > 300 * 1024 * 1024) throw new IOException("Atualiza��o maior que o permitido.");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellation); await using var file = File.Create(downloaded);
            var buffer = new byte[81920]; long total = 0; int count;
            while ((count = await stream.ReadAsync(buffer, cancellation)) > 0) { total += count; if (total > 300 * 1024 * 1024) throw new IOException("Atualiza��o excedeu o tamanho permitido."); await file.WriteAsync(buffer.AsMemory(0, count), cancellation); long expectedSize = update.Size > 0 ? update.Size : response.Content.Headers.ContentLength ?? 0; if (expectedSize > 0) progress?.Report((int)Math.Min(98, total * 98 / expectedSize)); }
        }
        cancellation.ThrowIfCancellationRequested();
        if (update.Size > 0 && new FileInfo(downloaded).Length != update.Size) throw new IOException("Download incompleto. O navegador atual foi preservado.");
        await using (var stream = File.OpenRead(downloaded)) if (!Convert.ToHexString(await SHA256.HashDataAsync(stream)).Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new IOException("Hash n�o confere. Atualiza��o descartada.");
        using (var stream = File.OpenRead(downloaded)) if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z') throw new IOException("Arquivo n�o � um execut�vel Windows.");
        progress?.Report(99);
        string target = Environment.ProcessPath ?? throw new IOException("Execut�vel atual n�o encontrado.");
        // Test write access before asking the application to close; never elevate privileges.
        string probe = Path.Combine(Path.GetDirectoryName(target)!, ".kay-update-probe"); File.WriteAllText(probe, ""); File.Delete(probe);
        static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
        string restartArgs = diagnostics ? "--update-receipt" : "";
        string healthToken=Guid.NewGuid().ToString("N");
        string script = $$"""
        $ErrorActionPreference = 'Stop'
        $target = {{Quote(target)}}
        $source = {{Quote(downloaded)}}
        $backup = $target + '.previous'
        $receipt = Join-Path (Split-Path $target) '.clean-health-{{healthToken}}.json'
        $probeProcess = $null
        $restarted = $null
        $backedUp = $false
        Wait-Process -Id {{Environment.ProcessId}} -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 700
        try {
          Copy-Item -LiteralPath $target -Destination $backup -Force
          $backedUp = $true
          Copy-Item -LiteralPath $source -Destination $target -Force
          $probeProcess = Start-Process -FilePath $target -ArgumentList '--startup-check','{{healthToken}}' -WindowStyle Hidden -PassThru
          if (!$probeProcess.WaitForExit(60000)) { Stop-Process -Id $probeProcess.Id -Force; throw 'A nova vers�o n�o confirmou a abertura em 60 segundos.' }
          if ($probeProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $receipt)) { throw 'A nova vers�o falhou no teste de abertura.' }
          $health = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
          if ($health.version -ne {{Quote(update.Version)}} -or $health.status -ne 'healthy') { throw 'Confirma��o de vers�o inv�lida.' }
          $restartArgs = {{Quote(restartArgs)}}
          if ($restartArgs) { $restarted = Start-Process -FilePath $target -ArgumentList $restartArgs -WindowStyle Normal -PassThru }
          else {
            $restarted = Start-Process -FilePath $target -WindowStyle Normal -PassThru
            if ($restarted.WaitForExit(4000)) { throw 'A nova vers�o encerrou durante a inicializa��o.' }
          }
        } catch {
          if ($null -ne $probeProcess -and !$probeProcess.HasExited) { Stop-Process -Id $probeProcess.Id -Force -ErrorAction SilentlyContinue; $probeProcess.WaitForExit() }
          if ($null -ne $restarted -and !$restarted.HasExited) { Stop-Process -Id $restarted.Id -Force -ErrorAction SilentlyContinue; $restarted.WaitForExit() }
          if ($backedUp -and (Test-Path -LiteralPath $backup)) {
            Copy-Item -LiteralPath $backup -Destination $target -Force
            if (!{{(diagnostics?"$true":"$false")}}) { Start-Process -FilePath $target -WindowStyle Normal }
          }
          Add-Type -AssemblyName System.Windows.Forms
          [System.Windows.Forms.MessageBox]::Show('Atualizacao falhou. A versao anterior foi preservada. ' + $_.Exception.Message, 'Clean Browser')
        } finally {
          if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt -Force -ErrorAction SilentlyContinue }
        }
        """;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        cancellation.ThrowIfCancellationRequested();
        Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + encoded) { UseShellExecute = false, CreateNoWindow = true });
    }
}

