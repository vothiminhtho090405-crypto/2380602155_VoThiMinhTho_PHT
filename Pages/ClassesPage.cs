
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Admin: xem tất cả lớp.
/// Giảng viên: chỉ xem lớp do mình phụ trách.
/// Hỗ trợ mở, thêm, sửa và xóa lớp học.
/// </summary>
public class ClassesPage : StackPage
{
    public ClassesPage() : base("Lớp học")
    {
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "+ Thêm lớp",
            Command = new Command(async () =>
            {
                await Navigation.PushAsync(
                    new ClassEditPage(null));
            })
        });
    }

    protected override async Task LoadAsync()
    {
        Body.Children.Clear();

        try
        {
            var me = AppState.Me;

            if (me == null)
            {
                Body.Children.Add(
                    UI.Sub("Vui lòng đăng nhập để xem danh sách lớp."));
                return;
            }

            var allClasses = await Db.C
                .Table<ClassRoom>()
                .ToListAsync();

            var classes = me.Role == Role.Admin
                ? allClasses
                : allClasses
                    .Where(c => c.TeacherId == me.Id)
                    .ToList();

            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            var enrollments = await Db.C
                .Table<Enrollment>()
                .ToListAsync();

            Body.Children.Add(
                UI.H1(
                    me.Role == Role.Admin
                        ? $"Tất cả lớp học ({classes.Count})"
                        : $"Lớp học của tôi ({classes.Count})"));

            if (classes.Count == 0)
            {
                Body.Children.Add(
                    UI.Sub(
                        "Chưa có lớp học nào. " +
                        "Nhấn “+ Thêm lớp” để tạo lớp mới."));
                return;
            }

            foreach (var classRoom in classes)
            {
                var teacher = users.FirstOrDefault(
                    u => u.Id == classRoom.TeacherId);

                var teacherName = teacher?.FullName ?? "Chưa xác định";

                var studentCount = enrollments.Count(
                    e => e.ClassId == classRoom.Id);

                var classCard = UI.Card(
                    new VerticalStackLayout
                    {
                        Spacing = 8,

                        Children =
                        {
                            UI.Txt(classRoom.Name, 17, true),

                            UI.Sub(
                                $"Mã lớp: {classRoom.Code}"),

                            UI.Sub(
                                $"Phòng học: {classRoom.Room}"),

                            UI.Sub(
                                $"Giảng viên: {teacherName}"),

                            UI.Sub(
                                $"Số sinh viên: {studentCount}"),

                            UI.Row(
                                UI.Btn(
                                    "Mở",
                                    async (s, e) =>
                                    {
                                        await Navigation.PushAsync(
                                            new SessionsPage(classRoom));
                                    },
                                    UI.Primary,
                                    true),

                                UI.Btn(
                                    "Sửa",
                                    async (s, e) =>
                                    {
                                        await Navigation.PushAsync(
                                            new ClassEditPage(classRoom));
                                    },
                                    UI.Gray,
                                    true),

                                UI.Btn(
                                    "Xóa",
                                    async (s, e) =>
                                    {
                                        var confirmed = await UI.Confirm(
                                            $"Bạn có chắc muốn xóa lớp " +
                                            $"“{classRoom.Name}” và dữ liệu " +
                                            "điểm danh liên quan không?");

                                        if (!confirmed)
                                            return;

                                        await Db.DeleteClass(classRoom.Id);

                                        await LoadAsync();
                                    },
                                    UI.Danger,
                                    true))
                        }
                    });

                Body.Children.Add(classCard);
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải dữ liệu",
                $"Không thể tải danh sách lớp: {ex.Message}");
        }
    }
}
