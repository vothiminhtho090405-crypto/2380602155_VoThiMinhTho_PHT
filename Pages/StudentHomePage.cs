
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Sinh viên: xem lớp đã tham gia,
/// tham gia lớp bằng mã và rời lớp.
/// </summary>
public class StudentHomePage : StackPage
{
    public StudentHomePage()
        : base("Lớp học của tôi")
    {
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
                    UI.Sub("Vui lòng đăng nhập để xem lớp học của bạn."));
                return;
            }

            int userId = me.Id;

            var myEnrollments = await Db.C
                .Table<Enrollment>()
                .Where(x => x.StudentId == userId)
                .ToListAsync();

            var classes = await Db.C
                .Table<ClassRoom>()
                .ToListAsync();

            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            var sessions = await Db.C
                .Table<AttSession>()
                .ToListAsync();

            var attendances = await Db.C
                .Table<Attendance>()
                .Where(a => a.StudentId == userId)
                .ToListAsync();

            Body.Children.Add(
                UI.H1($"Xin chào, {me.FullName} 👋"));

            Body.Children.Add(
                UI.Sub(
                    "Chọn tab “Điểm danh” bên dưới để quét mã QR của giảng viên."));

            Body.Children.Add(
                UI.Btn(
                    "＋ Tham gia lớp bằng mã",
                    async (s, e) => await Join()));

            if (myEnrollments.Count == 0)
            {
                Body.Children.Add(
                    UI.Sub("Bạn chưa tham gia lớp nào."));
            }

            foreach (var enrollment in myEnrollments)
            {
                var classroom = classes.FirstOrDefault(
                    x => x.Id == enrollment.ClassId);

                if (classroom == null)
                    continue;

                var sessionIds = sessions
                    .Where(s => s.ClassId == classroom.Id)
                    .Select(s => s.Id)
                    .ToHashSet();

                int attendedCount = attendances.Count(
                    a => sessionIds.Contains(a.SessionId));

                var teacherName = users.FirstOrDefault(
                    u => u.Id == classroom.TeacherId)?.FullName ?? "—";

                Body.Children.Add(
                    UI.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 5,
                            Children =
                            {
                                UI.Txt(classroom.Name, 17, true),

                                UI.Sub(
                                    $"Giảng viên: {teacherName} · " +
                                    $"Phòng {classroom.Room}"),

                                UI.Badge(
                                    $"Đã điểm danh {attendedCount}/{sessionIds.Count} buổi",
                                    attendedCount == sessionIds.Count &&
                                    sessionIds.Count > 0
                                        ? UI.Success
                                        : UI.Primary),

                                UI.Btn(
                                    "Rời lớp",
                                    async (s, e) =>
                                    {
                                        try
                                        {
                                            var confirmed = await UI.Confirm(
                                                $"Rời khỏi lớp “{classroom.Name}”?");

                                            if (!confirmed)
                                                return;

                                            await Db.C.DeleteAsync<Enrollment>(
                                                enrollment.Id);

                                            await LoadAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            await UI.Alert(
                                                "Lỗi rời lớp",
                                                ex.Message);
                                        }
                                    },
                                    UI.Danger,
                                    true)
                            }
                        }));
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải lớp học",
                ex.Message);
        }
    }

    private async Task Join()
    {
        try
        {
            var code = (
                await DisplayPromptAsync(
                    "Tham gia lớp",
                    "Nhập mã lớp do giảng viên cung cấp:")
                )?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(code))
                return;

            var classroom = await Db.C
                .Table<ClassRoom>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (classroom == null)
            {
                await UI.Alert(
                    "Không tìm thấy",
                    "Mã lớp không đúng.");

                return;
            }

            var me = AppState.Me;

            if (me == null)
            {
                await UI.Alert(
                    "Chưa đăng nhập",
                    "Vui lòng đăng nhập lại.");

                return;
            }

            int classId = classroom.Id;
            int userId = me.Id;

            int existingCount = await Db.C
                .Table<Enrollment>()
                .Where(x =>
                    x.ClassId == classId &&
                    x.StudentId == userId)
                .CountAsync();

            if (existingCount > 0)
            {
                await UI.Alert(
                    "Đã tham gia",
                    "Bạn đã ở trong lớp này rồi.");

                return;
            }

            await Db.C.InsertAsync(
                new Enrollment
                {
                    ClassId = classId,
                    StudentId = userId
                });

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tham gia lớp",
                ex.Message);
        }
    }
}
