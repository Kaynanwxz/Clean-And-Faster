using System.Drawing.Imaging;

namespace KayBrowser;

public static class BrandAssets
{
    static Bitmap Load(string name)
    {
        using var stream=typeof(BrandAssets).Assembly.GetManifestResourceStream("CleanBrowser.Assets."+name)??
            typeof(BrandAssets).Assembly.GetManifestResourceNames().Where(x=>x.EndsWith("."+name)).Select(x=>typeof(BrandAssets).Assembly.GetManifestResourceStream(x)).FirstOrDefault()??throw new IOException("Arte não encontrada: "+name);
        using var source=Image.FromStream(stream);
        // Share a single decode across tabs. Preserve the original wallpaper pixels.
        int width=name=="clean-emblem.png"?Math.Min(source.Width,256):source.Width;int height=source.Height*width/source.Width;
        var bitmap=new Bitmap(width,height);using var g=Graphics.FromImage(bitmap);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,width,height);return bitmap;
    }
    static Bitmap? wallpaper;static string wallpaperPath="";
    public static Image? Wallpaper(string path)
    {
        path??="";if(path==wallpaperPath)return wallpaper;
        wallpaper?.Dispose();wallpaper=null;wallpaperPath=path;
        if(path.Length==0||!File.Exists(path))return null;
        try
        {
            using var source=Image.FromFile(path);
            if((long)source.Width*source.Height>24000000)throw new IOException("Imagem acima de 24 megapixels.");
            wallpaper=new Bitmap(source);return wallpaper;
        }
        catch(Exception e)when(e is IOException or ArgumentException or OutOfMemoryException){wallpaper?.Dispose();wallpaper=null;return null;}
    }
    public static readonly Bitmap Logo=Load("clean-emblem.png");
    public static readonly Bitmap Sea=Load("apollo-sea.png");
    public static Rectangle Cover(Size image,Rectangle viewport)
    {
        if(image.Width<=0||image.Height<=0||viewport.Width<=0||viewport.Height<=0)return Rectangle.Empty;
        double scale=Math.Max((double)viewport.Width/image.Width,(double)viewport.Height/image.Height);
        int width=(int)Math.Ceiling(image.Width*scale),height=(int)Math.Ceiling(image.Height*scale);
        return new(viewport.X+(viewport.Width-width)/2,viewport.Y+(viewport.Height-height)/2,width,height);
    }
    public static Icon ApplicationIcon
    {
        get {using var stream=typeof(BrandAssets).Assembly.GetManifestResourceStream("CleanBrowser.kay.ico")??throw new IOException("Ícone não encontrado.");return new Icon(stream);}
    }
    public static void DrawArt(Graphics g,Image image,Rectangle bounds,float opacity=1)
    {
        if(bounds.Width<=0||bounds.Height<=0)return;
        using var attributes=new ImageAttributes();var matrix=new ColorMatrix{Matrix33=Math.Clamp(opacity,0,1)};attributes.SetColorMatrix(matrix);
        g.DrawImage(image,bounds,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
    }
    public static Palette Navy(Color accent)=>Palette.Create(false,accent);
}
