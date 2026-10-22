
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

public class ClassEditPage : StackPage
{
    private readonly ClassRoom? _classRoom;

    private readonly Entry _name = UI.Field("Tên lớp học phần");
    private readonly Entry _room = UI.Field("Phòng học");
    private readonly Entry _code = UI.Field("Mã lớp (để sinh viên tham gia)");

    private readonly Picker _teacher = new()
    {
        Title = "Chọn giảng viên phụ trách"
    };

    private List<User> _teachers = new();
    private bool _built;

    public ClassEditPage(ClassRoom? classRoom)
        : base(classRoom == null ? "Thêm lớp" : "Sửa lớp")
    {
        _classRoom = classRoom;

        if (classRoom != null)
        {
            _name.Text = classRoom.Name;
            _room.Text = classRoom.Room;
            _code.Text = classRoom.Code;
        }
        else
        {
            _code.Text = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();
        }
    }

    protected override async Task LoadAsync()
    {
        if (_built)
            return;

        _built = true;

        var currentUser = AppState.Me;

        if (currentUser == null)
        {
            await UI.Alert("Lỗi", "Bạn chưa đăng nhập.");
            await Navigation.PopAsync();
            return;
        }

        var layout = new VerticalStackLayout
        {
            Spacing = 12,
            Children = { _name, _room, _code }
        };

        if (currentUser.Role == Role.Admin)
        {
            _teachers = await Db.C.Table<User>()
                .Where(user => user.Role == Role.GiangVien)
                .ToListAsync();

            _teacher.ItemsSource = _teachers
                .Select(teacher =>
                    $"{teacher.FullName} ({teacher.Username})")
                .ToList();

            if (_classRoom != null)
            {
                var index = _teachers.FindIndex(
                    teacher => teacher.Id == _classRoom.TeacherId);

                _teacher.SelectedIndex = index;
            }
            else
            {
                _teacher.SelectedIndex =
                    _teachers.Count > 0 ? 0 : -1;
            }

            layout.Children.Add(_teacher);
        }

        layout.Children.Add(UI.Btn("Lưu lớp học", OnSave));

        Body.Children.Add(UI.Card(layout));
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        try
        {
            var name = (_name.Text ?? "").Trim();
            var room = (_room.Text ?? "").Trim();
            var code = (_code.Text ?? "")
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(code))
            {
                await UI.Alert(
                    "Thiếu thông tin",
                    "Vui lòng nhập tên lớp và mã lớp.");
                return;
            }

            var currentUser = AppState.Me;

            if (currentUser == null)
            {
                await UI.Alert(
                    "Lỗi",
                    "Phiên đăng nhập không hợp lệ.");
                return;
            }

            var currentId = _classRoom?.Id ?? 0;

            var duplicateCount = await Db.C.Table<ClassRoom>()
                .Where(item =>
                    item.Code == code && item.Id != currentId)
                .CountAsync();

            if (duplicateCount > 0)
            {
                await UI.Alert(
                    "Mã lớp đã tồn tại",
                    "Vui lòng nhập mã lớp khác.");
                return;
            }

            var teacherId = currentUser.Id;

            if (currentUser.Role == Role.Admin)
            {
                if (_teacher.SelectedIndex < 0 ||
                    _teacher.SelectedIndex >= _teachers.Count)
                {
                    await UI.Alert(
                        "Chưa chọn giảng viên",
                        "Hãy tạo tài khoản giảng viên và chọn giảng viên phụ trách.");
                    return;
                }

                teacherId = _teachers[_teacher.SelectedIndex].Id;
            }

            if (_classRoom == null)
            {
                var newClass = new ClassRoom
                {
                    Name = name,
                    Room = room,
                    Code = code,
                    TeacherId = teacherId
                };

                await Db.C.InsertAsync(newClass);
            }
            else
            {
                _classRoom.Name = name;
                _classRoom.Room = room;
                _classRoom.Code = code;
                _classRoom.TeacherId = teacherId;

                await Db.C.UpdateAsync(_classRoom);
            }

            await UI.Alert(
                "Thành công",
                _classRoom == null
                    ? "Đã thêm lớp học."
                    : "Đã cập nhật lớp học.");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi",
                $"Không thể lưu lớp học: {ex.Message}");
        }
    }
}
