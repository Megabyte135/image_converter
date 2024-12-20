namespace MauiApp1
{
    public partial class HelpPage : ContentPage
    {
        public HelpPage()
        {
            InitializeComponent();

            var tap1 = new TapGestureRecognizer();
            tap1.Tapped += (s, e) => ToggleSection(section1Content);
            section1Header.GestureRecognizers.Add(tap1);

            var tap2 = new TapGestureRecognizer();
            tap2.Tapped += (s, e) => ToggleSection(section2Content);
            section2Header.GestureRecognizers.Add(tap2);

            var tap3 = new TapGestureRecognizer();
            tap3.Tapped += (s, e) => ToggleSection(section3Content);
            section3Header.GestureRecognizers.Add(tap3);

            var tap4 = new TapGestureRecognizer();
            tap4.Tapped += (s, e) => ToggleSection(section4Content);
            section4Header.GestureRecognizers.Add(tap4);
        }

        private async void ToggleSection(VerticalStackLayout section)
        {
            if (section.IsVisible)
            {
                await section.FadeTo(0, 300, Easing.SinInOut);
                section.IsVisible = false;
            }
            else
            {
                section.IsVisible = true;
                await section.FadeTo(1, 300, Easing.SinInOut);
            }
        }
    }
}
