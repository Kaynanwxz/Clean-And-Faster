using System.Drawing.Imaging;

namespace KayBrowser;

public static class BrandAssets
{
    static Bitmap Load(string name)
    {
        using var stream=typeof(BrandAssets).Assembly.GetManifestResourceStream("CleanBrowser.Assets."+name)??
            typeof(BrandAssets).Assembly.GetManifestResourceNames().Where(x=>x.EndsWith("."+name)).Select(x=>typeof(BrandAssets).Assembly.GetManifestResourceStream(x)).FirstOrDefault()??throw new IOException("Arte não encontrada: "+name);
        using var source=Image.FromStream(stream);
        // Decode once at a bounded working resolution, shared by all tabs.
        int width=Math.Min(source.Width,name=="clean-logo.png"?256:1440);int height=source.Height*width/source.Width;
        var bitmap=new Bitmap(width,height);using var g=Graphics.FromImage(bitmap);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,width,height);return bitmap;
    }
    public static readonly Bitmap Logo=Load("clean-logo.png");
    public static readonly Bitmap Sea=Load("apollo-sea.png");
    public static void DrawArt(Graphics g,Image image,Rectangle bounds,float opacity=1)
    {
        if(bounds.Width<=0||bounds.Height<=0)return;
        using var attributes=new ImageAttributes();var matrix=new ColorMatrix{Matrix33=Math.Clamp(opacity,0,1)};attributes.SetColorMatrix(matrix);
        g.DrawImage(image,bounds,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
    }
    public static Palette Navy(Color accent)=>Palette.Create(false,accent);
}
