namespace KayBrowser;

public sealed partial class BrowserWindow
{
    internal async Task<int> VerifyCleanMotion()
    {
        int checks=0;
        void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("Motion: "+label);checks++;}
        bool enabled=Motion.Enabled;bool? preference=Motion.DiagnosticAnimations;
        var original=WindowState;var bounds=Bounds;
        try
        {
            Motion.Enabled=true;Motion.DiagnosticAnimations=true;
            using var host=new Form{Text="Clean Browser - motion fixture",Size=new Size(860,380),ShowInTaskbar=false};
            using var strip=new BrowserTabs{Dock=DockStyle.Fill};
            strip.Header.Dock=DockStyle.Top;strip.Header.Height=48;
            host.Controls.Add(strip);host.Controls.Add(strip.Header);host.Show(this);
            var a=new BrowserPage{Caption="First"};var b=new BrowserPage{Caption="Second"};
            strip.TabPages.Add(a);strip.TabPages.Add(b);strip.SelectedTab=b;
            Check(strip.Header.TabMotionRunning,"opening starts a timer");
            int entryWidth=strip.GetTabRect(1).Width;
            await Task.Delay(280);
            Check(!strip.Header.TabMotionRunning&&strip.GetTabRect(1).Width>entryWidth,"opening expands and stops");
            var c=new BrowserPage{Caption="Third"};strip.TabPages.Add(c);strip.SelectedTab=c;await Task.Delay(260);
            strip.TabPages.Remove(b);b.Dispose();
            Check(strip.TabCount==2&&strip.Header.ExitingTabCount==1,"closed content removed immediately while visual exits");
            Check(strip.Header.TabMotionRunning,"closing starts a timer");
            await Task.Delay(260);
            Check(!strip.Header.TabMotionRunning&&strip.Header.ExitingTabCount==0,"closing releases visual and timer");
            Check(!strip.GetTabRect(0).IntersectsWith(strip.GetTabRect(1)),"final tabs do not overlap");
            for(int i=0;i<16;i++){var page=new BrowserPage{Caption="Rapid "+i};strip.TabPages.Add(page);strip.TabPages.Remove(page);page.Dispose();}
            await Task.Delay(260);
            Check(!strip.Header.TabMotionRunning&&strip.Header.ExitingTabCount==0&&strip.TabCount==2,"rapid open-close settles without resurrecting pages");
            strip.Header.Vertical=true;strip.Header.Dock=DockStyle.Left;strip.Header.Width=180;
            var vertical=new BrowserPage{Caption="Vertical"};strip.TabPages.Add(vertical);await Task.Delay(260);
            Check(!strip.Header.TabMotionRunning&&!strip.GetTabRect(2).IsEmpty,"vertical entry settles");
            strip.TabPages.Remove(vertical);vertical.Dispose();await Task.Delay(260);
            Check(!strip.GetTabRect(0).IntersectsWith(strip.GetTabRect(1)),"vertical close reflows");
            var resized=new BrowserPage{Caption="Resize"};strip.TabPages.Add(resized);host.Width+=30;await Task.Delay(50);
            Check(!strip.Header.TabMotionRunning,"resize settles geometry immediately");
            strip.TabPages.Remove(resized);resized.Dispose();await Task.Delay(260);
            Motion.DiagnosticAnimations=false;
            var reduced=new BrowserPage{Caption="Reduced motion"};strip.TabPages.Add(reduced);
            Check(!strip.Header.TabMotionRunning,"system reduced motion skips entry");
            strip.TabPages.Remove(reduced);reduced.Dispose();
            Check(!strip.Header.TabMotionRunning&&strip.Header.ExitingTabCount==0,"system reduced motion skips exit");
            Motion.DiagnosticAnimations=true;Motion.Enabled=false;
            var disabled=new BrowserPage();strip.TabPages.Add(disabled);
            Check(!strip.Header.TabMotionRunning,"app preference skips entry");
            strip.TabPages.Remove(disabled);disabled.Dispose();
            Motion.Enabled=true;
            host.Close();
            ShowMenu();
            Check(menuMotion.Running&&menuPopup!.Opacity<1,"menu begins with fade");
            await Task.Delay(260);
            Check(!menuMotion.Running&&menuPopup!.Opacity==1&&menuPopup.Location==MenuLocation(),"menu ends opaque and aligned");
            CloseBrowserMenu();await Task.Delay(210);
            Check(!MenuVisible&&!menuMotion.Running&&tabStrip.Enabled,"menu exit restores input");
            for(int i=0;i<8;i++){ShowMenu();CloseBrowserMenu();}
            ShowMenu();await Task.Delay(260);
            Check(menuOpen&&menuPopup!.Opacity==1&&!menuMotion.Running,"reopening during fade settles");
            CloseBrowserMenu(false);
            Check(!MenuVisible&&tabStrip.Enabled,"immediate dismissal leaves no overlay");
            Motion.Enabled=false;ShowMenu();
            Check(!menuMotion.Running&&menuPopup!.Opacity==1,"menu respects disabled animations");
            CloseBrowserMenu(false);Motion.Enabled=true;
            // Exercise the actual native system-command path used by caption buttons and double click.
            NativeFrame.WindowCommand(this,0xF030);await Task.Delay(350);
            Check(WindowState==FormWindowState.Maximized,"native maximize");
            NativeFrame.WindowCommand(this,0xF120);await Task.Delay(350);
            Check(WindowState==FormWindowState.Normal,"native restore");
            Check(Math.Abs(Left-bounds.Left)<=2&&Math.Abs(Top-bounds.Top)<=2&&Math.Abs(Width-bounds.Width)<=2&&Math.Abs(Height-bounds.Height)<=2,"restore preserves bounds");
            NativeFrame.WindowCommand(this,0xF020);await Task.Delay(350);
            Check(WindowState==FormWindowState.Minimized,"native minimize");
            NativeFrame.WindowCommand(this,0xF120);await Task.Delay(350);
            Check(WindowState==FormWindowState.Normal,"restore after minimize");
            VerifyChromeLayout();Check(true,"native transitions preserve chrome layout");
            return checks;
        }
        finally
        {
            CloseBrowserMenu(false);WindowState=original;if(original==FormWindowState.Normal)Bounds=bounds;
            Motion.DiagnosticAnimations=preference;Motion.Enabled=enabled;
        }
    }
}
