
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

public class DashboardPage : StackPage
{
    public DashboardPage() : base("Tổng quan")
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
                    UI.Sub("Vui lòng đăng nhập để xem trang tổng quan."));
                return;
            }

            var users = await Db.C
                .Table<User>()
                .ToListAsync();

            var classes = await Db.C
                .Table<ClassRoom>()
                .ToListAsync();

            var sessions = await Db.C
                .Table<AttSession>()
                .ToListAsync();

            var atts = await Db.C
                .Table<Attendance>()
                .ToListAsync();

            var enrollments = await Db.C
                .Table<Enrollment>()
                .ToListAsync();

            Body.Children.Add(
                UI.H1($"Xin chào, {me.FullName} 👋"));

            Body.Children.Add(
                UI.Badge(Role.Ten(me.Role), UI.Primary));

            List<(string, string, Color)> stats;

            if (me.Role == Role.Admin)
            {
                stats = new()
                {
                    ("Người dùng", users.Count.ToString(), UI.Primary),
                    ("Lớp học", classes.Count.ToString(), UI.Success),
                    ("Buổi điểm danh", sessions.Count.ToString(), UI.Warn),
                    ("Lượt có mặt", atts.Count.ToString(), UI.Danger)
                };

                Body.Children.Add(
                    UI.Sub("Trang quản trị hệ thống"));
            }
            else if (me.Role == Role.GiangVien)
            {
                var myClasses = classes
                    .Where(c => c.TeacherId == me.Id)
                    .Select(c => c.Id)
                    .ToHashSet();

                var mySessions = sessions
                    .Where(s => myClasses.Contains(s.ClassId))
                    .ToList();

                var mySessionIds = mySessions
                    .Select(s => s.Id)
                    .ToHashSet();

                var studentIds = enrollments
                    .Where(e => myClasses.Contains(e.ClassId))
                    .Select(e => e.StudentId)
                    .Distinct()
                    .Count();

                stats = new()
                {
                    ("Lớp của tôi", myClasses.Count.ToString(), UI.Primary),
                    ("Sinh viên", studentIds.ToString(), UI.Success),
                    ("Buổi đã tạo", mySessions.Count.ToString(), UI.Warn),
                    (
                        "Lượt có mặt",
                        atts.Count(a => mySessionIds.Contains(a.SessionId))
                            .ToString(),
                        UI.Danger
                    )
                };

                Body.Children.Add(
                    UI.Sub("Tổng quan dành cho giảng viên"));
            }
            else if (me.Role == Role.SinhVien)
            {
                var myEnrollments = enrollments
                    .Where(e => e.StudentId == me.Id)
                    .ToList();

                var myClassIds = myEnrollments
                    .Select(e => e.ClassId)
                    .ToHashSet();

                var myClasses = classes
                    .Where(c => myClassIds.Contains(c.Id))
                    .ToList();

                var mySessions = sessions
                    .Where(s => myClassIds.Contains(s.ClassId))
                    .ToList();

                var mySessionIds = mySessions
                    .Select(s => s.Id)
                    .ToHashSet();

                var myAttendances = atts
                    .Where(a => a.StudentId == me.Id)
                    .ToList();

                stats = new()
                {
                    ("Lớp đã tham gia", myClasses.Count.ToString(), UI.Primary),
                    ("Buổi học", mySessions.Count.ToString(), UI.Success),
                    ("Đã điểm danh", myAttendances.Count.ToString(), UI.Warn),
                    (
                        "Chưa điểm danh",
                        mySessions.Count(s =>
                            !myAttendances.Any(a => a.SessionId == s.Id))
                            .ToString(),
                        UI.Danger
                    )
                };

                Body.Children.Add(
                    UI.Sub("Tổng quan dành cho sinh viên"));
            }
            else
            {
                Body.Children.Add(
                    UI.Sub("Vai trò tài khoản không hợp lệ."));
                return;
            }

            var grid = new Grid
            {
                ColumnSpacing = 12,
                RowSpacing = 12
            };

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            for (int i = 0; i < stats.Count; i++)
            {
                if (i % 2 == 0)
                {
                    grid.RowDefinitions.Add(
                        new RowDefinition(GridLength.Auto));
                }

                grid.Add(
                    UI.Stat(
                        stats[i].Item1,
                        stats[i].Item2,
                        stats[i].Item3),
                    i % 2,
                    i / 2);
            }

            Body.Children.Add(grid);
        }
        catch (Exception ex)
        {
            await UI.Alert(
                "Lỗi tải tổng quan",
                $"Không thể tải dữ liệu: {ex.Message}");
        }
    }
}
