using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KayBrowser;

public record Palette(Color Background, Color Surface, Color Elevated, Color Text, Color Muted, Color Line, Color Accent)
{
    public static Palette Create(bool light, Color accent) => light
        ? new(Color.FromArgb(244,247,249), Color.White, Color.FromArgb(233,239,242), Color.FromArgb(29,42,48), Color.FromArgb(96,114,121), Color.FromArgb(217,227,231), accent)
        : new(Color.FromArgb(15,20,25), Color.FromArgb(24,31,38), Color.FromArgb(34,44,53), Color.FromArgb(231,239,243), Color.FromArgb(143,162,175), Color.FromArgb(43,56,65), accent);
}
public interface IThemed { void SetPalette(Palette colors); }
public static class Shapes
{
    public static GraphicsPath Round(RectangleF r, float radius)
    {
        radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2); float d = radius * 2;
        var p = new GraphicsPath(); p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
}
public static class Glyphs
{
    // All chrome icons are drawn from the same 24-unit vector grid, independent of fonts.
    public static void Draw(Graphics g, string name, RectangleF bounds, Color color, float stroke = 1.7f)
    {
        var state=g.Save(); g.SmoothingMode=SmoothingMode.AntiAlias; g.TranslateTransform(bounds.X,bounds.Y); g.ScaleTransform(bounds.Width/24,bounds.Height/24);
        using var p=new Pen(color,stroke) { StartCap=LineCap.Round, EndCap=LineCap.Round, LineJoin=LineJoin.Round }; using var b=new SolidBrush(color);
        void Line(float a,float c,float d,float e)=>g.DrawLine(p,a,c,d,e);
        void Path(params PointF[] points)=>g.DrawLines(p,points);
        switch(name)
        {
            case "back": Path(new(14,5),new(7,12),new(14,19)); Line(7,12,21,12); break;
            case "forward": Path(new(10,5),new(17,12),new(10,19)); Line(3,12,17,12); break;
            case "reload": g.DrawArc(p,4,4,16,16,45,290); Path(new(20,4),new(20,9),new(15,9)); break;
            case "home": Path(new(3,10),new(12,3),new(21,10)); Path(new(6,9),new(6,21),new(18,21),new(18,9)); Path(new(10,21),new(10,14),new(14,14),new(14,21)); break;
            case "search": g.DrawEllipse(p,4,3,13,13); Line(15,15,21,21); break;
            case "star":
                var star=Enumerable.Range(0,10).Select(i=>{double a=i*Math.PI/5-Math.PI/2;float r=i%2==0?9:4.3f;return new PointF(12+(float)Math.Cos(a)*r,12+(float)Math.Sin(a)*r);}).ToArray(); g.DrawPolygon(p,star); break;
            case "plus": Line(12,5,12,19); Line(5,12,19,12); break;
            case "close": Line(6,6,18,18); Line(18,6,6,18); break;
            case "min": Line(5,12,19,12); break;
            case "max": g.DrawRectangle(p,5,5,14,14); break;
            case "restore": g.DrawRectangle(p,5,8,11,11); Path(new(9,5),new(19,5),new(19,15)); break;
            case "download": Line(12,3,12,16); Path(new(7,11),new(12,16),new(17,11)); Path(new(4,17),new(4,21),new(20,21),new(20,17)); break;
            case "menu": Line(4,6,20,6); Line(4,12,20,12); Line(4,18,20,18); break;
            case "shield": Path(new(12,2),new(20,6),new(19,15),new(16,19),new(12,22),new(8,19),new(5,15),new(4,6),new(12,2)); Path(new(8,12),new(11,15),new(16,9)); break;
            case "globe": g.DrawEllipse(p,3,3,18,18); g.DrawEllipse(p,8,3,8,18); Line(3,12,21,12); break;
            case "history": g.DrawArc(p,4,4,16,16,210,310); Path(new(3,3),new(3,8),new(8,8)); Path(new(12,7),new(12,12),new(16,14)); break;
            case "memory": g.DrawRectangle(p,6,6,12,12); g.DrawRectangle(p,9,9,6,6); for(int i=8;i<=16;i+=4){Line(i,2,i,6);Line(i,18,i,22);Line(2,i,6,i);Line(18,i,22,i);} break;
            case "settings":
                g.DrawEllipse(p,8,8,8,8); for(int i=0;i<8;i++){double a=i*Math.PI/4;Line(12+(float)Math.Cos(a)*7,12+(float)Math.Sin(a)*7,12+(float)Math.Cos(a)*10,12+(float)Math.Sin(a)*10);} g.DrawEllipse(p,5,5,14,14); break;
            case "user": g.DrawEllipse(p,8,3,8,8); g.DrawArc(p,4,13,16,15,180,180); break;
            case "extension": Path(new(4,4),new(9,4),new(9,2),new(14,2),new(14,4),new(20,4),new(20,10),new(22,10),new(22,15),new(20,15),new(20,21),new(4,21),new(4,4)); break;
            case "leaf": g.DrawArc(p,3,3,18,18,175,185); Path(new(3,13),new(6,18),new(11,20),new(17,18),new(21,3),new(3,13)); Line(6,18,15,9); break;
            case "play": g.FillPolygon(b,new PointF[]{new(9,5),new(20,12),new(9,19)}); break;
            case "code": Path(new(8,6),new(2,12),new(8,18)); Path(new(16,6),new(22,12),new(16,18)); Line(14,4,10,20); break;
            case "update": g.DrawArc(p,3,3,18,18,45,285); Path(new(21,3),new(21,9),new(15,9)); Line(12,7,12,17); Path(new(8,11),new(12,7),new(16,11)); break;
            case "key": g.DrawEllipse(p,3,4,10,10); Path(new(12,12),new(21,21),new(23,19)); Line(17,17,20,14); break;
            default: g.DrawEllipse(p,4,4,16,16); Line(12,7,12,13); g.FillEllipse(b,11,16,2,2); break;
        }
        g.Restore(state);
    }
    public static Bitmap Bitmap(string name, Color color, int size=24)
    { var bitmap=new Bitmap(size,size); using var g=Graphics.FromImage(bitmap); Draw(g,name,new(2,2,size-4,size-4),color); return bitmap; }
}

public class RoundedPanel : Panel, IThemed
{
    public Palette Colors { get; private set; } = Palette.Create(false,Color.MediumAquamarine);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 14;
    public RoundedPanel() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); }
    public void SetPalette(Palette p) { Colors=p; BackColor=p.Surface; ForeColor=p.Text; Invalidate(); }
    protected override void OnPaintBackground(PaintEventArgs e)
    { e.Graphics.Clear(Parent?.BackColor??Colors.Background); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; using var path=Shapes.Round(new(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1)),Radius*DeviceDpi/96f); using var fill=new SolidBrush(Colors.Surface); using var line=new Pen(Colors.Line); e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(line,path); }
}
public class SoftButton : Button, IThemed
{
    protected Palette Colors = Palette.Create(false, Color.MediumAquamarine);
    protected bool Hover;
    protected int D(int value) => (int)Math.Round(value * DeviceDpi / 96f);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Glyph { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Danger { get; set; }
    public SoftButton()
    { FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); }
    public void SetPalette(Palette p) { Colors=p;BackColor=p.Surface;ForeColor=p.Text;Invalidate(); }
    protected override void OnMouseEnter(EventArgs e){Hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){Hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent?.BackColor??Colors.Surface);
        if(Hover || Focused){using var path=Shapes.Round(new(2,2,Width-4,Height-4),8);using var fill=new SolidBrush(Danger?Color.FromArgb(192,58,73):Colors.Elevated);g.FillPath(fill,path);}
        var ink=Enabled?Colors.Text:Colors.Muted;
        if(Glyph.Length>0)
        {
            if (Text.Length > 0)
            {
                Glyphs.Draw(g,Glyph,new(D(12),(Height-D(20))/2f,D(20),D(20)),ink);
                TextRenderer.DrawText(g,Text,Font,new Rectangle(D(39),0,Math.Max(1,Width-D(43)),Height),ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            }
            else Glyphs.Draw(g,Glyph,new((Width-D(20))/2f,(Height-D(20))/2f,D(20),D(20)),ink);
        }
        else TextRenderer.DrawText(g,Text,Font,ClientRectangle,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}

public sealed class BrowserPage : Panel
{
    string caption = "Nova aba";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption
    {
        get => caption;
        set { caption = string.IsNullOrWhiteSpace(value) ? "Nova aba" : value; CaptionChanged?.Invoke(this, EventArgs.Empty); }
    }
    public event EventHandler? CaptionChanged;
}

// Page host without the platform TabControl frame or white native tab headers.
public sealed class BrowserTabs : Panel, IThemed
{
    public sealed class PageCollection(BrowserTabs owner) : Collection<BrowserPage>
    {
        protected override void InsertItem(int index,BrowserPage item)
        {base.InsertItem(index,item);item.Dock=DockStyle.Fill;item.Visible=false;owner.Controls.Add(item);item.CaptionChanged+=(_,_)=>owner.Header.Invalidate();if(owner.SelectedTab==null)owner.SelectedTab=item;owner.Header.Invalidate();}
        protected override void RemoveItem(int index)
        {var page=this[index];bool selected=page==owner.SelectedTab;owner.Controls.Remove(page);base.RemoveItem(index);if(selected)owner.SelectedTab=Count>0?this[Math.Min(index,Count-1)]:null;owner.Header.Invalidate();}
    }
    public PageCollection TabPages {get;}
    public TabRibbon Header {get;}
    BrowserPage? selected;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BrowserPage? SelectedTab
    {
        get=>selected;
        set
        {
            if(selected==value)return;
            if(selected!=null)selected.Visible=false; selected=value;
            if(selected!=null){selected.Visible=true;selected.BringToFront();}
            Header.EnsureSelectedVisible();Header.Invalidate();SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public int TabCount=>TabPages.Count;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex {get=>selected==null?-1:TabPages.IndexOf(selected);set{if(value>=0&&value<TabCount)SelectedTab=TabPages[value];}}
    public event EventHandler? SelectedIndexChanged;
    public event Action<BrowserPage>? CloseRequested;
    public event Action? NewRequested;
    public BrowserTabs(){TabPages=new(this);Header=new(this);SetStyle(ControlStyles.OptimizedDoubleBuffer,true);}
    public Rectangle GetTabRect(int index)=>Header.GetTabRect(index);
    internal void Close(BrowserPage page)=>CloseRequested?.Invoke(page);
    internal void New()=>NewRequested?.Invoke();
    public void SetPalette(Palette p){BackColor=p.Background;Header.SetPalette(p);}
}
public sealed class TabRibbon : Control, IThemed
{
    readonly BrowserTabs owner;
    Palette colors = Palette.Create(false, Color.MediumAquamarine);
    int first, hover = -1; bool overPlus;
    int D(int value) => (int)Math.Round(value * DeviceDpi / 96f);
    int TabWidth => Math.Clamp((Width - D(110)) / Math.Max(1, owner.TabCount), D(110), D(220));
    int Capacity => Math.Max(1, (Width - D(110)) / Math.Max(1, TabWidth));
    public TabRibbon(BrowserTabs tabs)
    {
        owner = tabs; Dock = DockStyle.Fill; Height = 48; AccessibleName = "Abas do navegador";
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
    public void SetPalette(Palette p) { colors = p; BackColor = p.Background; Invalidate(); }
    public Rectangle GetTabRect(int i) => i < first || i >= first + Capacity ? Rectangle.Empty : new(D(54) + (i - first) * TabWidth, D(6), TabWidth - D(4), Math.Max(D(24), Height - D(10)));
    Rectangle Plus => new(Math.Min(D(54) + Math.Max(0, Math.Min(owner.TabCount - first, Capacity)) * TabWidth, Width - D(42)), (Height - D(32)) / 2, D(32), D(32));
    public void EnsureSelectedVisible()
    {
        first = Math.Clamp(first, 0, Math.Max(0, owner.TabCount - 1));
        if (owner.SelectedIndex < first) first = Math.Max(0, owner.SelectedIndex);
        if (owner.SelectedIndex >= first + Capacity) first = owner.SelectedIndex - Capacity + 1;
    }
    protected override void OnResize(EventArgs e) { EnsureSelectedVisible(); base.OnResize(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.Clear(colors.Background); g.SmoothingMode = SmoothingMode.AntiAlias;
        int centerY = Height / 2;
        using (var path = Shapes.Round(new Rectangle(D(13), centerY - D(13), D(26), D(26)), D(8)))
        using (var fill = new SolidBrush(colors.Accent)) g.FillPath(fill, path);
        using (var pen = new Pen(colors.Background, D(2)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        { g.DrawLine(pen, D(21), centerY - D(7), D(21), centerY + D(7)); g.DrawLines(pen, new PointF[] { new(D(30), centerY - D(7)), new(D(23), centerY), new(D(31), centerY + D(7)) }); }
        for (int i = first; i < Math.Min(owner.TabCount, first + Capacity); i++)
        {
            var r = GetTabRect(i); bool active = i == owner.SelectedIndex;
            if (active || i == hover) { using var path = Shapes.Round(r, D(10)); using var fill = new SolidBrush(active ? colors.Surface : colors.Elevated); g.FillPath(fill, path); }
            Glyphs.Draw(g, "globe", new(r.X + D(12), r.Y + (r.Height - D(16)) / 2, D(16), D(16)), active ? colors.Accent : colors.Muted, 1.5f);
            using var font = new Font("Segoe UI", 9.2f, active ? FontStyle.Bold : FontStyle.Regular);
            // Use the explicit page caption; Panel.Text is not a reliable tab title store.
            TextRenderer.DrawText(g, owner.TabPages[i].Caption, font, new Rectangle(r.X + D(36), r.Y, Math.Max(1, r.Width - D(65)), r.Height), active ? colors.Text : colors.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (active || i == hover) Glyphs.Draw(g, "close", new(r.Right - D(24), r.Y + (r.Height - D(14)) / 2, D(14), D(14)), colors.Muted, 1.8f);
            if (active) { using var line = new Pen(colors.Accent, Math.Max(1, D(2))); g.DrawLine(line, r.X + D(16), r.Bottom - 1, r.Right - D(16), r.Bottom - 1); }
        }
        if (overPlus) { using var path = Shapes.Round(Plus, D(8)); using var fill = new SolidBrush(colors.Elevated); g.FillPath(fill, path); }
        Glyphs.Draw(g, "plus", new(Plus.X + D(7), Plus.Y + D(7), D(18), D(18)), colors.Muted);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    { hover = -1; for (int i = first; i < Math.Min(owner.TabCount, first + Capacity); i++) if (GetTabRect(i).Contains(e.Location)) hover = i; overPlus = Plus.Contains(e.Location); Invalidate(); base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = -1; overPlus = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Plus.Contains(e.Location) && e.Button == MouseButtons.Left) { owner.New(); return; }
        for (int i = first; i < Math.Min(owner.TabCount, first + Capacity); i++)
        {
            var r = GetTabRect(i); if (!r.Contains(e.Location)) continue;
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && e.X >= r.Right - D(28))) owner.Close(owner.TabPages[i]);
            else if (e.Button == MouseButtons.Left) owner.SelectedIndex = i;
            return;
        }
        if (e.Button == MouseButtons.Left && FindForm() is { } f) { if (e.Clicks == 2) f.WindowState = f.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; else NativeFrame.Drag(f); }
    }
    protected override void OnMouseWheel(MouseEventArgs e) { first = Math.Clamp(first + (e.Delta < 0 ? 1 : -1), 0, Math.Max(0, owner.TabCount - Capacity)); Invalidate(); base.OnMouseWheel(e); }
}
public static class NativeFrame
{
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("dwmapi.dll")]public static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    public static void Drag(Form form){ReleaseCapture();SendMessage(form.Handle,0xA1,new IntPtr(2),IntPtr.Zero);}
}
