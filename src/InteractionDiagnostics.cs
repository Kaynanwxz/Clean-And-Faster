using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace KayBrowser;

public sealed partial class BrowserWindow
{
    [DllImport("user32.dll")]static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
    internal async Task<string[]> VerifyInteractions()
    {
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),"BEGIN interactions; thread="+Environment.CurrentManagedThreadId+"; context="+SynchronizationContext.Current?.GetType().Name+Environment.NewLine);
        var checks=new List<string>();var errors=new List<Exception>();
        void UiError(object sender,System.Threading.ThreadExceptionEventArgs e)=>errors.Add(e.Exception);
        Application.ThreadException+=UiError;
        void Check(bool valid,string label){File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),(valid?"PASS ":"FAIL ")+label+Environment.NewLine);if(!valid)throw new InvalidOperationException(label);checks.Add(label);}
        async Task Settle(int ms=30){await Task.Delay(ms);if(errors.Count>0)throw new InvalidOperationException("Falha de interface",errors[0]);Check(!IsDisposed&&!closing,"Janela continua aberta");}
        void ClickAt(Point screen){Cursor.Position=screen;mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);}
        try
        {
            store.Settings.Animations=false;Motion.Enabled=false;CloseBrowserMenu(false);if(assistantOpen)ToggleAssistant();if(appearanceOpen)ShowAppearance();
            for(int i=0;i<20;i++)
            {
                ShowMenu();await Settle();Check(menuOpen&&browserMenu!.Visible,"Menu abre "+i);
                ClickAt(PointToScreen(new Point(Ui(30),Ui(120))));await Settle();Check(!menuOpen&&!browserMenu!.Visible,"Clique fora fecha somente menu "+i+"; open="+menuOpen+"; visible="+browserMenu!.Visible+"; "+string.Join(" / ",menuTrace));
            }
            checks.AddRange(await VerifySidebarDismissal());
            ShowMenu();await Settle();File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"interaction-progress.log"),"ESCAPE"+Environment.NewLine);SendKeys.SendWait("{ESC}");await Settle();Check(!menuOpen,"Escape fecha menu");
            ShowMenu();await Settle();using(var other=new Form{Size=new Size(160,90),StartPosition=FormStartPosition.Manual,Location=new Point(20,20)}){other.Show();other.Activate();await Settle(60);Check(!menuOpen,"Perda de foco fecha menu");other.Close();}Activate();
            foreach(int group in new[]{0,1,2}){if(!menuOpen)ShowMenu();browserMenu!.SelectGroup(group);await Settle();SaveChromeScreenshot("layout-menu-"+group+".png");}CloseBrowserMenu(false);
            var dialogs=new HashSet<string>();bool cancelPickers=false;
            using var closer=new System.Windows.Forms.Timer{Interval=25};
            closer.Tick+=(_,_)=>{
                foreach(var dialog in Application.OpenForms.Cast<Form>().OfType<AppDialog>().ToArray())
                {dialogs.Add(dialog.Text);if(dialog is UpdateCenter){var close=AppearancePanel.Descendants(dialog).OfType<Button>().FirstOrDefault(b=>b.Text is "Cancelar" or "Fechar");close?.PerformClick();}else dialog.Close();}
                if(cancelPickers){var h=GetForegroundWindow();var cls=new StringBuilder(64);GetClassName(h,cls,64);if(cls.ToString()=="#32770")SendMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);}
            };
            closer.Start();
            foreach(var command in MenuCommands())
            {
                if(!menuOpen)ShowMenu();lastMenuCommand=null;browserMenu!.ClickCommand(command.Id);await Settle(command.Id=="extensions"?900:100);if(command.Id=="extensions"){var deadline=DateTime.UtcNow.AddSeconds(8);while(!dialogs.Contains("Extensões · por pasta")&&DateTime.UtcNow<deadline)await Settle(100);}
                Check(lastMenuCommand==command.Id,"Botão de menu: "+command.Id);
                if(command.Id is "zoom-in" or "zoom-out")CloseBrowserMenu(false);
                if(appearanceOpen)ShowAppearance();if(assistantOpen)ToggleAssistant();if(fullscreen)Fullscreen();
            }
            Check(dialogs.Contains("Histórico")&&dialogs.Contains("Favoritos")&&dialogs.Contains("Downloads desta sessão"),"Histórico, favoritos e downloads abriram");
            Check(dialogs.Contains("Espaços de trabalho")&&dialogs.Contains("Biblioteca · leitura, fontes e notas")&&dialogs.Contains("Memória e abas"),"Organização e memória abriram");
            Check(dialogs.Contains("Extensões · por pasta"),"Gerenciador de extensões abriu");
            Check(Current?.Page.Controls.OfType<HomeBoard>().Any()==true,"Extensões preservam página inicial");
            Check(dialogs.Contains("Configurações avançadas")&&dialogs.Contains("Buscar nesta página")&&dialogs.Contains("Atualizações do Clean Browser"),"Configurações, busca e atualização abriram");
            ShowAppearance();await Settle();var panel=appearance!;
            foreach(var v in VisualPresets.All){panel.ClickChoice("Visual: "+v.Name);await Settle();Check(store.Settings.Visual==v.Name&&new Storage().Settings.Visual==v.Name,"Preset por botão e persistência: "+v.Name);}
            panel.ClickChoice("Visual: Copiloto Azul");
            foreach(var (option,verify) in new (string,Func<bool>)[]{("Escuro",()=>store.Settings.Theme=="Escuro"),("Claro",()=>store.Settings.Theme=="Claro"),("Lateral",()=>store.Settings.VerticalTabs),("Superior",()=>!store.Settings.VerticalTabs),("Clara",()=>!store.Settings.DarkChrome),("Azul-marinho",()=>store.Settings.DarkChrome),("Compacto",()=>store.Settings.Compact),("Confortável",()=>!store.Settings.Compact),("Ocultar",()=>!store.Settings.ShowShortcuts),("Mais visitados",()=>store.Settings.FrequentShortcuts),("Meus atalhos",()=>!store.Settings.FrequentShortcuts&&store.Settings.ShowShortcuts)})
            {panel.ClickChoice(option);await Settle();Check(verify(),"Opção imediata: "+option);VerifyChromeLayout();}
            foreach(string layout in new[]{"Clássico","Central","Editorial","Essencial","Espaços","Cristal","Biblioteca"}){panel.ClickChoice(layout);await Settle();Check(store.Settings.HomeLayout==layout,"Layout por botão: "+layout);}
            foreach(string bg in new[]{"Arte","Suave","Sem arte"}){panel.ClickChoice(bg);Check(store.Settings.Background==bg,"Fundo por botão: "+bg);}
            foreach(string hex in new[]{"#8FBBEF","#A9C9EF","#5D96D5","#C4D2E2","#F2F5FA"}){panel.ClickChoice("Destaque "+hex);Check(store.Settings.Accent==hex,"Cor por botão: "+hex);}
            string before=store.Settings.Accent;panel.SetAccentForDiagnostic("#XX0000");Check(store.Settings.Accent==before,"Cor inválida não altera configuração");panel.SetAccentForDiagnostic("#123ABC");Check(new Storage().Settings.Accent=="#123ABC","Cor digitada salva imediatamente");
            panel.SelectPage(1,false);var flags=AppearancePanel.Descendants(panel).OfType<CheckBox>().ToArray();foreach(var flag in flags){bool old=flag.Checked;flag.Checked=!old;bool actual=flag.AccessibleName switch{"Modo foco"=>store.Settings.FocusMode,"Mostrar barra inferior"=>store.Settings.ShowBottomBar,"Animações e transições"=>store.Settings.Animations,_=>throw new InvalidOperationException("Checkbox sem verificação: "+flag.AccessibleName)};Check(actual==flag.Checked,"Alternar "+flag.Text);if(flag.AccessibleName=="Mostrar barra inferior")Check(new Storage().Settings.ShowBottomBar==flag.Checked,"Barra inferior persiste pela opção da interface");flag.Checked=old;}
            store.Settings.Animations=false;Motion.Enabled=false;cancelPickers=true;
            panel.ClickChoice("Personalizado");await Settle();panel.ClickChoice("Escolher imagem");await Settle();Check(string.IsNullOrEmpty(store.Settings.Wallpaper),"Cancelar seletor de imagem preserva fundo");
            string imageFixture=Path.Combine(Storage.Root,"wallpaper-fixture.png");using(var bitmap=new Bitmap(75,35)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Coral);bitmap.Save(imageFixture,System.Drawing.Imaging.ImageFormat.Png);}
            SetWallpaperFromFile(imageFixture);panel.Sync();string copied=store.Settings.Wallpaper;Check(File.Exists(copied)&&new Storage().Settings.Background=="Personalizado","Imagem personalizada importada e persistida");
            panel.ClickChoice("Arte");Check(store.Settings.Background=="Arte"&&File.Exists(copied),"Arte padrão selecionada preserva imagem para reutilização");panel.ClickChoice("Personalizado");Check(store.Settings.Background=="Personalizado","Retornar à imagem personalizada sem novo seletor");
            panel.ClickChoice("Remover imagem");Check(!File.Exists(copied),"Remover imagem limpa arquivo próprio");Check(store.Settings.Wallpaper==""&&store.Settings.Background=="Sem arte","Remover imagem funciona");
            var reset=AppearancePanel.Descendants(panel).OfType<Button>().Single(b=>b.AccessibleName=="Restaurar aparência");reset.PerformClick();Check(store.Settings.Visual==VisualPresets.DefaultName&&store.Settings.Accent=="#8FBBEF"&&!store.Settings.DarkChrome&&store.Settings.Compact&&store.Settings.FrequentShortcuts&&!store.Settings.FocusMode,"Restaurar redefine escolhas coerentes");store.Settings.Animations=false;Motion.Enabled=false;
            for(int i=0;i<3;i++){panel.SelectPage(i,false);await Settle();SaveChromeScreenshot("layout-appearance-section-"+i+".png");}
            AppearancePanel.Descendants(panel).OfType<Button>().Single(b=>b.AccessibleName=="Fechar aparência").PerformClick();await Settle();Check(!appearanceOpen,"Botão fechar aparência");
            // Exercise every navigation toolbar button except the intentional exit; modal dialogs auto-cancel.
            foreach(var button in AppearancePanel.Descendants(navigationRow!).OfType<Button>().ToArray()){button.PerformClick();await Settle(150);Check(true,"Barra: "+button.AccessibleName);if(menuOpen)CloseBrowserMenu(false);if(assistantOpen)ToggleAssistant();}
            // AI buttons: no paid calls, file writes outside the isolated fixture, or real credentials.
            ToggleAssistant();await Settle();assistant!.Prompt="";
            foreach(var button in AppearancePanel.Descendants(assistant).OfType<Button>().Where(b=>b.Enabled).ToArray())
            {button.PerformClick();await Settle(40);Check(true,"IA: "+(button.AccessibleName??button.Text));if(!assistantOpen)ToggleAssistant();}
            using(var stopTask=new CancellationTokenSource()){aiCancellation=stopTask;assistant!.Busy(true);AppearancePanel.Descendants(assistant).OfType<Button>().Single(b=>b.Text=="Parar").PerformClick();Check(stopTask.IsCancellationRequested,"Parar IA cancela tarefa");assistant.Busy(false);aiCancellation=null;}
            if(assistantOpen)ToggleAssistant();closer.Stop();checks.AddRange(await VerifyDialogFlows());checks.AddRange(await VerifyNavigationButtons());
            // Run actual timer frames even on the remote CI desktop, then verify they stop.
            Motion.DiagnosticAnimations=true;Motion.Enabled=true;store.Settings.Animations=true;
            ShowMenu();await Settle(340);Check(!menuMotion.Running&&menuOpen,"Animação do menu termina");CloseBrowserMenu();await Settle(250);Check(!menuMotion.Running&&!browserMenu!.Visible,"Animação de saída termina");
            ShowAppearance();await Settle(340);Check(!assistantTween.Running&&appearance!.Left==0,"Deslize do painel termina alinhado");appearance!.SelectPage(1);await Settle(260);ShowAppearance();await Settle(280);Check(!assistantTween.Running&&pageHost.ColumnStyles[1].Width==0,"Painel fecha sem coluna residual");
            for(int i=0;i<12;i++){ShowMenu();CloseBrowserMenu();ShowAppearance();ShowAppearance();}await Settle(340);Check(!menuMotion.Running&&!assistantTween.Running,"Cliques rápidos não deixam timers pendentes");
            Motion.Enabled=false;ShowMenu();Check(!menuMotion.Running,"Redução de animações respeitada");CloseBrowserMenu(false);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"interaction-check.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));return checks.ToArray();
        }
        finally{Application.ThreadException-=UiError;Motion.DiagnosticAnimations=null;Motion.Enabled=false;store.Settings.Animations=false;CloseBrowserMenu(false);}
    }
}
