namespace KayBrowser;

// Animate only ribbon geometry. Site content stays live and closing releases WebViews immediately.
public sealed partial class TabRibbon
{
    sealed record TabVisual(BrowserPage Page,string Caption,Rectangle Bounds,float Opacity,bool Active);
    sealed record TabTrack(TabVisual From,Rectangle To,float Opacity,bool Exiting);
    readonly Tween tabMotion=new();
    readonly List<TabTrack> tabTracks=[];
    List<TabVisual>? beforeTabs;
    Rectangle beforePlus,plusFrom,plusTo;
    float tabProgress=1;
    internal string TabMotionEndReason {get;private set;}="idle";
    internal float TabMotionProgress=>tabProgress;
    internal bool TabMotionRunning=>tabMotion.Running;
    internal int ExitingTabCount=>tabTracks.Count(t=>t.Exiting);
    Rectangle AnimatedPlus
    {
        get
        {
            if(!tabMotion.Running)return TargetPlus;
            var button=Mix(plusFrom,plusTo,tabProgress);
            foreach(var track in tabTracks)
            {
                if(track.Exiting&&track.From.Opacity*(1-tabProgress)<=.01f)continue;
                var bounds=Mix(track.From.Bounds,track.To,tabProgress);
                if(Vertical)button.Y=Math.Max(button.Y,bounds.Bottom);
                else button.X=Math.Max(button.X,bounds.Right);
            }
            return button;
        }
    }
    internal bool AddButtonSeparated
    {
        get
        {
            var button=AnimatedPlus;var pages=Pages;
            for(int i=first;i<Math.Min(pages.Length,first+Capacity);i++)
                if(GetTabRect(i).IntersectsWith(button))return false;
            return !tabTracks.Any(t=>t.Exiting&&t.From.Opacity*(1-tabProgress)>.01f&&Mix(t.From.Bounds,t.To,tabProgress).IntersectsWith(button));
        }
    }
    static Rectangle Mix(Rectangle a,Rectangle b,float t)=>new(
        (int)MathF.Round(a.X+(b.X-a.X)*t),(int)MathF.Round(a.Y+(b.Y-a.Y)*t),
        Math.Max(1,(int)MathF.Round(a.Width+(b.Width-a.Width)*t)),
        Math.Max(1,(int)MathF.Round(a.Height+(b.Height-a.Height)*t)));
    Rectangle EnterBounds(Rectangle r)=>Vertical
        ?new(r.X+D(6),r.Y,r.Width,1)
        :new(r.X,r.Y+D(5),1,r.Height);
    Rectangle ExitBounds(Rectangle r)=>Vertical
        ?new(r.X+D(8),r.Y,r.Width,1)
        :new(r.X,r.Y+D(6),1,r.Height);
    Rectangle AnimatedTabRect(BrowserPage page,Rectangle target)
    {
        if(target.IsEmpty||!tabMotion.Running)return target;
        var track=tabTracks.FirstOrDefault(t=>!t.Exiting&&t.From.Page==page);
        return track==null?target:Mix(track.From.Bounds,track.To,tabProgress);
    }
    float TabOpacity(BrowserPage page)
    {
        if(!tabMotion.Running)return 1;
        var track=tabTracks.FirstOrDefault(t=>!t.Exiting&&t.From.Page==page);
        return track==null?1:track.From.Opacity+(track.Opacity-track.From.Opacity)*tabProgress;
    }
    internal void BeginTabChange()
    {
        beforeTabs=[];
        var pages=Pages;
        for(int i=first;i<Math.Min(pages.Length,first+Capacity);i++)
            beforeTabs.Add(new(pages[i],pages[i].Caption,GetTabRect(i),TabOpacity(pages[i]),pages[i]==owner.SelectedTab));
        if(tabMotion.Running)
            foreach(var t in tabTracks.Where(t=>t.Exiting))
            {
                float alpha=t.From.Opacity*(1-tabProgress);
                if(alpha>.02f)beforeTabs.Add(t.From with{Bounds=Mix(t.From.Bounds,t.To,tabProgress),Opacity=alpha});
            }
        beforePlus=AnimatedPlus;
        tabMotion.Stop();
    }
    internal void EndTabChange()
    {
        EnsureSelectedVisible();
        var snapshot=beforeTabs;beforeTabs=null;tabTracks.Clear();
        if(snapshot==null||!Motion.Allowed||!IsHandleCreated||!Visible){FinishTabMotion($"layout snapshot={snapshot!=null}, allowed={Motion.Allowed}, handle={IsHandleCreated}, visible={Visible}");return;}
        var pages=Pages;
        var visible=new HashSet<BrowserPage>();
        for(int i=first;i<Math.Min(pages.Length,first+Capacity);i++)
        {
            var page=pages[i];visible.Add(page);var target=TargetTabRect(i);
            var from=snapshot.FirstOrDefault(t=>t.Page==page)??new(page,page.Caption,EnterBounds(target),0,page==owner.SelectedTab);
            tabTracks.Add(new(from,target,1,false));
        }
        // Workspace changes and scrolling must never resurrect hidden tabs.
        foreach(var old in snapshot.Where(t=>!visible.Contains(t.Page)&&!owner.TabPages.Contains(t.Page)).TakeLast(24))
            tabTracks.Add(new(old,ExitBounds(old.Bounds),0,true));
        TabMotionEndReason="running";hover=-1;overPlus=false;plusFrom=beforePlus;plusTo=TargetPlus;tabProgress=0;
        tabMotion.Start(0,1,t=>{tabProgress=t;Invalidate();},280,()=>{TabMotionEndReason="completed";tabTracks.Clear();Invalidate();});
    }
    internal void FinishTabMotion(string reason="explicit")
    {
        TabMotionEndReason=reason;tabMotion.Stop();tabTracks.Clear();beforeTabs=null;tabProgress=1;
        Invalidate();
    }
    void PaintTabMotion(Graphics g)
    {
        if(!Motion.Allowed&&tabMotion.Running){FinishTabMotion();return;}
        foreach(var track in tabTracks.Where(t=>t.Exiting))
            PaintTab(g,track.From.Caption,Mix(track.From.Bounds,track.To,tabProgress),track.From.Active,false,track.From.Opacity*(1-tabProgress));
    }
    void PaintTab(Graphics g,string caption,Rectangle r,bool active,bool hovered,float opacity)
    {
        if(r.IsEmpty||r.Width<2||r.Height<2||opacity<=.01f)return;
        var state=g.Save();g.SetClip(r,System.Drawing.Drawing2D.CombineMode.Intersect);
        Color Fade(Color c)=>Motion.Blend(colors.Background,c,opacity);
        if(active||hovered){using var path=Shapes.Round(r,D(10));using var fill=new SolidBrush(Fade(active?colors.Elevated:colors.Surface));g.FillPath(fill,path);}
        Glyphs.Draw(g,"globe",new(r.X+D(10),r.Y+(r.Height-D(16))/2,D(16),D(16)),Fade(active?colors.Accent:colors.Muted),1.5f);
        if(!Compact&&r.Width>=D(100))
        {
            using var f=new Font("Segoe UI",9.2f,active?FontStyle.Bold:FontStyle.Regular);
            TextRenderer.DrawText(g,caption,f,new Rectangle(r.X+D(34),r.Y,Math.Max(1,r.Width-D(60)),r.Height),Fade(active?colors.Text:colors.Muted),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.PreserveGraphicsClipping);
            if(active||hovered)Glyphs.Draw(g,"close",new(r.Right-D(23),r.Y+(r.Height-D(14))/2,D(14),D(14)),Fade(colors.Muted));
        }
        g.Restore(state);
    }
}
