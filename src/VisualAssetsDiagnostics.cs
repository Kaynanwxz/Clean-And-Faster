using System.Drawing.Imaging;
namespace KayBrowser;
public sealed partial class BrowserWindow
{
    internal void VerifyVisualAssets()
    {
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        Check(store.Settings.Visual==VisualPresets.DefaultName&&!store.Settings.DarkChrome&&store.Settings.Compact&&store.Settings.HomeLayout=="Essencial","Padrão diferente das referências claras.");
        Check(BrandAssets.Engraving.Size==new Size(1672,941),"Gravura aprovada perdeu resolução original.");
        using(var original=typeof(BrandAssets).Assembly.GetManifestResourceStream(typeof(BrandAssets).Assembly.GetManifestResourceNames().Single(n=>n.EndsWith(".apollo-engraving.png")))!)Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)).Equals("1741291c368581f85b79f63a089697f8526d524af58f63160216d4349724a6cf",StringComparison.OrdinalIgnoreCase),"Gravura difere da versão 1 aprovada.");
        Check(!store.Settings.PointillistBackground,"Gravura aprovada não está selecionada no padrão.");
        foreach(var size in new[]{new Size(1920,1080),new Size(900,600),new Size(510,450),new Size(2560,1080)})
        {
            var view=new Rectangle(Point.Empty,size);var art=BrandAssets.Cover(BrandAssets.Sea.Size,view);
            Check(art.Contains(view),"Arte não cobre toda a página: "+size);
            Check(Math.Abs(art.Left+(art.Width-view.Width)/2)<=1&&Math.Abs(art.Top+(art.Height-view.Height)/2)<=1,"Arte fora do centro: "+size);
            Check(Math.Abs((double)art.Width/art.Height-(double)BrandAssets.Sea.Width/BrandAssets.Sea.Height)<.01,"Arte distorcida.");
        }
        Check(BrandMarks.ShortName("https://www.google.com/")=="Google"&&BrandMarks.ShortName("https://m.youtube.com/")=="YouTube"&&BrandMarks.ShortName("https://github.com/")=="GitHub","Marcas não reconhecidas.");
        Check(BrandMarks.ShortName("https://google.com.evil.test/")==null&&BrandMarks.ShortName("https://notgithub.com/")==null,"Marca em domínio incorreto.");
        using(var marks=new Bitmap(400,120))
        {
            using(var g=Graphics.FromImage(marks))
            {
                g.Clear(Color.FromArgb(249,251,254));
                foreach(var (url,index) in new[]{("https://google.com",0),("https://github.com",1),("https://youtube.com",2)})
                    foreach(int pixels in new[]{16,28,48})Check(BrandMarks.Draw(g,url,new Rectangle(index*130+(pixels==16?0:pixels==28?25:65),30,pixels,pixels),Color.FromArgb(9,34,65)),"Marca não desenhada.");
            }
            marks.Save(Path.Combine(AppContext.BaseDirectory,"layout-shortcut-vectors.png"),ImageFormat.Png);
        }
        using var iconStream=typeof(BrandAssets).Assembly.GetManifestResourceStream("CleanBrowser.kay.ico")!;
        using var iconReader=new BinaryReader(iconStream);
        Check(iconReader.ReadUInt16()==0&&iconReader.ReadUInt16()==1,"Cabeçalho ICO inválido.");
        int frames=iconReader.ReadUInt16();Check(frames==10,"Quantidade de tamanhos ICO incorreta.");
        var entries=new List<(int Width,int Height,int Length,int Offset)>();
        for(int i=0;i<frames;i++)
        {
            int width=iconReader.ReadByte(),height=iconReader.ReadByte();iconReader.ReadUInt16();iconReader.ReadUInt16();iconReader.ReadUInt16();
            entries.Add((width==0?256:width,height==0?256:height,iconReader.ReadInt32(),iconReader.ReadInt32()));
        }
        foreach(int size in new[]{16,20,24,32,40,48,64,96,128,256})
        {
            var entry=entries.Single(e=>e.Width==size&&e.Height==size);iconStream.Position=entry.Offset;
            using var data=new MemoryStream(iconReader.ReadBytes(entry.Length));using var bitmap=new Bitmap(data);
            Check(bitmap.Width==size&&bitmap.Height==size,"Tamanho de ícone ausente: "+size);
            Check(bitmap.GetPixel(0,0).A==0,"Ícone com placa opaca: "+size);
            if(size==32)bitmap.Save(Path.Combine(AppContext.BaseDirectory,"layout-taskbar-icon32.png"),ImageFormat.Png);
        }
        var history=store.History.ToArray();store.History.Clear();
        try
        {
            foreach(var size in new[]{new Size(1920,900),new Size(980,660)})
            {
                using var home=new HomeBoard(store,_=>{},()=>{},()=>{},(_,_)=>{},_=>{},_=>{},()=>{}){Size=size,Font=Font};
                home.SetPalette(Colors);home.CreateControl();home.VerifyLayout();
                using var bitmap=new Bitmap(size.Width,size.Height);home.DrawToBitmap(bitmap,new Rectangle(Point.Empty,size));
                bitmap.Save(Path.Combine(AppContext.BaseDirectory,$"layout-home-centered-{size.Width}.png"),ImageFormat.Png);
            }
        }
        finally{store.History.AddRange(history);}
        var previousTheme=store.Settings.Theme;store.Settings.Theme="Escuro";
        using(var paperHome=new HomeBoard(store,_=>{},()=>{},()=>{},(_,_)=>{},_=>{},_=>{},()=>{}){Size=new Size(1320,800),Font=Font}){paperHome.SetPalette(Colors);Check(paperHome.ForeColor==Palette.Create(true,Accent).Text,"Texto da gravura ilegível com barra escura.");}
        store.Settings.Theme=previousTheme;
        ToggleAssistant();Check(assistant!.BackColor==Palette.Create(true,Accent).Surface,"IA não acompanhou tema claro.");ToggleAssistant();
        ShowAppearance();ApplyChromeMetrics();Application.DoEvents();VerifyChromeLayout();SaveChromeScreenshot("layout-default-appearance.png");ShowAppearance();
    }
}