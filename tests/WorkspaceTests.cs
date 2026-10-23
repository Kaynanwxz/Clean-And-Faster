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
        var migrated=new Storage();Check(migrated.Settings.Workspaces.Count==3);Check(migrated.Settings.Workspaces[0].Urls.Single()=="https://example.com");Check(migrated.Settings.GoogleClientId=="keep-client");Check(migrated.Settings.MemoryBudgetMb==2048);Check(migrated.Settings.Visual=="Copiloto Azul");
        migrated.Settings.Shortcuts=[new("a","Example","https://example.com")];migrated.Settings.Workspaces[1].Urls=["https://other.example"];migrated.Settings.ActiveWorkspace=migrated.Settings.Workspaces[1].Id;migrated.Settings.VerticalTabs=true;migrated.Settings.FocusMode=true;migrated.SaveSettings();
        migrated.Library.Add(new("n","Minha nota","","note","Texto <script>é texto</script>",DateTime.UtcNow));migrated.Library.Add(new("l","Leitura","https://example.com","reading","",DateTime.UtcNow));migrated.SaveLibrary();
        var restored=new Storage();Check(restored.Settings.Shortcuts.Single().Name=="Example");Check(restored.Settings.Workspaces[1].Urls.Single()=="https://other.example");Check(restored.Settings.ActiveWorkspace==restored.Settings.Workspaces[1].Id);Check(restored.Settings.VerticalTabs&&restored.Settings.FocusMode);Check(restored.Library.Count==2);Check(restored.Library[0].Text.Contains("<script>"));
        // Null containers in old or edited files must not crash migration.
        File.WriteAllText(Path.Combine(Storage.Root,"settings.json"),"""{"Workspaces":null,"Shortcuts":null,"Session":null}""");var tolerant=new Storage();Check(tolerant.Settings.Workspaces.Count==3);Check(tolerant.Settings.Shortcuts.Count==0);tolerant.SaveSettings();
        return passed;
    }
}
