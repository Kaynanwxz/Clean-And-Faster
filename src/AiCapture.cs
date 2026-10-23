using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
namespace KayBrowser;
public sealed record AiCaptureSource(string Kind,string Title,IntPtr Window,Rectangle Bounds)
{public override string ToString()=>Title;}
public static class AiCapture
{
    delegate bool EnumProc(IntPtr window,IntPtr parameter);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc callback,IntPtr parameter);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out RECT rect);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
    [StructLayout(LayoutKind.Sequential)]struct RECT{public int Left,Top,Right,Bottom;}
    static readonly SemaphoreSlim windowCapture=new(1,1);
    public static AiCaptureSource[] Sources(bool includeTab)
    {
        var list=new List<AiCaptureSource>();if(includeTab)list.Add(new("tab","Aba atual do navegador",IntPtr.Zero,Rectangle.Empty));
        EnumWindows((h,_)=>{if(!IsWindowVisible(h)||IsIconic(h))return true;var title=new StringBuilder(512);GetWindowText(h,title,512);if(title.Length==0||!GetWindowRect(h,out var r)||r.Right-r.Left<80||r.Bottom-r.Top<60)return true;list.Add(new("window","Janela · "+title,h,Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom)));return true;},IntPtr.Zero);
        foreach(var (screen,index) in Screen.AllScreens.Select((s,i)=>(s,i)))list.Add(new("screen","Tela "+(index+1)+(screen.Primary?" · principal":"")+" · "+screen.Bounds.Width+"×"+screen.Bounds.Height,IntPtr.Zero,screen.Bounds));
        return list.ToArray();
    }
    public static async Task<Bitmap> WindowAsync(IntPtr handle,CancellationToken cancellation)
    {
        if(!await windowCapture.WaitAsync(0,cancellation))throw new IOException("Há uma captura de janela em andamento. Aguarde ou escolha outra fonte.");
        var task=Task.Run(()=>{try{return Window(handle);}finally{windowCapture.Release();}},CancellationToken.None);
        try{return await task.WaitAsync(TimeSpan.FromSeconds(4),cancellation);}
        catch{_ = task.ContinueWith(t=>{if(t.Status==TaskStatus.RanToCompletion)t.Result.Dispose();_ = t.Exception;},TaskScheduler.Default);throw;}
    }
    static Bitmap Window(IntPtr handle)
    {
        if(!IsWindowVisible(handle)||IsIconic(handle)||!GetWindowRect(handle,out var r))throw new IOException("A janela foi fechada ou minimizada. Escolha novamente.");
        int width=r.Right-r.Left,height=r.Bottom-r.Top;ValidateSize(width,height);
        var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        try{using var g=Graphics.FromImage(bitmap);g.Clear(Color.White);IntPtr dc=g.GetHdc();bool captured;try{captured=PrintWindow(handle,dc,2);}finally{g.ReleaseHdc(dc);}if(!captured)throw new IOException("Esta janela não permite captura. Use Aba atual ou uma tela e confira a prévia.");return bitmap;}
        catch{bitmap.Dispose();throw;}
    }
    public static Bitmap ScreenImage(Rectangle bounds)
    {
        if(!Screen.AllScreens.Any(s=>s.Bounds==bounds))throw new IOException("A tela mudou. Abra o seletor novamente.");ValidateSize(bounds.Width,bounds.Height);var bitmap=new Bitmap(bounds.Width,bounds.Height);
        try{using var g=Graphics.FromImage(bitmap);g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size);return bitmap;}catch{bitmap.Dispose();throw;}
    }
    static void ValidateSize(int w,int h){if(w<1||h<1||(long)w*h>24_000_000)throw new IOException("A imagem excede 24 megapixels. Escolha uma janela menor.");}
    public static AiAttachment Encode(Image image,string label)
    {
        ValidateSize(image.Width,image.Height);double scale=Math.Min(1,1920d/Math.Max(image.Width,image.Height));using var resized=new Bitmap(Math.Max(1,(int)Math.Round(image.Width*scale)),Math.Max(1,(int)Math.Round(image.Height*scale)));
        using(var g=Graphics.FromImage(resized)){g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,new Rectangle(Point.Empty,resized.Size));}
        using var bytes=new MemoryStream();var codec=ImageCodecInfo.GetImageEncoders().Single(c=>c.FormatID==ImageFormat.Jpeg.Guid);using var parameters=new EncoderParameters(1);parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,85L);resized.Save(bytes,codec,parameters);
        if(bytes.Length>2_200_000)throw new IOException("A captura ficou muito grande. Recorte uma área menor.");
        return new(label,"data:image/jpeg;base64,"+Convert.ToBase64String(bytes.ToArray()));
    }
}
public sealed class AiCropPreview : Control,IThemed
{
    Bitmap? source;Point anchor,end;bool dragging;Rectangle selected;Palette colors=Palette.Create(true,Color.SteelBlue);
    public AiCropPreview(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);AccessibleName="Prévia da captura; arraste para recortar";}
    public void SetImage(Bitmap bitmap){source?.Dispose();source=bitmap;selected=Rectangle.Empty;Invalidate();}
    Rectangle Display=>source==null?Rectangle.Empty:Fit(source.Size,ClientRectangle);
    public static Rectangle Fit(Size image,Rectangle area){if(image.Width<1||image.Height<1||area.Width<1||area.Height<1)return Rectangle.Empty;double s=Math.Min((double)area.Width/image.Width,(double)area.Height/image.Height);int w=Math.Max(1,(int)Math.Round(image.Width*s)),h=Math.Max(1,(int)Math.Round(image.Height*s));return new(area.X+(area.Width-w)/2,area.Y+(area.Height-h)/2,w,h);}
    public static Rectangle MapCrop(Point first,Point last,Rectangle display,Size image)
    {
        if(display.Width<1||display.Height<1)return Rectangle.Empty;
        int left=Math.Clamp(Math.Min(first.X,last.X),display.Left,display.Right),right=Math.Clamp(Math.Max(first.X,last.X),display.Left,display.Right),top=Math.Clamp(Math.Min(first.Y,last.Y),display.Top,display.Bottom),bottom=Math.Clamp(Math.Max(first.Y,last.Y),display.Top,display.Bottom);
        if(right-left<4||bottom-top<4)return Rectangle.Empty;
        return Rectangle.Intersect(new Rectangle(0,0,image.Width,image.Height),Rectangle.FromLTRB((int)Math.Floor((left-display.Left)*(double)image.Width/display.Width),(int)Math.Floor((top-display.Top)*(double)image.Height/display.Height),(int)Math.Ceiling((right-display.Left)*(double)image.Width/display.Width),(int)Math.Ceiling((bottom-display.Top)*(double)image.Height/display.Height)));
    }
    public Bitmap CopySelection(){if(source==null)throw new IOException("Capture uma imagem primeiro.");return source.Clone(selected.IsEmpty?new Rectangle(Point.Empty,source.Size):selected,PixelFormat.Format32bppArgb);}
    public bool HasImage=>source!=null;
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Display.Contains(e.Location))return;anchor=end=e.Location;dragging=true;Capture=true;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){end=e.Location;Invalidate();}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(!dragging)return;dragging=false;Capture=false;end=e.Location;selected=MapCrop(anchor,end,Display,source!.Size);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.Clear(colors.Elevated);if(source==null){TextRenderer.DrawText(e.Graphics,"Escolha o que quer mostrar e clique em Capturar.",Font,ClientRectangle,colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);return;}e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(source,Display);if(dragging){var r=Rectangle.FromLTRB(Math.Min(anchor.X,end.X),Math.Min(anchor.Y,end.Y),Math.Max(anchor.X,end.X),Math.Max(anchor.Y,end.Y));using var pen=new Pen(colors.Accent,2);e.Graphics.DrawRectangle(pen,r);}else if(!selected.IsEmpty){var d=Display;var r=new Rectangle(d.Left+selected.X*d.Width/source.Width,d.Top+selected.Y*d.Height/source.Height,selected.Width*d.Width/source.Width,selected.Height*d.Height/source.Height);using var pen=new Pen(colors.Accent,2);e.Graphics.DrawRectangle(pen,r);}}
    public void SetPalette(Palette p){colors=p;Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)source?.Dispose();base.Dispose(disposing);}
}
