using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VoThiMinhTho_2380602155_PHT;

public class CheckInPage : ContentPage
{
    readonly CameraBarcodeReaderView _reader;
    readonly Label _status;
    readonly Label _motionLabel;
    readonly ProgressBar _motionBar;
    readonly Entry _manual;
    readonly MotionMeter _motion = new();
    bool _busy;

    public CheckInPage()
    {
        Title = "Điểm danh";

        _reader = new CameraBarcodeReaderView
        {
            HeightRequest = 300,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            }
        };
        _reader.BarcodesDetected += OnDetected;

        _status = UI.Sub("Hướng camera vào mã QR trên màn hình của giảng viên.");
        _motionLabel = UI.Sub("Mức chuyển động: 0.00 g");
        _motionBar = new ProgressBar { Progress = 0, ProgressColor = UI.Primary };
        _manual = UI.Field("Ví dụ: 1-ab12cd34");

        _motion.Changed += v => MainThread.BeginInvokeOnMainThread(() =>
        {
            _motionBar.Progress = Math.Min(1, v / 1.5);
            _motionLabel.Text = $"Mức chuyển động: {v:0.00} g";
        });

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    UI.H1("Quét mã QR điểm danh"),
                    _status,
                    new Border
                    {
                        Content = _reader, Padding = 0, StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 }
                    },
                    UI.Card(new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children = { UI.Txt("📳 Cảm biến chuyển động (gia tốc kế)", 15, true), _motionBar, _motionLabel }
                    }),
                    UI.Btn("Quét lại", OnRescan),
                    UI.Card(new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children = { UI.Txt("Hoặc nhập mã bằng tay", 15, true), _manual, UI.Btn("Điểm danh bằng mã", OnManual) }
                    })
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var st = await Permissions.RequestAsync<Permissions.Camera>();
        if (st != PermissionStatus.Granted) { Status("Cần cấp quyền camera để quét mã QR.", UI.Danger); return; }
        _busy = false;
        _reader.IsDetecting = true;
        _motion.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _reader.IsDetecting = false;
        _motion.Stop();
    }

    void Status(string text, Color? c = null)
    {
        _status.Text = text;
        _status.TextColor = c ?? UI.Muted;
    }

    void Fail(string msg) => Status("❌ " + msg, UI.Danger);

    void OnRescan(object? s, EventArgs e)
    {
        _busy = false;
        Status("Hướng camera vào mã QR trên màn hình của giảng viên.");
        _reader.IsDetecting = true;
    }

    async void OnManual(object? s, EventArgs e)
    {
        var t = (_manual.Text ?? "").Trim();
        if (t == "") { Fail("Hãy nhập mã."); return; }
        if (!t.StartsWith("DD|")) t = "DD|" + t.Replace('-', '|');
        _reader.IsDetecting = false;
        _busy = true;
        await Process(t);
    }

    void OnDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (value == null || _busy) return;
        _busy = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _reader.IsDetecting = false;
            await Process(value);
        });
    }

    async Task Process(string value)
    {
        var p = value.Split('|');
        if (p.Length != 3 || p[0] != "DD" || !int.TryParse(p[1], out var sid)) { Fail("Mã QR không hợp lệ."); return; }

        var ses = await Db.C.FindAsync<AttSession>(sid);
        if (ses == null || ses.Token != p[2]) { Fail("Mã QR không đúng hoặc buổi học đã bị xóa."); return; }
        if (!ses.IsOpen) { Fail("Buổi điểm danh đã đóng."); return; }

        int uid = AppState.Me.Id, cid = ses.ClassId;
        if (await Db.C.Table<Enrollment>().Where(x => x.ClassId == cid && x.StudentId == uid).CountAsync() == 0)
        { Fail("Bạn không thuộc lớp này."); return; }
        if (await Db.C.Table<Attendance>().Where(x => x.SessionId == sid && x.StudentId == uid).CountAsync() > 0)
        { Fail("Bạn đã điểm danh buổi này rồi."); return; }

        Status("Đang lấy vị trí GPS...");
        var loc = await Geo.GetAsync();
        if (loc == null) { Fail("Không lấy được vị trí. Hãy bật GPS và cấp quyền."); return; }
        var dist = Geo.Meters(loc.Latitude, loc.Longitude, ses.Lat, ses.Lon);
        if (dist > ses.RadiusM) { Fail($"Bạn cách lớp học {dist:0} m (tối đa {ses.RadiusM} m)."); return; }

        var method = "QR + GPS";
        if (Biometric.IsAvailable())
        {
            var ok = await Biometric.AuthenticateAsync("Xác thực vân tay", "Chạm vào cảm biến để xác nhận điểm danh");
            if (!ok) { Fail("Xác thực vân tay thất bại hoặc đã hủy."); return; }
            method += " + Vân tay";
        }
        else method += " (máy chưa cài vân tay)";

        await Db.C.InsertAsync(new Attendance
        {
            SessionId = sid,
            StudentId = uid,
            Time = DateTime.Now,
            Lat = loc.Latitude,
            Lon = loc.Longitude,
            DistanceM = dist,
            Motion = _motion.Level,
            Method = method
        });
        Status($"✅ Điểm danh thành công! ({dist:0} m)", UI.Success);
        await DisplayAlert("Thành công", $"Đã điểm danh “{ses.Title}”.\nPhương thức: {method}", "OK");
    }
}