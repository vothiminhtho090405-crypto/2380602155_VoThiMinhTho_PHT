namespace VoThiMinhTho_2380602155_PHT;

public partial class RegisterPage : ContentPage
{
    public RegisterPage() => InitializeComponent();

    async void OnRegister(object? sender, EventArgs e)
    {
        try
        {
            var role = RolePicker.SelectedIndex == 1 ? Role.GiangVien : Role.SinhVien;
            var (user, err) = await Db.Prepare(NameEntry.Text ?? "", EmailEntry.Text ?? "", UserEntry.Text ?? "",
                CodeEntry.Text ?? "", PassEntry.Text ?? "", ConfirmEntry.Text ?? "", role);
            if (user == null) { await DisplayAlert("Chưa hợp lệ", err, "OK"); return; }
            var mailed = await OtpService.SendAsync(user.Email);
            await Navigation.PushAsync(new OtpPage(user, mailed, true));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Không kết nối được máy chủ", ex.Message, "OK");
        }
    }
}
