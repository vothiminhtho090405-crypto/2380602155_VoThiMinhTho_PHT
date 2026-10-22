using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using VoThiMinhTho_2380602155_PHT_API;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
var app = builder.Build();

// Tạo database (nếu chưa có) + dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    Seed.Run(db);
}

app.MapGet("/", () => "API Điểm danh - VoThiMinhTho_2380602155_PHT_API đang chạy.");

// ===================== ĐĂNG NHẬP + OTP =====================
app.MapPost("/api/auth/login", async (LoginReq r, AppDb db) =>
{
    var un = (r.Username ?? "").Trim().ToLower();
    var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == un);
    if (u == null || u.PasswordHash != Seed.Hash(r.Password ?? "", u.Salt)) return Results.Unauthorized();
    return Results.Ok(u.WithoutSecrets());
});

var otps = new ConcurrentDictionary<string, OtpEntry>();

app.MapPost("/api/auth/send-otp", async (OtpReq r, IConfiguration cfg) =>
{
    var email = (r.Email ?? "").Trim().ToLower();
    var code = Random.Shared.Next(100000, 1000000).ToString();
    otps[email] = new OtpEntry { Code = code, Exp = DateTime.Now.AddMinutes(5) };

    var smtpUser = cfg["Smtp:User"];
    if (string.IsNullOrWhiteSpace(smtpUser))
        return Results.Ok(new SendRes(false, code));          // chế độ DEMO: trả mã cho app hiển thị
    try
    {
        using var smtp = new SmtpClient(cfg["Smtp:Host"], int.Parse(cfg["Smtp:Port"] ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, cfg["Smtp:Pass"])
        };
        await smtp.SendMailAsync(new MailMessage(smtpUser, email, "Mã OTP - Điểm danh sinh viên",
            $"Mã OTP của bạn là: {code}\nMã có hiệu lực trong 5 phút. Không chia sẻ mã này cho ai."));
        return Results.Ok(new SendRes(true, null));
    }
    catch
    {
        return Results.Ok(new SendRes(false, code));
    }
});

app.MapPost("/api/auth/verify-otp", (VerifyReq r) =>
{
    var email = (r.Email ?? "").Trim().ToLower();
    if (!otps.TryGetValue(email, out var o)) return Results.Ok(new VerifyRes(false, "Chưa gửi mã OTP. Hãy gửi lại mã."));
    if (DateTime.Now > o.Exp) return Results.Ok(new VerifyRes(false, "Mã OTP đã hết hạn. Hãy gửi lại mã."));
    if (o.Fail >= 5) return Results.Ok(new VerifyRes(false, "Nhập sai quá nhiều lần. Hãy gửi lại mã."));
    if ((r.Code ?? "").Trim() != o.Code) { o.Fail++; return Results.Ok(new VerifyRes(false, "Mã OTP không đúng.")); }
    otps.TryRemove(email, out _);
    return Results.Ok(new VerifyRes(true, ""));
});

// ===================== CRUD NGƯỜI DÙNG =====================
var ug = app.MapGroup("/api/users");
ug.MapGet("", async (AppDb db) => (await db.Users.AsNoTracking().ToListAsync()).Select(u => u.WithoutSecrets()));
ug.MapGet("/{id:int}", async (int id, AppDb db) =>
    await db.Users.FindAsync(id) is { } u ? Results.Ok(u.WithoutSecrets()) : Results.NotFound());
ug.MapPost("", async (User u, AppDb db) =>
{
    if (string.IsNullOrEmpty(u.PasswordHash)) return Results.BadRequest("Thiếu mật khẩu.");
    u.Id = 0;
    u.Username = u.Username.Trim().ToLower();
    u.Email = u.Email.Trim().ToLower();
    if (await db.Users.AnyAsync(x => x.Username == u.Username || x.Email == u.Email))
        return Results.Conflict("Tên đăng nhập hoặc email đã tồn tại.");
    db.Users.Add(u);
    await db.SaveChangesAsync();
    return Results.Ok(u.WithoutSecrets());
});
ug.MapPut("/{id:int}", async (int id, User u, AppDb db) =>
{
    var cur = await db.Users.FindAsync(id);
    if (cur == null) return Results.NotFound();
    cur.Username = u.Username.Trim().ToLower();
    cur.Email = u.Email.Trim().ToLower();
    cur.FullName = u.FullName;
    cur.Code = u.Code;
    cur.Role = u.Role;
    if (!string.IsNullOrEmpty(u.PasswordHash)) { cur.Salt = u.Salt; cur.PasswordHash = u.PasswordHash; }
    await db.SaveChangesAsync();
    return Results.Ok();
});
ug.MapDelete("/{id:int}", async (int id, AppDb db) =>
{
    var cur = await db.Users.FindAsync(id);
    if (cur == null) return Results.NotFound();
    await db.Attendances.Where(a => a.StudentId == id).ExecuteDeleteAsync();
    await db.Enrollments.Where(e => e.StudentId == id).ExecuteDeleteAsync();
    db.Users.Remove(cur);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// ===================== CRUD CÁC BẢNG CÒN LẠI =====================
void Crud<T>(string route, Func<AppDb, DbSet<T>> set, Func<AppDb, int, Task>? cascade = null) where T : class, IEntity
{
    var g = app.MapGroup("/api/" + route);

    g.MapGet("", async (AppDb db) => await set(db).AsNoTracking().ToListAsync());

    g.MapGet("/{id:int}", async (int id, AppDb db) =>
    {
        var e = await set(db).FindAsync(id);
        return e == null ? Results.NotFound() : Results.Ok(e);
    });

    g.MapPost("", async (T e, AppDb db) =>
    {
        e.Id = 0;
        set(db).Add(e);
        await db.SaveChangesAsync();
        return Results.Ok(e);
    });

    g.MapPut("/{id:int}", async (int id, T e, AppDb db) =>
    {
        var cur = await set(db).FindAsync(id);
        if (cur == null) return Results.NotFound();
        e.Id = id;
        db.Entry(cur).CurrentValues.SetValues(e);
        await db.SaveChangesAsync();
        return Results.Ok();
    });

    g.MapDelete("/{id:int}", async (int id, AppDb db) =>
    {
        var cur = await set(db).FindAsync(id);
        if (cur == null) return Results.NotFound();
        if (cascade != null) await cascade(db, id);
        set(db).Remove(cur);
        await db.SaveChangesAsync();
        return Results.Ok();
    });
}

Crud<ClassRoom>("classes", d => d.Classes, async (db, id) =>
{
    var sessionIds = await db.Sessions.Where(s => s.ClassId == id).Select(s => s.Id).ToListAsync();
    await db.Attendances.Where(a => sessionIds.Contains(a.SessionId)).ExecuteDeleteAsync();
    await db.Sessions.Where(s => s.ClassId == id).ExecuteDeleteAsync();
    await db.Enrollments.Where(e => e.ClassId == id).ExecuteDeleteAsync();
});
Crud<Enrollment>("enrollments", d => d.Enrollments);
Crud<AttSession>("sessions", d => d.Sessions, async (db, id) =>
{
    await db.Attendances.Where(a => a.SessionId == id).ExecuteDeleteAsync();
});
Crud<Attendance>("attendances", d => d.Attendances);

app.Run();

// ===================== KIỂU DỮ LIỆU PHỤ =====================
public record LoginReq(string? Username, string? Password);
public record OtpReq(string? Email);
public record VerifyReq(string? Email, string? Code);
public record SendRes(bool Mailed, string? DemoCode);
public record VerifyRes(bool Ok, string Message);
public class OtpEntry { public string Code = ""; public DateTime Exp; public int Fail; }
