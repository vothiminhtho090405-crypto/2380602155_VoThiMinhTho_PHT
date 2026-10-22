using Microsoft.EntityFrameworkCore;

namespace VoThiMinhTho_2380602155_PHT_API;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ClassRoom> Classes => Set<ClassRoom>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttSession> Sessions => Set<AttSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Username).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<ClassRoom>().HasIndex(x => x.Code).IsUnique();
    }
}
