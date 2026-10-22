
using System.Globalization;
using Microsoft.Maui.Devices.Sensors;
using VoThiMinhTho_2380602155_PHT.Models;

namespace VoThiMinhTho_2380602155_PHT;

public static class AppState
{
    public static User Me { get; set; } = null!;
}

public static class Nav
{
    public static NavigationPage Wrap(Page p, string title) =>
        new(p)
        {
            Title = title,
            BarBackgroundColor = Colors.White,
            BarTextColor = UI.TextDark
        };

    public static void GoHome(User u)
    {
        AppState.Me = u;
        Application.Current!.MainPage = new MainTabs(u);
    }

    public static void Logout()
    {
        AppState.Me = null!;
        Application.Current!.MainPage =
            new NavigationPage(new LoginPage());
    }
}

public static class Geo
{
    public static async Task<Location?> GetAsync()
    {
        var st = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (st != PermissionStatus.Granted)
            return null;

        try
        {
            return await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(12)));
        }
        catch
        {
            return null;
        }
    }

    public static double Meters(
        double lat1, double lon1,
        double lat2, double lon2) =>
        Location.CalculateDistance(
            lat1, lon1, lat2, lon2,
            DistanceUnits.Kilometers) * 1000;

    public static string Link(double lat, double lon) =>
        $"https://maps.google.com/?q={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Đo chuyển động bằng gia tốc kế:
/// trả về độ lệch so với trọng lực (đơn vị g).
/// </summary>
public class MotionMeter
{
    public event Action<double>? Changed;

    public double Level { get; private set; }

    public void Start()
    {
        if (!Accelerometer.Default.IsSupported ||
            Accelerometer.Default.IsMonitoring)
            return;

        Accelerometer.Default.ReadingChanged += OnReading;
        Accelerometer.Default.Start(SensorSpeed.UI);
    }

    public void Stop()
    {
        if (!Accelerometer.Default.IsMonitoring)
            return;

        Accelerometer.Default.Stop();
        Accelerometer.Default.ReadingChanged -= OnReading;
    }

    void OnReading(object? s, AccelerometerChangedEventArgs e)
    {
        var a = e.Reading.Acceleration;

        var mag = Math.Sqrt(
            a.X * a.X +
            a.Y * a.Y +
            a.Z * a.Z);

        Level = Level * 0.8 + Math.Abs(mag - 1.0) * 0.2;

        Changed?.Invoke(Level);
    }
}
