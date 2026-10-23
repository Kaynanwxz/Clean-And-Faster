using KayBrowser;
static class WebAppsTests
{
    public static int Run()
    {
        int n=0;void Check(bool ok){if(!ok)throw new Exception("Apps regression");n++;}
        var original=new Storage();var prior=original.Settings.Apps.ToArray();
        Check(WebApps.Defaults().Select(a=>a.Id).Distinct().Count()==5);
        foreach(string bad in new[]{"javascript:alert(1)","file:///secret","http://example.com","https://user:pass@example.com","data:text/html,bad"}){bool rejected=false;try{WebApps.Create("Teste",bad);}catch(ArgumentException){rejected=true;}Check(rejected);}
        Check(WebApps.Create("  App  ","example.com").Url=="https://example.com/");
        Check(WebApps.SameSite("https://notion.so/a","https://notion.so/"));Check(!WebApps.SameSite("https://notion.so.evil.test","https://notion.so/"));Check(!WebApps.SameSite("http://notion.so","https://notion.so/"));Check(!WebApps.SameSite("https://notion.so:444","https://notion.so/"));
        Check(WebApps.SameSite("https://app.notion.com/login","https://www.notion.com/login"));Check(WebApps.SameSite("https://www.notion.so/workspace","https://www.notion.com/login"));Check(!WebApps.SameSite("https://app.notion.com.evil.test/login","https://www.notion.com/login"));
        var custom=WebApps.Create("Meu app","https://example.com/path");original.Settings.Apps=[custom];original.SaveSettings();var restored=new Storage();Check(restored.Settings.Apps.Single()==custom);
        restored.Settings.Apps.Clear();restored.SaveSettings();Check(new Storage().Settings.Apps.Count==0);
        Check(WebApps.Sanitize([custom,custom,new("bad","Bad","javascript:bad")]).Count==1);
        original.Settings.Apps=prior.ToList();original.SaveSettings();return n;
    }
}
