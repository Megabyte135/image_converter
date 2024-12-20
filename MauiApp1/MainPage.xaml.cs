using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        private string selectedFilePath;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSelectImageClicked(object sender, EventArgs e)
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Выберите изображение"
            });

            if (file != null)
            {
                var supportedExtensions = new[] { ".bmp", ".jpg", ".jpeg", ".png", ".ico" };
                if (!supportedExtensions.Contains(Path.GetExtension(file.FullPath).ToLower()))
                {
                    await DisplayAlert("Ошибка", "Формат файла не поддерживается.", "OK");
                    return;
                }

                selectedFilePath = file.FullPath;
                SelectedImage.Source = ImageSource.FromFile(selectedFilePath);

                var items = Preferences.Get("RecentImages", "");
                var list = new List<string>();
                if (!string.IsNullOrWhiteSpace(items))
                {
                    list = items.Split('|').ToList();
                }
                if (!list.Contains(selectedFilePath))
                {
                    list.Insert(0, selectedFilePath);
                    if (list.Count > 10) list = list.Take(10).ToList();
                    Preferences.Set("RecentImages", string.Join("|", list));
                }

                await Navigation.PushAsync(new ImageEditorPage(selectedFilePath));
            }
        }

        private async void OnSelectedImageTapped(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                await Navigation.PushAsync(new ImageEditorPage(selectedFilePath));
            }
            else
            {
                await DisplayAlert("Информация", "Изображение не выбрано.", "OK");
            }
        }

    }
}
