
namespace VoThiMinhTho_2380602155_PHT;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new LoginPage());
    }
}
