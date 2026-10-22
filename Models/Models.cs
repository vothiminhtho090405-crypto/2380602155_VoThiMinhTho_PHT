
using SQLite;

namespace VoThiMinhTho_2380602155_PHT.Models;

public static class Role
{
    public const string Admin = "Admin";
    public const string GiangVien = "GiangVien";
    public const string SinhVien = "SinhVien";

    public static string Ten(string role) => role switch
    {
        Admin => "Quản trị viên",
        GiangVien => "Giảng viên",
        SinhVien => "Sinh viên",
        _ => "Không xác định"
    };
}

public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Username { get; set; } = "";

    [Unique]
    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";
    public string Code { get; set; } = "";
    public string Role { get; set; } = global::VoThiMinhTho_2380602155_PHT.Models.Role.SinhVien;

    // Không lưu mật khẩu gốc
    public string Salt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class ClassRoom
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    [Unique]
    public string Code { get; set; } = "";

    public string Room { get; set; } = "";
    public int TeacherId { get; set; }
}

public class Enrollment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ClassId { get; set; }
    public int StudentId { get; set; }
}

public class AttSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ClassId { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Token { get; set; } = "";

    public double Lat { get; set; }
    public double Lon { get; set; }
    public int RadiusM { get; set; } = 100;
    public bool IsOpen { get; set; } = true;
}

public class Attendance
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public DateTime Time { get; set; }

    public double Lat { get; set; }
    public double Lon { get; set; }
    public double DistanceM { get; set; }
    public double Motion { get; set; }

    public string Method { get; set; } = "";
}
