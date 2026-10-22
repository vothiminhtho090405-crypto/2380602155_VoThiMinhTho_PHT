namespace VoThiMinhTho_2380602155_PHT;

public partial class LoginPage : ContentPage
{
    public LoginPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PassEntry.Text = "";
    }

    async void OnLogin(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserEntry.Text) || string.IsNullOrEmpty(PassEntry.Text))
        {
            await DisplayAlert("Thiếu thông tin", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
            return;
        }
        Busy.IsVisible = Busy.IsRunning = true;
        try
        {
            var u = await Db.Login(UserEntry.Text, PassEntry.Text);
            if (u == null)
            {
                await DisplayAlert("Đăng nhập thất bại", "Sai tên đăng nhập hoặc mật khẩu.", "OK");
                return;
            }
            var mailed = await OtpService.SendAsync(u.Email);
            await Navigation.PushAsync(new OtpPage(u, mailed, false));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Không kết nối được máy chủ",
                "Kiểm tra: API đang chạy, điện thoại và laptop cùng Wi-Fi, địa chỉ trong Services/Config.cs đúng.\n\n" + ex.Message, "OK");
        }
        finally { Busy.IsVisible = Busy.IsRunning = false; }
    }

    async void OnRegister(object? sender, EventArgs e) => await Navigation.PushAsync(new RegisterPage());
}
