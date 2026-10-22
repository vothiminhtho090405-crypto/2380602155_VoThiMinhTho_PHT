
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VoThiMinhTho_2380602155_PHT;

public class HistoryPage : StackPage
{
    public HistoryPage() : base("Lịch sử điểm danh")
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
                    UI.Sub("Vui lòng đăng nhập để xem lịch sử điểm danh."));
                return;
            }

            int uid = me.Id;

            var atts = (await Db.C
                    .Table<Attendance>()
                    .Where(a => a.StudentId == uid)
                    .ToListAsync())
                .OrderByDescending(a => a.Time)
                .ToList();

            var sessions = await Db.C
                .Table<AttSession>()
                .ToListAsync();

            var classes = await Db.C
                .Table<ClassRoom>()
                .ToListAsync();

            Body.Children.Add(
                UI.H1($"Lịch sử điểm danh ({atts.Count})"));

            if (atts.Count == 0)
            {
                Body.Children.Add(
                    UI.Sub("Bạn chưa điểm danh buổi nào."));
                return;
            }

            foreach (var attendance in atts)
            {
                var session = sessions.FirstOrDefault(
                    x => x.Id == attendance.SessionId);

                var classRoom = classes.FirstOrDefault(
                    x => x.Id == session?.ClassId);

                var className = classRoom?.Name ?? "(Lớp đã xóa)";
                var sessionTitle = session?.Title ?? "Không xác định";

                Body.Children.Add(
                    UI.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 6,

                            Children =
                            {
                                UI.Txt(className, 16, true),

                                UI.Sub(
                                    $"{sessionTitle} · " +
                                    $"{attendance.Time:dd/MM/yyyy HH:mm}"),

                                UI.Sub(
                                    $"Cách vị trí lớp {attendance.DistanceM:0} m"),

                                UI.Sub(
                                    $"Phương thức: {attendance.Method}"),

                                UI.Badge("Có mặt", UI.Success)
                            }
                        }));
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải lịch sử",
                $"Không thể tải lịch sử điểm danh: {ex.Message}");
        }
    }
}
