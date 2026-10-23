using System.ComponentModel;

namespace KayBrowser;

public class AppDialog : Form
{
    readonly Panel content = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    readonly Palette palette;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new Control.ControlCollection Controls => content.Controls;
    public AppDialog(string title, Palette colors, int width = 820, int height = 570)
    {
        palette = colors;
        Text = title; ClientSize = new Size(width, height); FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        BackColor = colors.Background; ForeColor = colors.Text; Padding = new Padding(1);
        MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false; KeyPreview = true;
        content.BackColor = colors.Background; content.ForeColor = colors.Text;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var caption = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = colors.Surface, Padding = new Padding(12, 4, 4, 4) };
        caption.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); caption.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40)); caption.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = colors.Text, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = Padding.Empty, AutoEllipsis = true };
        var close = new SoftButton { Glyph = "close", Danger = true, Dock = DockStyle.Fill, Margin = Padding.Empty, AccessibleName = "Fechar " + title };
        close.SetPalette(colors); close.Click += (_, _) => Close();
        caption.Controls.Add(label, 0, 0); caption.Controls.Add(close, 1, 0);
        label.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) NativeFrame.Drag(this); };
        layout.Controls.Add(caption, 0, 0); layout.Controls.Add(content, 0, 1);
        base.Controls.Add(layout);
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var area = Screen.FromControl(Owner ?? this).WorkingArea;
        if (Width > area.Width - 24 || Height > area.Height - 24)
            Size = new Size(Math.Min(Width, area.Width - 24), Math.Min(Height, area.Height - 24));
        if (Owner != null) Location = new Point(Math.Clamp(Owner.Left + (Owner.Width - Width) / 2, area.Left, area.Right - Width), Math.Clamp(Owner.Top + (Owner.Height - Height) / 2, area.Top, area.Bottom - Height));
    }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using var pen = new Pen(palette.Line); e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1); }
}

public static class UiMessage
{
    public static DialogResult Show(IWin32Window? owner, Palette colors, string message, string title = "Clean Browser", MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        using var f = new AppDialog(title, colors, 590, 310);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(22) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var text = new TextBox { Text = message, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = colors.Background, ForeColor = colors.Text, ScrollBars = ScrollBars.Vertical, TabStop = false };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        SoftButton Add(string label, DialogResult result)
        {
            var button = new SoftButton { Text = label, Width = 120, Height = 38, DialogResult = result }; button.SetPalette(colors); actions.Controls.Add(button); return button;
        }
        if (buttons == MessageBoxButtons.YesNo) { f.CancelButton = Add("Não", DialogResult.No); f.AcceptButton = Add("Sim", DialogResult.Yes); }
        else if (buttons == MessageBoxButtons.OKCancel) { f.CancelButton = Add("Cancelar", DialogResult.Cancel); f.AcceptButton = Add("Continuar", DialogResult.OK); }
        else f.AcceptButton = f.CancelButton = Add("Entendi", DialogResult.OK);
        body.Controls.Add(text, 0, 0); body.Controls.Add(actions, 0, 1); f.Controls.Add(body);
        return owner == null ? f.ShowDialog() : f.ShowDialog(owner);
    }
}
