
using Microsoft.Maui.Controls;
using AndroidPlat = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Phân quyền: mỗi vai trò hiển thị bộ tab tương ứng.
/// </summary>
public class MainTabs : Microsoft.Maui.Controls.TabbedPage
{
    public MainTabs(User user)
    {
        On<AndroidPlat>().SetToolbarPlacement(ToolbarPlacement.Bottom);

        SelectedTabColor = UI.Primary;
        UnselectedTabColor = UI.Muted;
        BarBackgroundColor = Colors.White;

        void Add(Page page, string title)
        {
            Children.Add(Nav.Wrap(page, title));
        }

        switch (user.Role)
        {
            case Role.Admin:
                // Trang quản trị
                Add(new DashboardPage(), "Tổng quan");
                Add(new UsersPage(), "Người dùng");
                Add(new ClassesPage(), "Lớp học");
                break;

            case Role.GiangVien:
                // Trang giảng viên
                Add(new DashboardPage(), "Tổng quan");
                Add(new ClassesPage(), "Lớp của tôi");
                break;

            case Role.SinhVien:
                // Trang sinh viên
                Add(new StudentHomePage(), "Lớp học");
                Add(new CheckInPage(), "Điểm danh");
                Add(new HistoryPage(), "Lịch sử");
                break;

            default:
                // Vai trò không xác định: chỉ hiển thị tài khoản
                Add(new ProfilePage(), "Tài khoản");
                return;
        }

        Add(new ProfilePage(), "Tài khoản");
    }
}
