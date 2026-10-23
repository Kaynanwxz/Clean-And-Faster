using System.Drawing.Drawing2D;
namespace KayBrowser;

// Local vector marks: no network request, bitmap enlargement or runtime SVG dependency.
// GitHub outline: Primer Octicons (MIT), see ThirdParty/Octicons-LICENSE.txt.
public static class BrandMarks
{
    static bool Is(string host,string domain)=>host==domain||host.EndsWith("."+domain,StringComparison.Ordinal);
    public static string? ShortName(string url)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri))return null;string h=uri.IdnHost.ToLowerInvariant();
        return Is(h,"google.com")||Is(h,"google.com.br")?"Google":Is(h,"github.com")?"GitHub":Is(h,"youtube.com")||h=="youtu.be"?"YouTube":null;
    }
    public static bool Draw(Graphics g,string url,Rectangle bounds,Color ink)
    {
        string? name=ShortName(url);if(name==null)return false;
        var state=g.Save();try
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(bounds.X,bounds.Y);
            g.ScaleTransform(bounds.Width/(name=="GitHub"?16f:24f),bounds.Height/(name=="GitHub"?16f:24f));
            void Fill(GraphicsPath shape,Color color){using var brush=new SolidBrush(color);g.FillPath(brush,shape);}
            if(name=="GitHub")Fill(GitHub,ink);
            else if(name=="Google")
            {Fill(GoogleBlue,Color.FromArgb(66,133,244));Fill(GoogleGreen,Color.FromArgb(52,168,83));Fill(GoogleYellow,Color.FromArgb(251,188,5));Fill(GoogleRed,Color.FromArgb(234,67,53));}
            else
            {using var box=Shapes.Round(new RectangleF(0,3.5f,24,17),4);Fill(box,Color.FromArgb(255,0,51));using var white=new SolidBrush(Color.White);g.FillPolygon(white,new PointF[]{new(9.5f,7.5f),new(9.5f,16.5f),new(17,12)});}
        }
        finally{g.Restore(state);}return true;
    }
    static readonly GraphicsPath GitHub=CreateGitHub();
    static GraphicsPath CreateGitHub()
    {
        var p=new GraphicsPath();
        p.StartFigure();
        p.AddBezier(6.766f,11.328f,4.703f,11.078f,3.25f,9.594f,3.25f,7.672f);
        p.AddBezier(3.25f,7.672f,3.25f,6.891f,3.531f,6.047f,4f,5.484f);
        p.AddBezier(4f,5.484f,3.797f,4.969f,3.828f,3.875f,4.063f,3.422f);
        p.AddBezier(4.063f,3.422f,4.688f,3.344f,5.531f,3.672f,6.031f,4.125f);
        p.AddBezier(6.031f,4.125f,6.625f,3.938f,7.25f,3.844f,8.016f,3.844f);
        p.AddBezier(8.016f,3.844f,8.781f,3.844f,9.406f,3.938f,9.969f,4.109f);
        p.AddBezier(9.969f,4.109f,10.453f,3.672f,11.313f,3.344f,11.938f,3.422f);
        p.AddBezier(11.938f,3.422f,12.156f,3.844f,12.188f,4.937f,11.984f,5.469f);
        p.AddBezier(11.984f,5.469f,12.484f,6.062f,12.75f,6.859f,12.75f,7.672f);
        p.AddBezier(12.75f,7.672f,12.75f,9.594f,11.297f,11.047f,9.203f,11.312f);
        p.AddBezier(9.203f,11.312f,9.734f,11.656f,10.093f,12.406f,10.093f,13.266f);
        p.AddLine(10.093f,13.266f,10.093f,14.891f);
        p.AddBezier(10.093f,14.891f,10.093f,15.359f,10.484f,15.625f,10.953f,15.438f);
        p.AddBezier(10.953f,15.438f,13.781f,14.359f,16f,11.53f,16f,8.03f);
        p.AddBezier(16f,8.03f,16f,3.61f,12.406f,0f,7.984f,0f);
        p.AddBezier(7.984f,0f,3.563f,0f,0f,3.61f,0f,8.031f);
        p.AddBezier(0f,8.031f,-0.00922f,11.34674f,2.05818f,14.31354f,5.172f,15.453f);
        p.AddBezier(5.172f,15.453f,5.594f,15.609f,6f,15.328f,6f,14.906f);
        p.AddLine(6f,14.906f,6f,13.656f);
        p.AddBezier(6f,13.656f,5.781f,13.75f,5.5f,13.812f,5.25f,13.812f);
        p.AddBezier(5.25f,13.812f,4.219f,13.812f,3.61f,13.25f,3.172f,12.203f);
        p.AddBezier(3.172f,12.203f,3f,11.781f,2.812f,11.531f,2.453f,11.484f);
        p.AddBezier(2.453f,11.484f,2.266f,11.469f,2.203f,11.391f,2.203f,11.297f);
        p.AddBezier(2.203f,11.297f,2.203f,11.109f,2.516f,10.969f,2.828f,10.969f);
        p.AddBezier(2.828f,10.969f,3.281f,10.969f,3.672f,11.25f,4.078f,11.829f);
        p.AddBezier(4.078f,11.829f,4.391f,12.281f,4.718f,12.484f,5.109f,12.484f);
        p.AddBezier(5.109f,12.484f,5.5f,12.484f,5.75f,12.344f,6.109f,11.984f);
        p.AddBezier(6.109f,11.984f,6.375f,11.719f,6.579f,11.484f,6.766f,11.328f);
        p.CloseFigure();
        return p;
    }
    static readonly GraphicsPath GoogleBlue=CreateGoogleBlue();
    static GraphicsPath CreateGoogleBlue()
    {
        var p=new GraphicsPath();
        p.StartFigure();
        p.AddBezier(23.49f,12.27f,23.49f,11.48f,23.42f,10.73f,23.3f,10f);
        p.AddLine(23.3f,10f,12f,10f);
        p.AddLine(12f,10f,12f,14.51f);
        p.AddLine(12f,14.51f,18.47f,14.51f);
        p.AddBezier(18.47f,14.51f,18.18f,15.99f,17.33f,17.24f,16.07f,18.09f);
        p.AddLine(16.07f,18.09f,19.95f,21.1f);
        p.AddBezier(19.95f,21.1f,22.21f,19.01f,23.49f,15.93f,23.49f,12.27f);
        p.AddLine(23.49f,12.27f,23.49f,12.27f);
        p.CloseFigure();
        return p;
    }
    static readonly GraphicsPath GoogleGreen=CreateGoogleGreen();
    static GraphicsPath CreateGoogleGreen()
    {
        var p=new GraphicsPath();
        p.StartFigure();
        p.AddBezier(12f,24f,15.24f,24f,17.95f,22.92f,19.93f,21.08f);
        p.AddLine(19.93f,21.08f,16.05f,18.07f);
        p.AddBezier(16.05f,18.07f,14.97f,18.79f,13.59f,19.23f,12f,19.23f);
        p.AddBezier(12f,19.23f,8.86f,19.23f,6.2f,17.12f,5.25f,14.28f);
        p.AddLine(5.25f,14.28f,1.26f,17.37f);
        p.AddBezier(1.26f,17.37f,3.24f,21.3f,7.31f,24f,12f,24f);
        p.CloseFigure();
        return p;
    }
    static readonly GraphicsPath GoogleYellow=CreateGoogleYellow();
    static GraphicsPath CreateGoogleYellow()
    {
        var p=new GraphicsPath();
        p.StartFigure();
        p.AddBezier(5.25f,14.28f,5f,13.56f,4.87f,12.79f,4.87f,12f);
        p.AddBezier(4.87f,12f,4.87f,11.21f,5.01f,10.44f,5.25f,9.72f);
        p.AddLine(5.25f,9.72f,1.26f,6.63f);
        p.AddBezier(1.26f,6.63f,0.45f,8.24f,0f,10.06f,0f,12f);
        p.AddBezier(0f,12f,0f,13.94f,0.45f,15.76f,1.26f,17.37f);
        p.AddLine(1.26f,17.37f,5.25f,14.28f);
        p.AddLine(5.25f,14.28f,5.25f,14.28f);
        p.CloseFigure();
        return p;
    }
    static readonly GraphicsPath GoogleRed=CreateGoogleRed();
    static GraphicsPath CreateGoogleRed()
    {
        var p=new GraphicsPath();
        p.StartFigure();
        p.AddBezier(12f,4.75f,13.77f,4.75f,15.35f,5.36f,16.6f,6.55f);
        p.AddLine(16.6f,6.55f,20.02f,3.13f);
        p.AddBezier(20.02f,3.13f,17.95f,1.19f,15.24f,0f,12f,0f);
        p.AddBezier(12f,0f,7.31f,0f,3.25f,2.69f,1.28f,6.61f);
        p.AddLine(1.28f,6.61f,5.26f,9.7f);
        p.AddBezier(5.26f,9.7f,6.2f,6.86f,8.86f,4.75f,12f,4.75f);
        p.CloseFigure();
        return p;
    }
}
