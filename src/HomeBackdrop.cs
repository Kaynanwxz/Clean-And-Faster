using System.Drawing.Drawing2D;

namespace KayBrowser;

// One shared raster for the visible viewport, not one full-screen bitmap per tab.
// Resampling the original engraving on every animation frame starves WM_TIMER.
internal static class HomeBackdrop
{
    static Bitmap? raster;
    static Image? source;
    static float alpha;
    static int paper;
    internal static void Draw(Graphics target,Image art,Rectangle viewport,float opacity,Color background)
    {
        if(viewport.Width<1||viewport.Height<1)return;
        if((long)viewport.Width*viewport.Height>24000000)
        {
            BrandAssets.DrawArt(target,art,BrandAssets.Cover(art.Size,viewport),opacity);return;
        }
        if(raster?.Size!=viewport.Size||!ReferenceEquals(source,art)||alpha!=opacity||paper!=background.ToArgb())
        {
            var next=new Bitmap(viewport.Width,viewport.Height);
            using(var g=Graphics.FromImage(next))
            {
                g.Clear(background);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                BrandAssets.DrawArt(g,art,BrandAssets.Cover(art.Size,new Rectangle(Point.Empty,viewport.Size)),opacity);
            }
            var old=raster;raster=next;source=art;alpha=opacity;paper=background.ToArgb();old?.Dispose();
        }
        target.DrawImageUnscaled(raster,viewport.Location);
    }
}
