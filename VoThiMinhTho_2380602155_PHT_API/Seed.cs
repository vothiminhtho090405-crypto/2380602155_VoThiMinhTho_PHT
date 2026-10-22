using System.Security.Cryptography;
using System.Text;

namespace VoThiMinhTho_2380602155_PHT_API;

public static class Seed
{
    // Phải giống hệt cách băm trong ứng dụng (Db.Hash)
    public static string Hash(string pw, string salt) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(salt + pw)));

    static User Make(string un, string email, string name, string code, string pw, string role)
    {
        var salt = Guid.NewGuid().ToString("N");
        return new User
        {
            Username = un, Email = email, FullName = name, Code = code, Role = role,
            Salt = salt, PasswordHash = Hash(pw, salt)
        };
    }

    public static void Run(AppDb db)
    {
        if (db.Users.Any()) return;

        var admin = Make("admin", "admin@demo.com", "Quản trị viên", "AD001", "Admin@123", "Admin");
        var gv = Make("gv1", "gv1@demo.com", "Nguyễn Văn Giảng", "GV001", "Gv@12345", "GiangVien");
        var sv1 = Make("sv1", "sv1@demo.com", "Trần Thị Sinh", "SV001", "Sv@12345", "SinhVien");
        var sv2 = Make("sv2", "sv2@demo.com", "Lê Văn Học", "SV002", "Sv@12345", "SinhVien");
        db.Users.AddRange(admin, gv, sv1, sv2);
        db.SaveChanges();

        var cls = new ClassRoom { Name = "Lập trình thiết bị di động", Code = "LTDD01", Room = "A101", TeacherId = gv.Id };
        db.Classes.Add(cls);
        db.SaveChanges();

        db.Enrollments.AddRange(
            new Enrollment { ClassId = cls.Id, StudentId = sv1.Id },
            new Enrollment { ClassId = cls.Id, StudentId = sv2.Id });
        db.SaveChanges();
    }
}
