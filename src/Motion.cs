using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KayBrowser;

public static class Motion
{
    public static bool Enabled { get; set; } = true;
    [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action,uint parameter,out bool value,uint flags);
    public static bool Allowed => Enabled && !SystemInformation.TerminalServerSession && SystemParametersInfo(0x1042,0,out bool animate,0) && animate;
    public static Color Blend(Color a, Color b, float amount)
    {
        float t = Math.Clamp(amount, 0, 1);
        return Color.FromArgb((int)(a.R+(b.R-a.R)*t), (int)(a.G+(b.G-a.G)*t), (int)(a.B+(b.B-a.B)*t));
    }
}
// Timers exist only for the duration of an interaction; no idle animation loop.
public sealed class Tween : IDisposable
{
    System.Windows.Forms.Timer? timer;
    public bool Running => timer != null;
    public void Start(float from, float to, Action<float> update, int duration = 180, Action? completed = null)
    {
        Stop();
        if(!Motion.Allowed || Math.Abs(from-to)<0.1f) { update(to); completed?.Invoke(); return; }
        long start=Stopwatch.GetTimestamp();
        timer=new() { Interval=16 };
        timer.Tick+=(_,_)=> {
            float t=Math.Min(1,(float)Stopwatch.GetElapsedTime(start).TotalMilliseconds/duration);
            update(from+(to-from)*(1-MathF.Pow(1-t,3)));
            if(t>=1) { Stop(); completed?.Invoke(); }
        };
        timer.Start();
    }
    public void Stop() { timer?.Stop(); timer?.Dispose(); timer=null; }
    public void Dispose()=>Stop();
}
