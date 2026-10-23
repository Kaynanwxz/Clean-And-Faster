using System.Drawing;
namespace KayBrowser;
// Paint and hit testing share these bounds, including the last incomplete row.
public static class ShortcutLayout
{
    public static int Columns(int count,int width,int step,int padding) => Math.Min(Math.Max(1,count),Math.Clamp((width-padding)/Math.Max(1,step),1,6));
    public static Rectangle Tile(int index,int count,int columns,int width,int step,int tile,int rowStep,int top,int firstRow)
    {
        int row=index/columns;
        int items=Math.Min(columns,count-row*columns);
        int rowWidth=(items-1)*step+tile;
        return new((width-rowWidth)/2+index%columns*step,top+(row-firstRow)*rowStep,tile,tile);
    }
}
