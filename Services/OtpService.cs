using System.Net.Http.Json;

namespace VoThiMinhTho_2380602155_PHT;

public static class OtpService
{
    static string _email = "";
    static string _demo = "";

    public static string DemoCode => _demo;

    public static async Task<bool> SendAsync(string email)
    {
        _email = email;
        _demo = "";
        try
        {
            var r = await Api.Http.PostAsJsonAsync("api/auth/send-otp", new { email }, Api.Json);
            r.EnsureSuccessStatusCode();
            var res = await r.Content.ReadFromJsonAsync<SendRes>(Api.Json);
            _demo = res?.DemoCode ?? "";
            return res?.Mailed ?? false;
        }
        catch { return false; }
    }

    public static async Task<string?> VerifyAsync(string input)
    {
        try
        {
            var r = await Api.Http.PostAsJsonAsync("api/auth/verify-otp", new { email = _email, code = input }, Api.Json);
            r.EnsureSuccessStatusCode();
            var res = await r.Content.ReadFromJsonAsync<VerifyRes>(Api.Json);
            return res != null && res.Ok ? null : (res?.Message ?? "Không xác thực được OTP.");
        }
        catch { return "Không kết nối được máy chủ."; }
    }

    record SendRes(bool Mailed, string? DemoCode);
    record VerifyRes(bool Ok, string? Message);
}