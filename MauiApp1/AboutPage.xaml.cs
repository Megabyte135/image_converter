using System.Windows.Input;

namespace MauiApp1
{
    public partial class AboutPage : ContentPage
    {
        public ICommand OpenLinkCommand { get; private set; }

        public AboutPage()
        {
            InitializeComponent();
            OpenLinkCommand = new Command(async () =>
            {
                await Launcher.OpenAsync("https://github.com/Megabyte135/");
            });
            BindingContext = this;
        }
    }
}
