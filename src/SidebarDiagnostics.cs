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
        probe.Size=new Size(900,600);probe.Location=new Point(20,20);probe.PrepareDiagnosticPage();probe.Show();await Task.Delay(70);
        probe.ShowMenu();await Task.Delay(70);
        var exit=probe.windowButtons!.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Fechar navegador");
        Click(exit);await Task.Delay(120);
        Check(!probe.IsDisposed&&!probe.closing,"Clique fora sobre Fechar descarta apenas menu, sem encerrar navegador");
        Check(!probe.menuOpen&&probe.menuPopup?.Visible!=true,"Menu fica oculto após clique externo na barra da janela");
        // A second, intentional click must still work after the dismissal gesture is consumed.
        var maximize=probe.windowButtons.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Maximizar / restaurar");
        probe.ShowMenu();await Task.Delay(70);Click(maximize);await Task.Delay(100);
        Check(probe.WindowState==FormWindowState.Normal&&!probe.menuOpen,"Clique de dispensa não aciona Maximizar por baixo");
        Click(maximize);await Task.Delay(100);Check(probe.WindowState==FormWindowState.Maximized,"Próximo clique intencional continua funcionando");
        Click(maximize);await Task.Delay(100);
        foreach(bool animated in new[]{false,true})
        {
            Motion.DiagnosticAnimations=animated;Motion.Enabled=animated;
            if(animated)
            {
                probe.ShowMenu();await Task.Delay(300);probe.CloseBrowserMenu();
                Click(exit);await Task.Delay(250);
                Check(!probe.IsDisposed&&!probe.closing,"Clique durante saída do menu não encerra navegador");
            }
            probe.ShowAppearance();await Task.Delay(340);
            for(int i=0;i<4;i++)
            {
                Cursor.Position=probe.tabStrip.PointToScreen(new Point(25,100));
                mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);await Task.Delay(50);
                Check(probe.appearanceOpen&&probe.appearance!.Visible&&!probe.closing,"Clique fora mantém Aparência e navegador abertos; animações="+animated+"; ciclo="+i);
            }
            using(var external=new Form{Size=new Size(120,80),StartPosition=FormStartPosition.Manual,Location=new Point(0,0)})
            {
                external.Show();external.Activate();await Task.Delay(80);
                Check(probe.appearanceOpen&&!probe.closing,"Perda de foco preserva Aparência");external.Close();probe.Activate();
            }
            // Open advanced settings through the actual menu route while Appearance remains open.
            int stage=0;Exception? error=null;var done=new TaskCompletionSource<bool>();
            using var driver=new System.Windows.Forms.Timer{Interval=70};
            driver.Tick+=(_,_)=>
            {
                try
                {
                    var dialog=Application.OpenForms.Cast<Form>().OfType<AppDialog>().LastOrDefault(f=>f.Text=="Configurações avançadas");
                    if(dialog==null)return;
                    if(stage++==0){Click(exit);return;}
                    Check(dialog.Visible&&!dialog.IsDisposed&&!probe.closing,"Clique fora mantém Configurações abertas; animações="+animated);
                    var combo=AppearancePanel.Descendants(dialog).OfType<ComboBox>().First();
                    if(stage==2){combo.DroppedDown=true;return;}
                    if(stage==3){Click(dialog);return;}
                    Check(dialog.Visible&&!probe.closing,"Dispensar lista de Configurações não fecha a janela");
                    driver.Stop();dialog.Close();done.TrySetResult(true);
                }
                catch(Exception e){error=e;driver.Stop();foreach(var f in Application.OpenForms.Cast<Form>().OfType<AppDialog>().ToArray())f.Close();done.TrySetResult(false);}
            };
            driver.Start();probe.ShowMenu();probe.browserMenu!.ClickCommand("settings");
            await done.Task.WaitAsync(TimeSpan.FromSeconds(10));if(error!=null)throw error;
            Check(probe.appearanceOpen&&!probe.closing,"Fechar Configurações preserva lateral de Aparência");
            probe.ShowAppearance();await Task.Delay(300);
            probe.ToggleAssistant();await Task.Delay(340);
            Cursor.Position=probe.tabStrip.PointToScreen(new Point(25,100));mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);await Task.Delay(70);
            Check(probe.assistantOpen&&probe.assistant!.Visible&&!probe.closing,"Clique fora mantém IA e navegador abertos; animações="+animated);
            probe.ShowMenu();await Task.Delay(300);
            Click(probe.address);await Task.Delay(100);
            Check(probe.assistantOpen&&!probe.menuOpen,"Dispensar menu na barra de endereço preserva IA");
            probe.ToggleAssistant();await Task.Delay(300);
        }
        Motion.DiagnosticAnimations=null;Motion.Enabled=false;
        Click(exit);await Task.Delay(100);Check(probe.IsDisposed,"Fechar navegador funciona após dispensar painéis");Activate();
        return checks.ToArray();
    }
}
