using System.Text.Json;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal void PrepareDiagnosticPage()
    {
        var tab = new BrowserTab(); tabs.Add(tab); tabStrip.TabPages.Add(tab.Page);
    }
    internal void VerifyChromeLayout()
    {
        ApplyChromeMetrics();
        void Layout(Control control)
        { control.PerformLayout(); foreach (Control c in control.Controls) Layout(c); }
        Layout(this);
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        foreach (var table in new[] { titleRow!, windowButtons!, navigationRow! })
        {
            Check(table.RowCount == 1 && table.RowStyles.Count == 1 && table.RowStyles[0].SizeType == SizeType.Percent, "Barra sem linha de altura limitada.");
            foreach (Control child in table.Controls)
                Check(child.Left >= 0 && child.Top >= 0 && child.Right <= table.ClientSize.Width && child.Bottom <= table.ClientSize.Height, $"Controle cortado: {child.AccessibleName ?? child.GetType().Name}; controle={child.Bounds}, pai={table.ClientRectangle}");
        }
        Check(locationBox!.ClientRectangle.Contains(address.Bounds), "Campo de endereço fora da barra.");
        Check(locationBox.ClientRectangle.Contains(connectionIcon!.Bounds), "Ícone de conexão fora da barra.");
        Check(!address.Bounds.IntersectsWith(connectionIcon.Bounds), "Texto do endereço sobreposto ao ícone.");
        Check(chromeLayout.GetRowHeights()[0] >= Ui(46), "Linha de abas insuficiente.");
        Check(navigationRow!.Height >= Ui(58), "Linha de navegação insuficiente.");
        Check(tabStrip.Top >= navigationRow.Bottom, "Conteúdo web sobreposto ao topo.");
        var page = new BrowserPage { Caption = "YouTube · teste de título" };
        tabStrip.TabPages.Add(page); Check(page.Caption == "YouTube · teste de título", "Título da aba foi perdido.");
        tabStrip.TabPages.Remove(page); page.Dispose();
    }
}
public static class ChromeDiagnostics
{
    public static int Run()
    {
        var result = new List<object>();
        try
        {
            using var window = new BrowserWindow(diagnostics: true);
            window.Show(); window.PrepareDiagnosticPage(); Application.DoEvents();
            foreach (var size in new[] { new Size(900, 600), new Size(1320, 860), new Size(1920, 1080) })
            {
                window.WindowState = FormWindowState.Normal; window.Size = size;
                Application.DoEvents(); window.VerifyChromeLayout();
                using (var bitmap = new Bitmap(window.Width, window.Height))
                {
                    window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(AppContext.BaseDirectory, $"layout-{size.Width}x{size.Height}.png"));
                }
                result.Add(new { width = window.Width, height = window.Height, dpi = window.DeviceDpi, status = "PASS" });
            }
            window.WindowState = FormWindowState.Maximized; Application.DoEvents(); window.VerifyChromeLayout();
            result.Add(new { mode = "maximized", status = "PASS" });
            window.WindowState = FormWindowState.Normal; Application.DoEvents(); window.VerifyChromeLayout();
            result.Add(new { mode = "restored", status = "PASS" });
            window.Close();
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "layout-check.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "layout-check.json"), JsonSerializer.Serialize(new { status = "FAIL", error = ex.ToString() }));
            return 1;
        }
    }
}
