
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

/// <summary>
/// Hiển thị mã QR, danh sách có mặt/vắng
/// và các thao tác quản lý buổi điểm danh.
/// </summary>
public class SessionDetailPage : StackPage
{
    private readonly AttSession _session;
    private readonly ClassRoom _classRoom;

    public SessionDetailPage(AttSession session, ClassRoom classRoom)
        : base(session.Title)
    {
        _session = session;
        _classRoom = classRoom;
    }

    protected override async Task LoadAsync()
    {
        Body.Children.Clear();

        try
        {
            int sessionId = _session.Id;
            int classId = _classRoom.Id;

            var enrollments = await Db.C
                .Table<Enrollment>()
                .Where(x => x.ClassId == classId)
                .ToListAsync();

            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            var attendances = await Db.C
                .Table<Attendance>()
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();

            Body.Children.Add(UI.H1(_session.Title));

            Body.Children.Add(
                UI.Sub(
                    $"{_session.CreatedAt:dd/MM/yyyy HH:mm} · " +
                    $"Bán kính cho phép {_session.RadiusM} m"));

            Body.Children.Add(
                UI.Badge(
                    _session.IsOpen ? "Đang mở điểm danh" : "Đã đóng",
                    _session.IsOpen ? UI.Success : UI.Gray));

            // Hiển thị mã QR để sinh viên quét.
            var qr = new BarcodeGeneratorView
            {
                Format = BarcodeFormat.QrCode,
                Value = $"DD|{_session.Id}|{_session.Token}",
                WidthRequest = 240,
                HeightRequest = 240,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = Colors.White,
                ForegroundColor = Colors.Black
            };

            Body.Children.Add(
                UI.Card(
                    new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            qr,
                            UI.Sub(
                                "Sinh viên mở tab “Điểm danh” và quét mã này"),
                            UI.Txt(
                                $"Mã nhập tay: {_session.Id}-{_session.Token}",
                                16,
                                true)
                        }
                    }));

            Body.Children.Add(
                UI.Btn(
                    "📍 Chia sẻ vị trí lớp học",
                    async (s, e) =>
                    {
                        try
                        {
                            await Share.Default.RequestAsync(
                                new ShareTextRequest
                                {
                                    Title = "Vị trí lớp học",
                                    Text =
                                        $"📍 {_classRoom.Name} - {_session.Title}: " +
                                        Geo.Link(_session.Lat, _session.Lon)
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
                UI.Row(
                    UI.Btn(
                        _session.IsOpen ? "Đóng điểm danh" : "Mở lại",
                        async (s, e) =>
                        {
                            try
                            {
                                _session.IsOpen = !_session.IsOpen;

                                await Db.C.UpdateAsync(_session);
                                await LoadAsync();
                            }
                            catch (Exception ex)
                            {
                                await UI.Alert(
                                    "Lỗi cập nhật",
                                    ex.Message);
                            }
                        },
                        UI.Warn,
                        true),

                    UI.Btn(
                        "Đổi tên",
                        async (s, e) =>
                        {
                            try
                            {
                                var newTitle = await DisplayPromptAsync(
                                    "Đổi tên buổi",
                                    "Tên mới:",
                                    initialValue: _session.Title);

                                if (string.IsNullOrWhiteSpace(newTitle))
                                    return;

                                _session.Title = newTitle.Trim();
                                Title = _session.Title;

                                await Db.C.UpdateAsync(_session);
                                await LoadAsync();
                            }
                            catch (Exception ex)
                            {
                                await UI.Alert(
                                    "Lỗi đổi tên",
                                    ex.Message);
                            }
                        },
                        UI.Gray,
                        true),

                    UI.Btn(
                        "Xóa buổi",
                        async (s, e) =>
                        {
                            try
                            {
                                var confirmed = await UI.Confirm(
                                    "Bạn có chắc muốn xóa buổi điểm danh này?");

                                if (!confirmed)
                                    return;

                                await Db.DeleteSession(_session.Id);
                                await Navigation.PopAsync();
                            }
                            catch (Exception ex)
                            {
                                await UI.Alert(
                                    "Lỗi xóa buổi",
                                    ex.Message);
                            }
                        },
                        UI.Danger,
                        true)));

            Body.Children.Add(
                UI.Txt(
                    $"Điểm danh: {attendances.Count}/{enrollments.Count}",
                    17,
                    true));

            foreach (var enrollment in enrollments)
            {
                var student = users.FirstOrDefault(
                    u => u.Id == enrollment.StudentId);

                if (student == null)
                    continue;

                var attendance = attendances.FirstOrDefault(
                    a => a.StudentId == student.Id);

                var info = attendance == null
                    ? UI.Sub("✘ Vắng mặt")
                    : UI.Sub(
                        $"✔ {attendance.Time:HH:mm:ss} · " +
                        $"cách {attendance.DistanceM:0} m · " +
                        $"chuyển động {attendance.Motion:0.00} g · " +
                        $"{attendance.Method}");

                var attendanceButton = attendance == null
                    ? UI.Btn(
                        "Có mặt",
                        async (s, e) =>
                        {
                            try
                            {
                                if (attendances.Any(
                                    a => a.StudentId == student.Id))
                                {
                                    await UI.Alert(
                                        "Thông báo",
                                        "Sinh viên đã có dữ liệu điểm danh.");
                                    return;
                                }

                                await Db.C.InsertAsync(
                                    new Attendance
                                    {
                                        SessionId = sessionId,
                                        StudentId = student.Id,
                                        Time = DateTime.Now,
                                        Method = "Thủ công (giảng viên)"
                                    });

                                await LoadAsync();
                            }
                            catch (Exception ex)
                            {
                                await UI.Alert(
                                    "Lỗi điểm danh",
                                    ex.Message);
                            }
                        },
                        UI.Success,
                        true)
                    : UI.Btn(
                        "Hủy",
                        async (s, e) =>
                        {
                            try
                            {
                                var confirmed = await UI.Confirm(
                                    $"Hủy điểm danh của {student.FullName}?");

                                if (!confirmed)
                                    return;

                                await Db.C.DeleteAsync<Attendance>(
                                    attendance.Id);

                                await LoadAsync();
                            }
                            catch (Exception ex)
                            {
                                await UI.Alert(
                                    "Lỗi hủy điểm danh",
                                    ex.Message);
                            }
                        },
                        UI.Danger,
                        true);

                Body.Children.Add(
                    UI.Card(
                        new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                UI.Txt(
                                    $"{student.FullName} ({student.Code})",
                                    15,
                                    true),
                                info,
                                attendanceButton
                            }
                        }));
            }
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải buổi điểm danh",
                ex.Message);
        }
    }
}
