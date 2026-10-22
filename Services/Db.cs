
using System.Security.Cryptography;
using System.Text;
using SQLite;
using VoThiMinhTho_2380602155_PHT.Models;


namespace VoThiMinhTho_2380602155_PHT;

public static class Db
{
    static SQLiteAsyncConnection? _c;
    static Task? _init;

    public static SQLiteAsyncConnection C => _c!;

    public static Task InitAsync() => _init ??= Core();

    static async Task Core()
    {
        _c = new SQLiteAsyncConnection(
            Path.Combine(FileSystem.AppDataDirectory, "diemdanh.db3"));

        await _c.CreateTableAsync<User>();
        await _c.CreateTableAsync<ClassRoom>();
        await _c.CreateTableAsync<Enrollment>();
        await _c.CreateTableAsync<AttSession>();
        await _c.CreateTableAsync<Attendance>();

        if (await _c.Table<User>().CountAsync() == 0)
        {
            var admin = Make(
                "admin", "admin@demo.com", "Quản trị viên",
                "AD001", "Admin@123", Role.Admin);

            var gv = Make(
                "gv1", "gv1@demo.com", "Nguyễn Văn Giảng",
                "GV001", "Gv@12345", Role.GiangVien);

            var sv1 = Make(
                "sv1", "sv1@demo.com", "Trần Thị Sinh",
                "SV001", "Sv@12345", Role.SinhVien);

            var sv2 = Make(
                "sv2", "sv2@demo.com", "Lê Văn Học",
                "SV002", "Sv@12345", Role.SinhVien);

            foreach (var u in new[] { admin, gv, sv1, sv2 })
                await _c.InsertAsync(u);

            var cls = new ClassRoom
            {
                Name = "Lập trình thiết bị di động",
                Code = "LTDD01",
                Room = "A101",
                TeacherId = gv.Id
            };

            await _c.InsertAsync(cls);

            await _c.InsertAsync(new Enrollment
            {
                ClassId = cls.Id,
                StudentId = sv1.Id
            });

            await _c.InsertAsync(new Enrollment
            {
                ClassId = cls.Id,
                StudentId = sv2.Id
            });
        }
    }

    public static string Hash(string pw, string salt) =>
        Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(salt + pw)));

    public static User Make(
        string username,
        string email,
        string name,
        string code,
        string pw,
        string role)
    {
        var salt = Guid.NewGuid().ToString("N");

        return new User
        {
            Username = username.Trim().ToLower(),
            Email = email.Trim().ToLower(),
            FullName = name.Trim(),
            Code = code.Trim(),
            Role = role,
            Salt = salt,
            PasswordHash = Hash(pw, salt)
        };
    }

    public static async Task<User?> Login(string username, string pw)
    {
        var un = username.Trim().ToLower();

        var u = await C.Table<User>()
            .Where(x => x.Username == un)
            .FirstOrDefaultAsync();

        if (u == null || u.PasswordHash != Hash(pw, u.Salt))
            return null;

        return u;
    }

    /// <summary>
    /// Kiểm tra dữ liệu đăng ký.
    /// Trả về (user chưa lưu, lỗi).
    /// </summary>
    public static async Task<(User? user, string? error)> Prepare(
        string name,
        string email,
        string username,
        string code,
        string pw,
        string confirm,
        string role)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(pw))
        {
            return (null, "Vui lòng nhập đầy đủ thông tin.");
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(email.Trim());
        }
        catch
        {
            return (null, "Email không hợp lệ.");
        }

        if (username.Trim().Contains(' '))
            return (null, "Tên đăng nhập không được chứa khoảng trắng.");

        if (pw.Length < 6)
            return (null, "Mật khẩu tối thiểu 6 ký tự.");

        if (pw != confirm)
            return (null, "Mật khẩu nhập lại không khớp.");

        var un = username.Trim().ToLower();
        var em = email.Trim().ToLower();

        if (await C.Table<User>()
                .Where(x => x.Username == un)
                .CountAsync() > 0)
        {
            return (null, "Tên đăng nhập đã tồn tại.");
        }

        if (await C.Table<User>()
                .Where(x => x.Email == em)
                .CountAsync() > 0)
        {
            return (null, "Email đã được đăng ký.");
        }

        return (Make(username, email, name, code ?? "", pw, role), null);
    }

    public static async Task DeleteClass(int classId)
    {
        var sessions = await C.Table<AttSession>()
            .Where(s => s.ClassId == classId)
            .ToListAsync();

        foreach (var session in sessions)
            await DeleteSession(session.Id);

        await C.ExecuteAsync(
            "DELETE FROM Enrollment WHERE ClassId = ?", classId);

        await C.DeleteAsync<ClassRoom>(classId);
    }

    public static async Task DeleteSession(int sessionId)
    {
        await C.ExecuteAsync(
            "DELETE FROM Attendance WHERE SessionId = ?", sessionId);

        await C.DeleteAsync<AttSession>(sessionId);
    }

    public static async Task DeleteUser(int userId)
    {
        await C.ExecuteAsync(
            "DELETE FROM Attendance WHERE StudentId = ?", userId);

        await C.ExecuteAsync(
            "DELETE FROM Enrollment WHERE StudentId = ?", userId);

        await C.DeleteAsync<User>(userId);
    }
}
