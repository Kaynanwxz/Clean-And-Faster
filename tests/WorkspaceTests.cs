using KayBrowser;
using System.Text.Json;

static class WorkspaceTests
{
    public static int Run()
    {
        int passed=0;void Check(bool ok){if(!ok)throw new Exception("Falha de persistência/personalização");passed++;}
        Check(VisualPresets.All.Length==10);Check(VisualPresets.All.Select(v=>v.Name).Distinct().Count()==10);
        var preferences=new Preferences();foreach(var preset in VisualPresets.All){VisualPresets.Apply(preferences,preset.Name);Check(preferences.Visual==preset.Name&&preferences.Theme==preset.Theme&&preferences.VerticalTabs==preset.Vertical);}
        Check(WebLinks.Normalize("example.com")=="https://example.com/");
        foreach(string bad in new[]{"javascript://alert(1)","file:///C:/secret","https://u:p@example.com","http://","ftp://site"})
        {try{WebLinks.Normalize(bad);throw new Exception("Endereço inseguro aceito");}catch(ArgumentException){passed++;}}
        // A legacy settings file must migrate once and preserve existing identities and data.
        string old="""{"Theme":"Escuro","Accent":"#FF0000","Session":["https://example.com"],"GoogleClientId":"keep-client","MemoryBudgetMb":2048}""";
        File.WriteAllText(Path.Combine(Storage.Root,"settings.json"),old);
        var migrated=new Storage();Check(migrated.Settings.Workspaces.Count==3);Check(migrated.Settings.Workspaces[0].Urls.Single()=="https://example.com");Check(migrated.Settings.GoogleClientId=="keep-client");Check(migrated.Settings.MemoryBudgetMb==2048);Check(migrated.Settings.Visual==VisualPresets.DefaultName);
        Check(migrated.Settings.AppearanceRevision==2&&!migrated.Settings.DarkChrome&&migrated.Settings.Theme=="Claro"&&migrated.Settings.Compact&&migrated.Settings.FrequentShortcuts&&migrated.Settings.HomeLayout=="Essencial"&&migrated.Settings.Background=="Arte");
        Check(File.ReadAllText(Path.Combine(Storage.Root,"settings.json")).Contains("AppearanceRevision"));
        migrated.Settings.Shortcuts=[new("a","Example","https://example.com")];migrated.Settings.Workspaces[1].Urls=["https://other.example"];migrated.Settings.ActiveWorkspace=migrated.Settings.Workspaces[1].Id;migrated.Settings.VerticalTabs=true;migrated.Settings.FocusMode=true;migrated.Settings.DarkChrome=true;migrated.Settings.HomeLayout="Editorial";migrated.SaveSettings();
        migrated.Library.Add(new("n","Minha nota","","note","Texto <script>é texto</script>",DateTime.UtcNow));migrated.Library.Add(new("l","Leitura","https://example.com","reading","",DateTime.UtcNow));migrated.SaveLibrary();
        var restored=new Storage();Check(restored.Settings.Shortcuts.Single().Name=="Example");Check(restored.Settings.Workspaces[1].Urls.Single()=="https://other.example");Check(restored.Settings.ActiveWorkspace==restored.Settings.Workspaces[1].Id);Check(restored.Settings.VerticalTabs&&restored.Settings.FocusMode&&restored.Settings.DarkChrome&&restored.Settings.HomeLayout=="Editorial");Check(restored.Library.Count==2);Check(restored.Library[0].Text.Contains("<script>"));
        File.WriteAllText(Path.Combine(Storage.Root,"settings.json"),"""{"AppearanceRevision":1,"Visual":"Copiloto Azul","Shortcuts":[{"Id":"custom","Name":"Meu site","Url":"https://example.com"}],"GoogleClientId":"preserve","MemoryBudgetMb":4096,"Wallpaper":"keep.png"}""");
        var upgraded=new Storage();Check(upgraded.Settings.Visual==VisualPresets.DefaultName&&!upgraded.Settings.DarkChrome&&upgraded.Settings.Compact);
        Check(upgraded.Settings.Shortcuts.Single().Id=="custom"&&upgraded.Settings.GoogleClientId=="preserve"&&upgraded.Settings.MemoryBudgetMb==4096&&upgraded.Settings.Wallpaper=="keep.png");
        // Null containers in old or edited files must not crash migration.
        File.WriteAllText(Path.Combine(Storage.Root,"settings.json"),"""{"Workspaces":null,"Shortcuts":null,"Session":null}""");var tolerant=new Storage();Check(tolerant.Settings.Workspaces.Count==3);Check(tolerant.Settings.Shortcuts.Count==0);tolerant.SaveSettings();
        return passed;
    }
}
