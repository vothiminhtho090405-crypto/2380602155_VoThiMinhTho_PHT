
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Admin: quản lý người dùng.
/// </summary>
public class UsersPage : StackPage
{
    public UsersPage() : base("Quản lý người dùng")
    {
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "+ Thêm",
            Command = new Command(async () =>
            {
                await Navigation.PushAsync(
                    new UserEditPage(null));
            })
        });
    }

    protected override async Task LoadAsync()
    {
        Body.Children.Clear();

        try
        {
            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            Body.Children.Add(
                UI.H1($"Người dùng ({users.Count})"));

            foreach (var user in users
                .OrderBy(x => x.Role)
                .ThenBy(x => x.FullName))
            {
                var color = user.Role == Role.Admin
                    ? UI.Danger
                    : user.Role == Role.GiangVien
                        ? UI.Warn
                        : UI.Success;

                var actions = UI.Row(
                    UI.Btn(
                        "Sửa",
                        async (s, e) =>
                        {
                            await Navigation.PushAsync(
                                new UserEditPage(user));
                        },
                        UI.Primary,
                        true));

                if (user.Id != AppState.Me?.Id)
                {
                    actions.Children.Add(
                        UI.Btn(
                            "Xóa",
                            async (s, e) => await Delete(user),
                            UI.Danger,
                            true));
                }

                Body.Children.Add(
                    UI.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 5,
                            Children =
                            {
                                UI.Txt(user.FullName, 16, true),

                                UI.Sub(
                                    $"@{user.Username} · {user.Email}"),

                                UI.Sub(
                                    $"Mã số: " +
                                    $"{(string.IsNullOrEmpty(user.Code) ? "—" : user.Code)}"),

                                UI.Badge(
                                    Role.Ten(user.Role),
                                    color),

                                actions
                            }
                        }));
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải người dùng",
                ex.Message);
        }
    }

    private async Task Delete(User user)
    {
        try
        {
            if (user.Id == AppState.Me?.Id)
            {
                await UI.Alert(
                    "Không thể xóa",
                    "Bạn không thể xóa tài khoản đang đăng nhập.");

                return;
            }

            if (user.Role == Role.GiangVien)
            {
                int classCount = await Db.C
                    .Table<ClassRoom>()
                    .Where(c => c.TeacherId == user.Id)
                    .CountAsync();

                if (classCount > 0)
                {
                    await UI.Alert(
                        "Không thể xóa",
                        "Giảng viên này đang phụ trách lớp. " +
                        "Hãy đổi giảng viên của lớp hoặc xóa lớp trước.");

                    return;
                }
            }

            var confirmed = await UI.Confirm(
                $"Xóa người dùng “{user.FullName}”?");

            if (!confirmed)
                return;

            await Db.DeleteUser(user.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi xóa người dùng",
                ex.Message);
        }
    }
}
