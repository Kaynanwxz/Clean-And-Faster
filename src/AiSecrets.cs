using System.Security.Cryptography;
using System.Text;
namespace KayBrowser;
public static class AiSecrets
{
    static string FilePath(string endpoint)=>endpoint==AiConnection.OpenAiUrl?Path.Combine(Storage.Root,"ai-key.bin"):Path.Combine(Storage.Root,"AI","keys",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)))+".bin");
    static readonly byte[] entropy=Encoding.UTF8.GetBytes("CleanBrowser.AI.v1");
    public static string Read(string endpoint=AiConnection.OpenAiUrl)
    {
        string file=FilePath(endpoint);if(!File.Exists(file))return "";
        try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(file),entropy,DataProtectionScope.CurrentUser));}catch(CryptographicException){return "";}
    }
    public static void Save(string value,string endpoint=AiConnection.OpenAiUrl)
    {
        string file=FilePath(endpoint);if(string.IsNullOrWhiteSpace(value)){File.Delete(file);return;}
        value=AiPolicy.CleanKey(value);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file+".tmp",ProtectedData.Protect(Encoding.UTF8.GetBytes(value.Trim()),entropy,DataProtectionScope.CurrentUser));File.Move(file+".tmp",file,true);
    }
}
