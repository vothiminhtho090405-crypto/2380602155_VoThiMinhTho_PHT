
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace VoThiMinhTho_2380602155_PHT;

public class ProfilePage : StackPage
{
    public ProfilePage() : base("Tài khoản")
    {
    }

    protected override Task LoadAsync()
    {
        Body.Children.Clear();

        var me = AppState.Me;

        if (me == null)
        {
            Body.Children.Add(
                UI.Sub("Vui lòng đăng nhập để xem thông tin tài khoản."));

            return Task.CompletedTask;
        }

        Body.Children.Add(
            UI.Card(
                new VerticalStackLayout
                {
                    Spacing = 8,

                    Children =
                    {
                        UI.H1(me.FullName),
                        UI.Badge(Role.Ten(me.Role), UI.Primary),
                        UI.Txt($"Tên đăng nhập: {me.Username}", 14),
                        UI.Txt($"Email: {me.Email}", 14),
                        UI.Txt(
                            $"Mã số: {(string.IsNullOrWhiteSpace(me.Code) ? "—" : me.Code)}",
                            14)
                    }
                }));

        Body.Children.Add(
            UI.Btn(
                "📍  Chia sẻ vị trí của tôi",
                async (s, e) =>
                {
                    try
                    {
                        var location = await Geo.GetAsync();

                        if (location == null)
                        {
                            await UI.Alert(
                                "Không lấy được vị trí",
                                "Hãy bật GPS và cấp quyền truy cập vị trí.");
                            return;
                        }

                        await Share.Default.RequestAsync(
                            new ShareTextRequest
                            {
                                Title = "Chia sẻ vị trí",
                                Text =
                                    $"📍 Vị trí của {me.FullName}: " +
                                    Geo.Link(
                                        location.Latitude,
                                        location.Longitude)
                            });
                    }
                    catch (Exception ex)
                    {
                        await UI.Alert(
                            "Lỗi chia sẻ vị trí",
                            ex.Message);
                    }
                }));

        Body.Children.Add(
            UI.Btn(
                "✏️  Sửa tên hiển thị",
                async (s, e) =>
                {
                    try
                    {
                        var newName = await DisplayPromptAsync(
                            "Sửa tên",
                            "Họ và tên mới:",
                            initialValue: me.FullName,
                            maxLength: 100);

                        if (string.IsNullOrWhiteSpace(newName))
                            return;

                        me.FullName = newName.Trim();

                        await Db.C.UpdateAsync(me);

                        await LoadAsync();
                    }
                    catch (Exception ex)
                    {
                        await UI.Alert(
                            "Lỗi cập nhật",
                            $"Không thể cập nhật tên: {ex.Message}");
                    }
                },
                UI.Gray));

        Body.Children.Add(
            UI.Btn(
                "Đăng xuất",
                (s, e) => Nav.Logout(),
                UI.Danger));

        return Task.CompletedTask;
    }
}
