using Microsoft.Maui.Storage;
using System;

namespace MauiApp1
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            themePicker.SelectedIndex = 0;
            themePicker.SelectedIndexChanged += OnThemeSelected;

            resetTutorialButton.Clicked += OnResetTutorialClicked;
        }

        private void OnThemeSelected(object sender, EventArgs e)
        {
            if (themePicker.SelectedIndex == -1)
                return;

            var selectedTheme = themePicker.Items[themePicker.SelectedIndex];
            if (selectedTheme == "Светлая")
            {
                Application.Current.UserAppTheme = AppTheme.Light;
            }
            else if (selectedTheme == "Тёмная")
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
            }
        }

        private void OnResetTutorialClicked(object sender, EventArgs e)
        {
            Preferences.Set("IsTutorialShown", false);
            DisplayAlert("Сброс обучения", "Обучение будет показано снова при следующем запуске приложения.", "OK");
        }
    }
}
