
using Microsoft.Maui.Controls.Shapes;

namespace VoThiMinhTho_2380602155_PHT;

public static class UI
{
    public static readonly Color Primary = Color.FromArgb("#2563EB");
    public static readonly Color Danger = Color.FromArgb("#DC2626");
    public static readonly Color Success = Color.FromArgb("#16A34A");
    public static readonly Color Warn = Color.FromArgb("#D97706");
    public static readonly Color Gray = Color.FromArgb("#64748B");
    public static readonly Color Muted = Color.FromArgb("#6B7280");
    public static readonly Color TextDark = Color.FromArgb("#1F2937");

    public static Label H1(string t) => new()
    {
        Text = t,
        FontSize = 22,
        FontAttributes = FontAttributes.Bold
    };

    public static Label Sub(string t) => new()
    {
        Text = t,
        FontSize = 13,
        TextColor = Muted
    };

    public static Label Txt(string t, double size = 15, bool bold = false) =>
        new()
        {
            Text = t,
            FontSize = size,
            FontAttributes = bold
                ? FontAttributes.Bold
                : FontAttributes.None
        };

    public static Border Card(View v) => new()
    {
        Content = v,
        Padding = 14,
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E5E7EB"),
        StrokeShape = new RoundRectangle
        {
            CornerRadius = 16
        }
    };

    public static Border Badge(string t, Color c) => new()
    {
        BackgroundColor = c,
        Padding = new Thickness(10, 3),
        StrokeThickness = 0,
        HorizontalOptions = LayoutOptions.Start,
        StrokeShape = new RoundRectangle
        {
            CornerRadius = 10
        },
        Content = new Label
        {
            Text = t,
            FontSize = 11,
            TextColor = Colors.White
        }
    };

    public static Border Stat(string label, string val, Color c) =>
        Card(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = val,
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = c
                },
                Sub(label)
            }
        });

    public static Button Btn(
        string text,
        EventHandler click,
        Color? bg = null,
        bool small = false)
    {
        var b = new Button
        {
            Text = text,
            BackgroundColor = bg ?? Primary,
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = small ? 36 : 46,
            FontSize = small ? 13 : 15,
            Padding = new Thickness(small ? 12 : 16, 0)
        };

        b.Clicked += click;
        return b;
    }

    public static Entry Field(
        string placeholder,
        bool password = false,
        Keyboard? kb = null) =>
        new()
        {
            Placeholder = placeholder,
            IsPassword = password,
            Keyboard = kb ?? Keyboard.Default
        };

    public static HorizontalStackLayout Row(params View[] views)
    {
        var h = new HorizontalStackLayout { Spacing = 8 };

        foreach (var v in views)
            h.Children.Add(v);

        return h;
    }

    public static Task Alert(string title, string msg) =>
        Application.Current!.MainPage!.DisplayAlert(
            title, msg, "OK");

    public static Task<bool> Confirm(string msg) =>
        Application.Current!.MainPage!.DisplayAlert(
            "Xác nhận", msg, "Đồng ý", "Hủy");
}

/// <summary>
/// Trang cơ bản: ScrollView + VerticalStackLayout,
/// tự tải lại dữ liệu mỗi lần hiện.
/// </summary>
public abstract class StackPage : ContentPage
{
    protected readonly VerticalStackLayout Body = new()
    {
        Spacing = 12,
        Padding = 16
    };

    protected StackPage(string title)
    {
        Title = title;
        Content = new ScrollView
        {
            Content = Body
        };
    }

    protected abstract Task LoadAsync();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }
}
