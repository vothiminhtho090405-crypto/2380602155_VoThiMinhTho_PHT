
using Android.App;
using Android.Runtime;

namespace VoThiMinhTho_2380602155_PHT;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
}
