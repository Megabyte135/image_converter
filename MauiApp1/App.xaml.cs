namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            bool isTutorialShown = Preferences.Get("IsTutorialShown", false);

            if (!isTutorialShown)
            {
                MainPage = new NavigationPage(new TutorialPage());
            }
            else
            {
                MainPage = new AppShell();
            }
        }
    }
}
