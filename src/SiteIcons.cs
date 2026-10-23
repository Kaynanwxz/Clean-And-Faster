using System.Security.Cryptography;
using System.Text;

namespace KayBrowser;

public static class SiteIcons
{
    static readonly Dictionary<string,Bitmap> cache=[];
    static string Host(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)?u.Host:"";
    static string FileName(string host)=>Path.Combine(Storage.Root,"Icons",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(host)))+".png");
    public static Image? Get(string url)
    {
        string host=Host(url);if(host.Length==0)return null;if(cache.TryGetValue(host,out var icon))return icon;
        string file=FileName(host);
        if(File.Exists(file))try{using var s=Image.FromFile(file);return Add(host,new Bitmap(s));}catch(Exception e)when(e is IOException or ArgumentException or OutOfMemoryException){}
        return null;
    }
    static Bitmap Add(string host,Bitmap bitmap)
    {
        if(cache.Remove(host,out var old))old.Dispose();
        if(cache.Count>=32){var first=cache.First();first.Value.Dispose();cache.Remove(first.Key);}
        cache[host]=bitmap;return bitmap;
    }
    public static void Save(string url,Stream stream)
    {
        string host=Host(url);if(host.Length==0)return;
        using var data=new MemoryStream();var buffer=new byte[8192];int n;while((n=stream.Read(buffer))>0){if(data.Length+n>262144)return;data.Write(buffer,0,n);}data.Position=0;
        using var source=Image.FromStream(data);if(source.Width>1024||source.Height>1024)return;
        var bitmap=new Bitmap(Math.Min(128,source.Width),Math.Min(128,source.Height));using(var g=Graphics.FromImage(bitmap)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,bitmap.Width,bitmap.Height);}
        string directory=Path.GetDirectoryName(FileName(host))!;Directory.CreateDirectory(directory);
        try{bitmap.Save(FileName(host),System.Drawing.Imaging.ImageFormat.Png);}catch{bitmap.Dispose();throw;}
        Add(host,bitmap);
        foreach(var old in new DirectoryInfo(directory).GetFiles("*.png").OrderByDescending(f=>f.LastWriteTimeUtc).Skip(128))try{old.Delete();}catch(IOException){}
    }
}
