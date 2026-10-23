using System.Drawing.Drawing2D;
namespace KayBrowser;

public class ChoiceButton : SoftButton
{
    public bool Selected;
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if(Selected){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var shape=Shapes.Round(new Rectangle(2,2,Math.Max(1,Width-5),Math.Max(1,Height-5)),D(10));using var pen=new Pen(Colors.Accent,D(2));e.Graphics.DrawPath(pen,shape);}
    }
}

public sealed class PresetCard(VisualPreset preset) : ChoiceButton
{
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??Colors.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        var bounds=new Rectangle(3,3,Math.Max(1,Width-7),Math.Max(1,Height-7));using var shape=Shapes.Round(bounds,D(12));using var fill=new SolidBrush(Motion.Blend(Colors.Surface,Colors.Elevated,HoverAmount*.65f));using var line=new Pen(Selected?Colors.Accent:Colors.Line,Selected?D(2):1);g.FillPath(fill,shape);g.DrawPath(line,shape);
        var preview=new Rectangle(D(12),D(12),Math.Max(1,Width-D(24)),D(35));using(var b=new SolidBrush(preset.Theme=="Claro"?Color.FromArgb(235,241,249):Color.FromArgb(6,27,48)))g.FillRectangle(b,preview);
        using(var b=new SolidBrush(Color.FromArgb(12,40,70)))g.FillRectangle(b,preview.X,preview.Y,preview.Width,D(7));
        if(preset.Vertical)using(var b=new SolidBrush(Color.FromArgb(27,61,96)))g.FillRectangle(b,preview.X,preview.Y,D(15),preview.Height);
        g.DrawImage(BrandAssets.Logo,new Rectangle(preview.X+preview.Width/2-D(8),preview.Y+D(12),D(16),D(16)));
        using var f=new Font("Segoe UI",8.5f);TextRenderer.DrawText(g,preset.Name,f,new Rectangle(D(10),D(53),Math.Max(1,Width-D(20)),D(34)),Colors.Text,TextFormatFlags.WordBreak|TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPrefix);
    }
}
