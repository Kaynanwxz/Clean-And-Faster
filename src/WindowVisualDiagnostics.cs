using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace KayBrowser;

public sealed partial class BrowserWindow
{
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out VisualRect rectangle);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
    [StructLayout(LayoutKind.Sequential)] struct VisualRect { public int Left,Top,Right,Bottom; }

    // This fixture contains only synthetic local pages. Screen reads are CI-only,
    // confined to this known window, with a foreground-owner check before each read.
    internal async Task<int> VerifyWindowVisuals()
    {
        int checks=0;
        var evidence=new List<object>();
        var savedMotion=Motion.Enabled;var savedOverride=Motion.DiagnosticAnimations;
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("Visual: "+text);checks++;}
        using var probe=new BrowserWindow(diagnostics:true){StartPosition=FormStartPosition.Manual,Location=new Point(12,12),Size=new Size(960,680)};
        probe.environment=environment;
        try
        {
            Motion.Enabled=true;Motion.DiagnosticAnimations=true;
            probe.Show();probe.Activate();await probe.AddTab("kay://new");await Task.Delay(420);
            string directory=Path.Combine(AppContext.BaseDirectory,"visual-evidence");Directory.CreateDirectory(directory);
            bool ci=Environment.GetEnvironmentVariable("GITHUB_ACTIONS")=="true";
            string Capture(string name,bool ribbon=false)
            {
                var area=probe.Bounds;if(ribbon)area.Height=Math.Min(area.Height,probe.Ui(58));
                using var bitmap=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppArgb);
                if(ci)
                {
                    var foreground=GetForegroundWindow();
                    Check(foreground==probe.Handle||foreground==probe.menuPopup?.Handle,"evidence belongs to the synthetic foreground window");
                    Check(Screen.FromHandle(probe.Handle).Bounds.Contains(area),"capture is inside the fixture monitor");
                    using var g=Graphics.FromImage(bitmap);g.CopyFromScreen(area.Location,Point.Empty,area.Size,CopyPixelOperation.SourceCopy);
                }
                else
                {
                    using var full=new Bitmap(probe.Width,probe.Height);using var g=Graphics.FromImage(full);var dc=g.GetHdc();
                    try{Check(PrintWindow(probe.Handle,dc,2),"native window can render its frame");}finally{g.ReleaseHdc(dc);}
                    using var target=Graphics.FromImage(bitmap);target.DrawImageUnscaled(full,Point.Empty);
                }
                string path=Path.Combine(directory,name+".png");bitmap.Save(path,ImageFormat.Png);
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
            }
            void ClientMatchesWindow(string state)
            {
                Check(GetClientRect(probe.Handle,out var rect),state+" client rectangle is available");
                Check(rect.Right==probe.Width&&rect.Bottom==probe.Height,state+" has no hidden caption or offscreen client inset");
                Check(probe.PointToScreen(Point.Empty)==probe.Location,state+" client origin matches visible window origin");
                Check(probe.chromeLayout.Bounds==probe.DisplayRectangle,state+" chrome fills the client surface");
                Check(probe.locationBox!.ClientRectangle.Contains(probe.address.Bounds),state+" address fits its surface");
            }
            async Task Record(string name,Action action,Func<float> read,bool ribbon)
            {
                var samples=new List<object>();var distinct=new HashSet<int>();var timer=Stopwatch.StartNew();
                action();int frame=0;
                do
                {
                    float value=read();distinct.Add((int)(value*1000));
                    samples.Add(new{ms=timer.Elapsed.TotalMilliseconds,value});
                    Capture(name+"-"+(frame++).ToString("D2"),ribbon);
                    await Task.Delay(25);
                }while(timer.ElapsedMilliseconds<380);
                Check(distinct.Count>=4,name+" renders several intermediate states through the real command path");
                evidence.Add(new{name,frames=frame,distinct=distinct.Count,samples});
                await Task.Delay(120);
            }
            ClientMatchesWindow("normal");Capture("window-normal");
            string titleBefore=Capture("title-before",true);
            probe.Text="Synthetic native title repaint regression";
            string titleAfter=Capture("title-after",true);
            Check(titleBefore==titleAfter,"changing the native title never paints a second caption over the tabs");
            int originalTabs=probe.tabs.Count;
            await Record("tab-open",()=>probe.Shortcut(probe,new KeyEventArgs(Keys.Control|Keys.T)),()=>probe.tabStrip.Header.TabMotionProgress,true);
            Check(probe.tabs.Count==originalTabs+1,"Ctrl+T opens one real tab");
            Check(!probe.tabStrip.Header.TabMotionRunning,"real tab opening settles");
            await Record("tab-close",()=>probe.Shortcut(probe,new KeyEventArgs(Keys.Control|Keys.W)),()=>probe.tabStrip.Header.TabMotionProgress,true);
            Check(probe.tabs.Count==originalTabs,"Ctrl+W closes the real tab immediately");
            await Record("menu-open",probe.ShowMenu,()=>probe.menuPopup is {Visible:true} popup?(float)popup.Opacity:0,false);
            Check(probe.menuOpen&&probe.menuPopup!.Opacity==1,"menu reaches full opacity");
            probe.CloseBrowserMenu(false);await Task.Delay(100);probe.Activate();
            var normal=probe.Bounds;
            var maximize=probe.windowButtons!.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Maximizar / restaurar");
            for(int cycle=0;cycle<4;cycle++)
            {
                maximize.PerformClick();await Task.Delay(400);
                Check(probe.WindowState==FormWindowState.Maximized,"caption button maximizes");
                Check(probe.Bounds==Screen.FromHandle(probe.Handle).WorkingArea,"maximized frame exactly fills current work area");
                ClientMatchesWindow("maximized");if(cycle==0)Capture("window-maximized");
                maximize.PerformClick();await Task.Delay(400);
                Check(probe.WindowState==FormWindowState.Normal&&probe.Bounds==normal,"caption restore retains the original bounds");
                ClientMatchesWindow("restored");
            }
            Capture("window-restored");
            var minimize=probe.windowButtons.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Minimizar");
            minimize.PerformClick();await Task.Delay(350);Check(probe.WindowState==FormWindowState.Minimized,"caption button minimizes");
            NativeFrame.WindowCommand(probe,0xF120);probe.Activate();await Task.Delay(350);
            Check(probe.Bounds==normal,"restore after minimize preserves bounds");ClientMatchesWindow("unminimized");
            for(int cycle=0;cycle<3;cycle++)
            {
                probe.Fullscreen();await Task.Delay(150);
                Check(probe.Bounds==Screen.FromHandle(probe.Handle).Bounds,"F11 covers the monitor");
                probe.Fullscreen();await Task.Delay(150);
                Check(probe.WindowState==FormWindowState.Normal&&probe.Bounds==normal,"F11 returns to the original normal window");
            }
            maximize.PerformClick();await Task.Delay(350);probe.Fullscreen();await Task.Delay(150);probe.Fullscreen();await Task.Delay(350);
            Check(probe.WindowState==FormWindowState.Maximized&&probe.Bounds==Screen.FromHandle(probe.Handle).WorkingArea,"F11 returns to a previously maximized window");
            maximize.PerformClick();await Task.Delay(350);
            foreach(var size in new[]{new Size(920,620),new Size(980,700),new Size(900,600)})
            {
                probe.Size=size;await Task.Delay(80);ClientMatchesWindow("resized");
            }
            probe.Activate();Capture("window-resized");
            Motion.Enabled=false;
            probe.Shortcut(probe,new KeyEventArgs(Keys.Control|Keys.T));Check(!probe.tabStrip.Header.TabMotionRunning,"real Ctrl+T respects disabled motion");
            probe.Shortcut(probe,new KeyEventArgs(Keys.Control|Keys.W));Check(!probe.tabStrip.Header.TabMotionRunning,"real Ctrl+W respects disabled motion");
            evidence.Add(new{systemAnimations=Motion.SystemAnimations,screen=Screen.FromHandle(probe.Handle).Bounds,dpi=probe.DeviceDpi,ciScreenCapture=ci,checks});
            File.WriteAllText(Path.Combine(directory,"visual-check.json"),JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
            return checks;
        }
        finally{probe.CloseBrowserMenu(false);probe.Close();Motion.Enabled=savedMotion;Motion.DiagnosticAnimations=savedOverride;Activate();}
    }
}
