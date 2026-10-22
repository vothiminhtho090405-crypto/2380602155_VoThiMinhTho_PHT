namespace VoThiMinhTho_2380602155_PHT_API;

public interface IEntity { int Id { get; set; } }

public class User : IEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Code { get; set; } = "";
    public string Role { get; set; } = "SinhVien";
    public string Salt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class ClassRoom : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Room { get; set; } = "";
    public int TeacherId { get; set; }
}

public class Enrollment : IEntity
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public int StudentId { get; set; }
}

public class AttSession : IEntity
{
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

public class Attendance : IEntity
{
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

public static class UserExt
{
    /// <summary>Không bao giờ trả mật khẩu băm về cho ứng dụng.</summary>
    public static User WithoutSecrets(this User u) => new()
    {
        Id = u.Id, Username = u.Username, Email = u.Email, FullName = u.FullName,
        Code = u.Code, Role = u.Role, Salt = "", PasswordHash = ""
    };
}
