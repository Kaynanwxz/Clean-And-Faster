using KayBrowser;
using System.Text.Json;
static class LayoutTests
{
    public static int Run()
    {
        int passed=0;
        void Check(bool ok){if(!ok)throw new Exception("Falha de centralização ou barra inferior");passed++;}
        // Vary count as when adding/removing items, then resize at three DPI scales.
        foreach(int dpi in new[]{96,144,192}) foreach(int width in new[]{270,510,760,1200}) foreach(int count in Enumerable.Range(1,21))
        {
            int D(int n)=>(int)Math.Round(n*dpi/96f); int w=D(width);
            int cols=ShortcutLayout.Columns(count,w,D(98),D(12));
            for(int start=0;start<count;start+=cols)
            {
                int last=Math.Min(count-1,start+cols-1);
                var a=ShortcutLayout.Tile(start,count,cols,w,D(98),D(96),D(101),D(2),0);
                var b=ShortcutLayout.Tile(last,count,cols,w,D(98),D(96),D(101),D(2),0);
                Check(Math.Abs(a.Left-(w-b.Right))<=1);
                Check(a.Left>=0&&b.Right<=w);
                for(int i=start;i<last;i++)
                {
                    var x=ShortcutLayout.Tile(i,count,cols,w,D(98),D(96),D(101),D(2),0);
                    var y=ShortcutLayout.Tile(i+1,count,cols,w,D(98),D(96),D(101),D(2),0);
                    Check(x.Right<=y.Left);
                }
            }
        }
        // Old profiles have no new property. Existing visual choices must survive.
        File.WriteAllText(Path.Combine(Storage.Root,"settings.json"),"""{"AppearanceRevision":2,"Theme":"Escuro","HomeLayout":"Editorial","Accent":"#112233","Shortcuts":[{"Id":"custom","Name":"Meu site","Url":"https://example.com"}]}""");
        var old=new Storage();Check(!old.Settings.ShowBottomBar);Check(old.Settings.Theme=="Escuro"&&old.Settings.HomeLayout=="Editorial"&&old.Settings.Shortcuts.Single().Id=="custom");
        old.Settings.ShowBottomBar=true;old.SaveSettings();Check(new Storage().Settings.ShowBottomBar);
        old.Settings.ShowBottomBar=false;old.SaveSettings();Check(!new Storage().Settings.ShowBottomBar);
        return passed;
    }
}
