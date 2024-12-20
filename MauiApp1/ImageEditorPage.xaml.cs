using BitmapEncode;
using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using SkiaSharp;

namespace MauiApp1
{
    public partial class ImageEditorPage : ContentPage
    {
        private SKBitmap currentBitmap;
        private SKBitmap originalBitmap;
        private string currentFilePath;
        private string outputFilePath;
        private byte[] currentImageData;

        public ImageEditorPage(string imagePath)
        {
            InitializeComponent();
            currentFilePath = imagePath;
            cropButton.Clicked += OnCropClicked;
            filterButton.Clicked += OnFilterClicked;
            convertButton.Clicked += OnConvertClicked;
            saveButton.Clicked += OnSaveImageClicked;
            editableImage.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => OnImageTapped())
            });
            UpdateImageSource();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadImage();
            UpdateImageSource();
        }

        private void OnImageTapped()
        {
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                Navigation.PushAsync(new ImageEditorPage(currentFilePath));
            }
        }

        public void SetNewImage(string newImagePath)
        {
            currentFilePath = newImagePath;
            LoadImage();
        }

        private void LoadImage()
        {
            if (string.IsNullOrEmpty(currentFilePath))
                return;

            using var fs = File.OpenRead(currentFilePath);
            using var stream = new SKManagedStream(fs);
            var bitmap = SKBitmap.Decode(stream);
            currentBitmap = bitmap.Copy();
            originalBitmap = bitmap;

            editableImage.Source = ImageSource.FromFile(currentFilePath);
            editableImage.IsVisible = true;
            buttonsLayout.IsVisible = true;
        }

        private void UpdateImageSource()
        {
            if (currentBitmap == null) return;

            using var ms = new MemoryStream();
            currentBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
            currentImageData = ms.ToArray();
            editableImage.Source = ImageSource.FromStream(() => new MemoryStream(currentImageData));
        }

        private async void OnCropClicked(object sender, EventArgs e)
        {
            if (currentBitmap == null)
            {
                await DisplayAlert("Ошибка", "Изображение не загружено", "OK");
                return;
            }

            await Navigation.PushAsync(new CropPage(currentFilePath));
        }

        private async void OnFilterClicked(object sender, EventArgs e)
        {
            if (currentBitmap == null)
            {
                await DisplayAlert("Ошибка", "Изображение не загружено", "OK");
                return;
            }

            string action = await DisplayActionSheet("Выберите фильтр:", "Отмена", null, "Сепия", "Ч/Б");
            if (action == "Сепия")
            {
                ApplySepiaFilter();
                UpdateImageSource();
            }
            else if (action == "Ч/Б")
            {
                ApplyGrayScaleFilter();
                UpdateImageSource();
            }
        }

        private void ApplyGrayScaleFilter()
        {
            for (int i = 0; i < currentBitmap.Height; i++)
            {
                for (int j = 0; j < currentBitmap.Width; j++)
                {
                    var color = currentBitmap.GetPixel(j, i);
                    byte gray = (byte)((color.Red + color.Green + color.Blue) / 3);
                    currentBitmap.SetPixel(j, i, new SKColor(gray, gray, gray, color.Alpha));
                }
            }
        }

        private void ApplySepiaFilter()
        {
            for (int y = 0; y < currentBitmap.Height; y++)
            {
                for (int x = 0; x < currentBitmap.Width; x++)
                {
                    var color = currentBitmap.GetPixel(x, y);
                    byte r = color.Red;
                    byte g = color.Green;
                    byte b = color.Blue;

                    var tr = (int)(0.393 * r + 0.769 * g + 0.189 * b);
                    var tg = (int)(0.349 * r + 0.686 * g + 0.168 * b);
                    var tb = (int)(0.272 * r + 0.534 * g + 0.131 * b);

                    byte nr = (byte)Math.Min(255, tr);
                    byte ng = (byte)Math.Min(255, tg);
                    byte nb = (byte)Math.Min(255, tb);

                    currentBitmap.SetPixel(x, y, new SKColor(nr, ng, nb, color.Alpha));
                }
            }
        }

        private async void OnConvertClicked(object sender, EventArgs e)
        {
            if (currentBitmap == null)
            {
                await DisplayAlert("Ошибка", "Изображение не загружено", "OK");
                return;
            }

            string action = await DisplayActionSheet("Выберите битность:", "Отмена", null, "1-bit", "8-bit", "24-bit", "32-bit");
            if (action == "Отмена" || string.IsNullOrEmpty(action))
                return;

            PixelFormat selectedFormat;
            switch (action)
            {
                case "1-bit": selectedFormat = PixelFormat.OneBit; break;
                case "8-bit": selectedFormat = PixelFormat.EightBit; break;
                case "24-bit": selectedFormat = PixelFormat.TwentyFourBit; break;
                case "32-bit": selectedFormat = PixelFormat.ThirtyTwoBit; break;
                default: selectedFormat = PixelFormat.TwentyFourBit; break;
            }

            var tempInput = Path.Combine(FileSystem.CacheDirectory, "input_for_convert.png");
            using (var fs = File.OpenWrite(tempInput))
            {
                currentBitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
            }

            outputFilePath = Path.Combine(FileSystem.CacheDirectory, "output.bmp");
            try
            {
                CustomBitmap customBitmap = new(tempInput, selectedFormat);
                customBitmap.ToImage(outputFilePath);
                BitmapMeta<CustomBitmap> meta = new(tempInput, outputFilePath, customBitmap);

                await DisplayAlert("Конвертация",
                    $"Original Size: {meta.OriginalSize} bytes\n" +
                    $"Compressed Size: {meta.CompressedSize} bytes\n" +
                    $"Compression Rate: {meta.CalculateCompressionRate():F2}%", "OK");

                using var resultFs = File.OpenRead(outputFilePath);
                using var resultStream = new SKManagedStream(resultFs);
                currentBitmap = SKBitmap.Decode(resultStream);

                UpdateImageSource();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка во время конвертации: {ex.Message}", "OK");
            }
        }

        private async void OnSaveImageClicked(object sender, EventArgs e)
        {
            if (currentBitmap == null)
            {
                await DisplayAlert("Ошибка", "Нет изображения для сохранения", "OK");
                return;
            }

            var downloadsPath = "/storage/emulated/0/Download";
            var newFileName = $"converted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var destinationPath = Path.Combine(downloadsPath, newFileName);

            using (var fs = File.OpenWrite(destinationPath))
            {
                currentBitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
            }

            await DisplayAlert("Информация", $"Изображение сохранено: {destinationPath}", "OK");
        }
    }
}