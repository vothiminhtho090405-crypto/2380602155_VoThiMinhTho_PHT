
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Giảng viên/Admin: quản lý buổi điểm danh
/// và danh sách sinh viên của một lớp.
/// </summary>
public class SessionsPage : StackPage
{
    private readonly ClassRoom _classRoom;

    public SessionsPage(ClassRoom classRoom)
        : base(classRoom.Name)
    {
        _classRoom = classRoom;
    }

    protected override async Task LoadAsync()
    {
        Body.Children.Clear();

        try
        {
            int classId = _classRoom.Id;

            var sessions = (await Db.C
                .Table<AttSession>()
                .Where(s => s.ClassId == classId)
                .ToListAsync())
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            var enrollments = await Db.C
                .Table<Enrollment>()
                .Where(x => x.ClassId == classId)
                .ToListAsync();

            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            var attendances = await Db.C
                .Table<Attendance>()
                .ToListAsync();

            Body.Children.Add(UI.H1(_classRoom.Name));

            Body.Children.Add(
                UI.Sub(
                    $"Mã lớp: {_classRoom.Code} · " +
                    $"{enrollments.Count} sinh viên"));

            Body.Children.Add(
                UI.Btn(
                    "＋ Tạo buổi điểm danh (QR + vị trí)",
                    async (s, e) => await NewSession()));

            Body.Children.Add(
                UI.Btn(
                    "＋ Thêm sinh viên vào lớp",
                    async (s, e) => await AddStudent(),
                    UI.Gray));

            Body.Children.Add(
                UI.Txt("Các buổi điểm danh", 17, true));

            if (sessions.Count == 0)
            {
                Body.Children.Add(
                    UI.Sub("Chưa có buổi nào."));
            }

            foreach (var session in sessions)
            {
                int presentCount = attendances.Count(
                    a => a.SessionId == session.Id);

                var tap = new TapGestureRecognizer();

                tap.Tapped += async (s, e) =>
                {
                    await Navigation.PushAsync(
                        new SessionDetailPage(session, _classRoom));
                };

                var card = UI.Card(
                    new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            UI.Txt(session.Title, 16, true),

                            UI.Sub(
                                $"{session.CreatedAt:dd/MM/yyyy HH:mm} · " +
                                $"Có mặt {presentCount}/{enrollments.Count}"),

                            UI.Badge(
                                session.IsOpen ? "Đang mở" : "Đã đóng",
                                session.IsOpen ? UI.Success : UI.Gray)
                        }
                    });

                card.GestureRecognizers.Add(tap);
                Body.Children.Add(card);
            }

            Body.Children.Add(
                UI.Txt("Danh sách sinh viên", 17, true));

            foreach (var enrollment in enrollments)
            {
                var student = users.FirstOrDefault(
                    u => u.Id == enrollment.StudentId);

                if (student == null)
                    continue;

                var studentCard = UI.Card(
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new VerticalStackLayout
                            {
                                WidthRequest = 220,
                                Children =
                                {
                                    UI.Txt(student.FullName, 15, true),

                                    UI.Sub(
                                        $"{student.Code} · @{student.Username}")
                                }
                            },

                            UI.Btn(
                                "Xóa",
                                async (s, e) =>
                                {
                                    try
                                    {
                                        var confirmed = await UI.Confirm(
                                            $"Xóa {student.FullName} khỏi lớp?");

                                        if (!confirmed)
                                            return;

                                        await Db.C.DeleteAsync<Enrollment>(
                                            enrollment.Id);

                                        await LoadAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        await UI.Alert(
                                            "Lỗi xóa sinh viên",
                                            ex.Message);
                                    }
                                },
                                UI.Danger,
                                true)
                        }
                    });

                Body.Children.Add(studentCard);
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải danh sách lớp",
                ex.Message);
        }
    }

    private async Task NewSession()
    {
        try
        {
            int classId = _classRoom.Id;

            int sessionCount = await Db.C
                .Table<AttSession>()
                .Where(x => x.ClassId == classId)
                .CountAsync();

            var title = await DisplayPromptAsync(
                "Buổi điểm danh mới",
                "Tên buổi:",
                initialValue: $"Buổi {sessionCount + 1}");

            if (string.IsNullOrWhiteSpace(title))
                return;

            var location = await Geo.GetAsync();

            if (location == null)
            {
                await UI.Alert(
                    "Chưa có vị trí",
                    "Hãy bật GPS và cấp quyền vị trí để tạo buổi điểm danh.");

                return;
            }

            var session = new AttSession
            {
                ClassId = classId,
                Title = title.Trim(),
                CreatedAt = DateTime.Now,
                Token = Guid.NewGuid().ToString("N")[..8],
                Lat = location.Latitude,
                Lon = location.Longitude,
                RadiusM = 100,
                IsOpen = true
            };

            await Db.C.InsertAsync(session);

            await Navigation.PushAsync(
                new SessionDetailPage(session, _classRoom));
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tạo buổi điểm danh",
                ex.Message);
        }
    }

    private async Task AddStudent()
    {
        try
        {
            var username = (
                await DisplayPromptAsync(
                    "Thêm sinh viên",
                    "Nhập tên đăng nhập của sinh viên:")
                )?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(username))
                return;

            var student = await Db.C
                .Table<User>()
                .Where(u =>
                    u.Username == username &&
                    u.Role == Role.SinhVien)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                await UI.Alert(
                    "Không tìm thấy",
                    "Không có sinh viên nào với tên đăng nhập này.");

                return;
            }

            int classId = _classRoom.Id;
            int studentId = student.Id;

            int existingCount = await Db.C
                .Table<Enrollment>()
                .Where(x =>
                    x.ClassId == classId &&
                    x.StudentId == studentId)
                .CountAsync();

            if (existingCount > 0)
            {
                await UI.Alert(
                    "Đã có",
                    "Sinh viên đã ở trong lớp.");

                return;
            }

            await Db.C.InsertAsync(
                new Enrollment
                {
                    ClassId = classId,
                    StudentId = studentId
                });

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi thêm sinh viên",
                ex.Message);
        }
    }
}
