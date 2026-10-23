using System.Security.Cryptography;
using System.Text;

namespace KayBrowser;

public static class AiSecrets
{
    static string FilePath => Path.Combine(Storage.Root,"ai-key.bin");
    static readonly byte[] entropy=Encoding.UTF8.GetBytes("CleanBrowser.AI.v1");
    public static string Read()
    {
        if(!File.Exists(FilePath))return "";
        try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(FilePath),entropy,DataProtectionScope.CurrentUser)); }
        catch(CryptographicException){return "";}
    }
    public static void Save(string value)
    {
        if(string.IsNullOrWhiteSpace(value)){File.Delete(FilePath);return;}
        File.WriteAllBytes(FilePath+".tmp",ProtectedData.Protect(Encoding.UTF8.GetBytes(value.Trim()),entropy,DataProtectionScope.CurrentUser));
        File.Move(FilePath+".tmp",FilePath,true);
    }
}
