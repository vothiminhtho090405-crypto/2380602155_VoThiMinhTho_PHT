namespace VoThiMinhTho_2380602155_PHT;

public partial class OtpPage : ContentPage
{
    readonly User _user;
    readonly bool _register;
    bool _mailed;

    public OtpPage(User user, bool mailed, bool register)
    {
        InitializeComponent();
        _user = user; _mailed = mailed; _register = register;
        ShowInfo();
    }

    void ShowInfo() => InfoLabel.Text = _mailed
        ? $"Mã gồm 6 số đã được gửi tới {_user.Email}. Mã có hiệu lực 5 phút."
        : "API chưa cấu hình SMTP (appsettings.json) nên đang ở chế độ DEMO.";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_mailed)
            await DisplayAlert("Chế độ DEMO", $"Mã OTP của bạn là: {OtpService.DemoCode}", "OK");
    }

    async void OnVerify(object? sender, EventArgs e)
    {
        var err = await OtpService.VerifyAsync(OtpEntry.Text ?? "");
        if (err != null) { await DisplayAlert("Lỗi", err, "OK"); return; }

        try
        {
            if (_register)
            {
                await Db.C.InsertAsync(_user);
                await DisplayAlert("Thành công", "Đăng ký thành công! Hãy đăng nhập.", "OK");
                Nav.Logout();
            }
            else Nav.GoHome(_user);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không lưu được tài khoản: " + ex.Message, "OK");
        }
    }

    async void OnResend(object? sender, EventArgs e)
    {
        _mailed = await OtpService.SendAsync(_user.Email);
        ShowInfo();
        OtpEntry.Text = "";
        if (!_mailed) await DisplayAlert("Chế độ DEMO", $"Mã OTP mới: {OtpService.DemoCode}", "OK");
        else await DisplayAlert("Đã gửi", "Đã gửi lại mã OTP vào email của bạn.", "OK");
    }
}
