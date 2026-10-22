using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace VoThiMinhTho_2380602155_PHT;

/// <summary>HttpClient dùng chung để gọi Web API.</summary>
public static class Api
{
    public static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri(Config.ApiUrl),
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Route<T>() => typeof(T).Name switch
    {
        nameof(User) => "users",
        nameof(ClassRoom) => "classes",
        nameof(Enrollment) => "enrollments",
        nameof(AttSession) => "sessions",
        nameof(Attendance) => "attendances",
        _ => throw new InvalidOperationException("Chưa khai báo route cho " + typeof(T).Name)
    };
}

/// <summary>Thay thế kết nối SQLite: các trang vẫn gọi Db.C.Table&lt;T&gt;()... nhưng dữ liệu đến từ API.</summary>
public class ApiConn
{
    public ApiTable<T> Table<T>() where T : class, new() => new();

    public async Task<int> InsertAsync<T>(T obj) where T : class
    {
        var r = await Api.Http.PostAsJsonAsync("api/" + Api.Route<T>(), obj, Api.Json);
        r.EnsureSuccessStatusCode();
        var created = await r.Content.ReadFromJsonAsync<T>(Api.Json);
        var p = typeof(T).GetProperty("Id")!;
        p.SetValue(obj, p.GetValue(created));   // lấy Id do SQL Server cấp
        return 1;
    }

    public async Task<int> UpdateAsync<T>(T obj) where T : class
    {
        var id = (int)typeof(T).GetProperty("Id")!.GetValue(obj)!;
        var r = await Api.Http.PutAsJsonAsync($"api/{Api.Route<T>()}/{id}", obj, Api.Json);
        r.EnsureSuccessStatusCode();
        return 1;
    }

    public async Task<int> DeleteAsync<T>(int id) where T : class
    {
        var r = await Api.Http.DeleteAsync($"api/{Api.Route<T>()}/{id}");
        r.EnsureSuccessStatusCode();
        return 1;
    }

    public async Task<T?> FindAsync<T>(int id) where T : class
    {
        var r = await Api.Http.GetAsync($"api/{Api.Route<T>()}/{id}");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<T>(Api.Json);
    }
}

public class ApiTable<T> where T : class, new()
{
    Func<T, bool>? _pred;

    public ApiTable<T> Where(Expression<Func<T, bool>> p)
    {
        var f = p.Compile();
        var prev = _pred;
        return new ApiTable<T> { _pred = prev == null ? f : x => prev(x) && f(x) };
    }

    public async Task<List<T>> ToListAsync()
    {
        var all = await Api.Http.GetFromJsonAsync<List<T>>("api/" + Api.Route<T>(), Api.Json) ?? new();
        return _pred == null ? all : all.Where(_pred).ToList();
    }

    public async Task<int> CountAsync() => (await ToListAsync()).Count;
    public async Task<T?> FirstOrDefaultAsync() => (await ToListAsync()).FirstOrDefault();
}
