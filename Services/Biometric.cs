#if ANDROID
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
#endif

namespace VoThiMinhTho_2380602155_PHT;

public static class Biometric
{
#if ANDROID
    public static bool IsAvailable()
    {
        try
        {
            Android.Content.Context ctx = (Android.Content.Context?)Platform.CurrentActivity ?? Android.App.Application.Context;
            var r = BiometricManager.From(ctx).CanAuthenticate(BiometricManager.Authenticators.BiometricWeak);
            return r == BiometricManager.BiometricSuccess;
        }
        catch { return false; }
    }

    public static Task<bool> AuthenticateAsync(string title, string subtitle)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (Platform.CurrentActivity is not FragmentActivity act) { tcs.SetResult(false); return tcs.Task; }

        var executor = ContextCompat.GetMainExecutor(act);
        var prompt = new BiometricPrompt(act, executor, new Callback(tcs));
        var info = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(title)
            .SetSubtitle(subtitle)
            .SetNegativeButtonText("Hủy")
            .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricWeak)
            .Build();
        prompt.Authenticate(info);
        return tcs.Task;
    }

    sealed class Callback : BiometricPrompt.AuthenticationCallback
    {
        readonly TaskCompletionSource<bool> _tcs;
        public Callback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            _tcs.TrySetResult(true);
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            _tcs.TrySetResult(false);
        }
    }
#else
    // Nền tảng khác (Windows, iOS...): bỏ qua bước vân tay
    public static bool IsAvailable() => false;
    public static Task<bool> AuthenticateAsync(string title, string subtitle) => Task.FromResult(false);
#endif
}