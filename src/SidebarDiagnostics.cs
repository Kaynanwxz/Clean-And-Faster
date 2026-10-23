namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task<string[]> VerifySidebarDismissal()
    {
        var checks=new List<string>();
        void Check(bool ok,string label)
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),(ok?"PASS ":"FAIL ")+label+Environment.NewLine);
            if(!ok)throw new InvalidOperationException(label);checks.Add(label);
        }
        void Click(Control control)
        {
            Cursor.Position=control.PointToScreen(new Point(control.Width/2,control.Height/2));
            mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);
        }
        using var probe=new BrowserWindow(diagnostics:true);
        probe.Size=new Size(900,600);probe.Show();await Task.Delay(70);
        probe.ShowMenu();await Task.Delay(70);
        var exit=probe.windowButtons!.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Fechar navegador");
        Click(exit);await Task.Delay(120);
        Check(!probe.IsDisposed&&!probe.closing,"Clique fora sobre Fechar descarta apenas menu, sem encerrar navegador");
        probe.Close();Activate();
        return checks.ToArray();
    }
}
