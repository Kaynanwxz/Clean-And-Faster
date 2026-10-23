using System.Drawing.Drawing2D;
namespace KayBrowser;

// Render the approved composition as crisp stippling at the current physical viewport size.
// A shared single-entry cache avoids allocating a high-resolution bitmap for every new tab.
public static class PointillistArt
{
    static Bitmap? cache;
    public static Bitmap Render(Size size)
    {
        size=new Size(Math.Clamp(size.Width,1,3840),Math.Clamp(size.Height,1,2160));
        if(cache?.Size==size)return cache;
        var next=new Bitmap(size.Width,size.Height);using var g=Graphics.FromImage(next);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
        var source=BrandAssets.Sea;var cover=BrandAssets.Cover(source.Size,new Rectangle(Point.Empty,size));
        double step=Math.Max(3.2,Math.Sqrt((double)size.Width*size.Height/160000));
        using var brush=new SolidBrush(Color.Transparent);
        for(int row=0;row*step<size.Height;row++)for(int col=0;col*step<size.Width;col++)
        {
            double jitter=((col*37+row*17)%11-5)*.035;
            float x=(float)((col+.5+jitter)*step),y=(float)((row+.5-jitter)*step);
            int sx=Math.Clamp((int)((x-cover.X)*source.Width/cover.Width),0,source.Width-1),sy=Math.Clamp((int)((y-cover.Y)*source.Height/cover.Height),0,source.Height-1);
            Color pixel=source.GetPixel(sx,sy);if(pixel.A<16)continue;
            double shade=1-(pixel.R+pixel.G+pixel.B)/765d;float radius=(float)(step*(.19+.23*Math.Sqrt(shade)));
            int alpha=(int)(pixel.A*(.28+.72*shade));brush.Color=Color.FromArgb(alpha,38,105,171);g.FillEllipse(brush,x-radius,y-radius,radius*2,radius*2);
        }
        var old=cache;cache=next;old?.Dispose();return next;
    }
}
