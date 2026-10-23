using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
// Package the approved mark at native Windows sizes, with a transparent safe area.
using var source=Image.FromFile(args[0]);
var sizes=new[]{16,20,24,32,40,48,64,96,128,256};var images=new List<byte[]>();
foreach(int size in sizes)
{
    using var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb);
    using(var g=Graphics.FromImage(bitmap))
    {
        g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        float margin=Math.Max(1,size/16f),side=size-margin*2,radius=side*.12f,d=radius*2;
        var rect=new RectangleF(margin,margin,side,side);using var clip=new GraphicsPath();
        clip.AddArc(rect.Left,rect.Top,d,d,180,90);clip.AddArc(rect.Right-d,rect.Top,d,d,270,90);
        clip.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);clip.AddArc(rect.Left,rect.Bottom-d,d,d,90,90);clip.CloseFigure();
        g.SetClip(clip);g.DrawImage(source,rect,new RectangleF(source.Width*7f/256,source.Height*7f/256,source.Width*242f/256,source.Height*242f/256),GraphicsUnit.Pixel);
    }
    using var data=new MemoryStream();bitmap.Save(data,ImageFormat.Png);images.Add(data.ToArray());
}
using var output=File.Create(args[1]);using var writer=new BinaryWriter(output);
writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
for(int i=0;i<sizes.Length;i++)
{
    writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);
    writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;
}
foreach(var bytes in images)writer.Write(bytes);
Console.WriteLine("Windows icon: ten sizes, transparent corners, approved navy artwork.");
