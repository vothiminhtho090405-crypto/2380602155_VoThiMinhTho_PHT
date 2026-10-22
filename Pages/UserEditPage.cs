
using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;

namespace VoThiMinhTho_2380602155_PHT;

public class UserEditPage : StackPage
{
    private readonly User? _user;

    private readonly Entry _name = UI.Field("Họ và tên");
    private readonly Entry _username = UI.Field("Tên đăng nhập");
    private readonly Entry _email = UI.Field("Email", false, Keyboard.Email);
    private readonly Entry _code = UI.Field("Mã số (MSSV / MSGV)");
    private readonly Entry _password;

    private readonly Picker _role = new()
    {
        Title = "Vai trò",
        ItemsSource = new[]
        {
            "Quản trị viên",
            "Giảng viên",
            "Sinh viên"
        }
    };

    private static readonly string[] Roles =
    {
        Role.Admin,
        Role.GiangVien,
        Role.SinhVien
    };

    private bool _isSaving;

    public UserEditPage(User? user)
        : base(user == null ? "Thêm người dùng" : "Sửa người dùng")
    {
        _user = user;

        _password = UI.Field(
            user == null
                ? "Mật khẩu"
                : "Mật khẩu mới (bỏ trống nếu không đổi)",
            true);

        if (user != null)
        {
            _name.Text = user.FullName;
            _username.Text = user.Username;
            _email.Text = user.Email;
            _code.Text = user.Code;

            _role.SelectedIndex = Array.IndexOf(Roles, user.Role);
        }
        else
        {
            _role.SelectedIndex = 2;
        }

        Body.Children.Add(
            UI.Card(
                new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        _name,
                        _username,
                        _email,
                        _code,
                        _role,
                        _password,
                        UI.Btn("Lưu", OnSave)
                    }
                }));
    }

    protected override Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        _isSaving = true;

        try
        {
            var name = _name.Text?.Trim() ?? string.Empty;
            var username = _username.Text?.Trim().ToLowerInvariant()
                ?? string.Empty;
            var email = _email.Text?.Trim().ToLowerInvariant()
                ?? string.Empty;
            var code = _code.Text?.Trim() ?? string.Empty;
            var password = _password.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email))
            {
                await UI.Alert(
                    "Thiếu thông tin",
                    "Nhập họ tên, tên đăng nhập và email.");
                return;
            }

            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                await UI.Alert("Lỗi", "Email không hợp lệ.");
                return;
            }

            if (_role.SelectedIndex < 0 ||
                _role.SelectedIndex >= Roles.Length)
            {
                await UI.Alert("Lỗi", "Hãy chọn vai trò.");
                return;
            }

            if (_user == null && password.Length < 6)
            {
                await UI.Alert(
                    "Lỗi",
                    "Mật khẩu tối thiểu 6 ký tự.");
                return;
            }

            if (!string.IsNullOrEmpty(password) &&
                password.Length < 6)
            {
                await UI.Alert(
                    "Lỗi",
                    "Mật khẩu tối thiểu 6 ký tự.");
                return;
            }

            int currentUserId = _user?.Id ?? 0;

            int usernameExists = await Db.C
                .Table<User>()
                .Where(x =>
                    x.Username == username &&
                    x.Id != currentUserId)
                .CountAsync();

            if (usernameExists > 0)
            {
                await UI.Alert(
                    "Lỗi",
                    "Tên đăng nhập đã tồn tại.");
                return;
            }

            int emailExists = await Db.C
                .Table<User>()
                .Where(x =>
                    x.Email == email &&
                    x.Id != currentUserId)
                .CountAsync();

            if (emailExists > 0)
            {
                await UI.Alert(
                    "Lỗi",
                    "Email đã tồn tại.");
                return;
            }

            var role = Roles[_role.SelectedIndex];

            if (_user == null)
            {
                var newUser = Db.Make(
                    username,
                    email,
                    name,
                    code,
                    password,
                    role);

                await Db.C.InsertAsync(newUser);
            }
            else
            {
                _user.FullName = name;
                _user.Username = username;
                _user.Email = email;
                _user.Code = code;
                _user.Role = role;

                if (!string.IsNullOrEmpty(password))
                {
                    _user.Salt = Guid.NewGuid().ToString("N");
                    _user.PasswordHash = Db.Hash(
                        password,
                        _user.Salt);
                }

                await Db.C.UpdateAsync(_user);

                if (AppState.Me?.Id == _user.Id)
                {
                    AppState.Me = _user;
                }
            }

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi lưu người dùng",
                ex.Message);
        }
        finally
        {
            _isSaving = false;
        }
    }
}
