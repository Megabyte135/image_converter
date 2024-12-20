using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace MauiApp1
{
    public partial class CropPage : ContentPage
    {
        private readonly string imagePath;
        private double imageScale;
        private RectF originalImageRect;
        private SKBitmap originalBitmap;
        private PointF lastTouchPoint;
        private BoxView selectedHandle;
        private bool isDragging = false;

        public CropPage(string imagePath)
        {
            InitializeComponent();
            this.imagePath = imagePath;

            cancelButton.Clicked += OnCancelClicked;
            confirmButton.Clicked += OnConfirmClicked;

            var handles = new[] { topLeftHandle, topRightHandle, bottomLeftHandle, bottomRightHandle };
            foreach (var handle in handles)
            {
                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += OnHandlePanned;
                handle.GestureRecognizers.Add(panGesture);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadImage();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnImageTapped;
            cropContainer.GestureRecognizers.Add(tapGesture);
        }

        private void LoadImage()
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            sourceImage.Source = ImageSource.FromFile(imagePath);

            // Load original bitmap for later cropping
            using var stream = File.OpenRead(imagePath);
            using var skStream = new SKManagedStream(stream);
            originalBitmap = SKBitmap.Decode(skStream);

            sourceImage.SizeChanged += OnImageSizeChanged;
        }

        private void OnImageSizeChanged(object sender, EventArgs e)
        {
            if (sourceImage.Width <= 0 || sourceImage.Height <= 0)
                return;

            // Calculate image scale and position
            var containerAspect = cropContainer.Width / cropContainer.Height;
            var imageAspect = originalBitmap.Width / (double)originalBitmap.Height;

            if (imageAspect > containerAspect)
            {
                imageScale = cropContainer.Width / originalBitmap.Width;
                originalImageRect = new RectF(
                    0,
                    (float)((cropContainer.Height - originalBitmap.Height * imageScale) / 2),
                    (float)cropContainer.Width,
                    (float)(originalBitmap.Height * imageScale)
                );
            }
            else
            {
                imageScale = cropContainer.Height / originalBitmap.Height;
                originalImageRect = new RectF(
                    (float)((cropContainer.Width - originalBitmap.Width * imageScale) / 2),
                    0,
                    (float)(originalBitmap.Width * imageScale),
                    (float)cropContainer.Height
                );
            }
        }

        private void InitializeCropFrame(double x, double y)
        {
            cropFrame.IsVisible = true;

            // Set initial crop area size
            double cropWidth = 100;
            double cropHeight = 100;

            // Ensure crop area fits within the image boundaries
            if (x + cropWidth > originalImageRect.Right)
                x = originalImageRect.Right - cropWidth;
            if (y + cropHeight > originalImageRect.Bottom)
                y = originalImageRect.Bottom - cropHeight;

            cropArea.WidthRequest = cropWidth;
            cropArea.HeightRequest = cropHeight;
            cropArea.Margin = new Thickness(x, y, 0, 0);
        }

        private void OnImageTapped(object sender, TappedEventArgs e)
        {
            var tapPosition = e.GetPosition(cropContainer);

            if (tapPosition.HasValue)
            {
                var x = tapPosition.Value.X;
                var y = tapPosition.Value.Y;

                if (x >= originalImageRect.Left && x <= originalImageRect.Right &&
                    y >= originalImageRect.Top && y <= originalImageRect.Bottom)
                {
                    InitializeCropFrame(x, y);
                }
            }
        }

        private void OnHandlePanned(object sender, PanUpdatedEventArgs e)
        {
            var handle = (BoxView)sender;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    isDragging = true;
                    selectedHandle = handle;
                    lastTouchPoint = new PointF((float)e.TotalX, (float)e.TotalY);
                    break;

                case GestureStatus.Running:
                    if (isDragging)
                    {
                        var deltaX = e.TotalX - lastTouchPoint.X;
                        var deltaY = e.TotalY - lastTouchPoint.Y;
                        UpdateCropArea(selectedHandle, deltaX, deltaY);
                        lastTouchPoint = new PointF((float)e.TotalX, (float)e.TotalY);
                    }
                    break;

                case GestureStatus.Completed:
                    isDragging = false;
                    selectedHandle = null;
                    break;
            }
        }

        private void UpdateCropArea(BoxView handle, double deltaX, double deltaY)
        {
            var margin = cropArea.Margin;
            var width = cropArea.Width;
            var height = cropArea.Height;

            if (handle == topLeftHandle)
            {
                width -= deltaX;
                height -= deltaY;
                margin = new Thickness(margin.Left + deltaX, margin.Top + deltaY, margin.Right, margin.Bottom);
            }
            else if (handle == topRightHandle)
            {
                width += deltaX;
                height -= deltaY;
                margin = new Thickness(margin.Left, margin.Top + deltaY, margin.Right, margin.Bottom);
            }
            else if (handle == bottomLeftHandle)
            {
                width -= deltaX;
                height += deltaY;
                margin = new Thickness(margin.Left + deltaX, margin.Top, margin.Right, margin.Bottom);
            }
            else if (handle == bottomRightHandle)
            {
                width += deltaX;
                height += deltaY;
            }

            if (width >= 50 && height >= 50 &&
                margin.Left >= originalImageRect.Left &&
                margin.Top >= originalImageRect.Top &&
                margin.Left + width <= originalImageRect.Right &&
                margin.Top + height <= originalImageRect.Bottom)
            {
                cropArea.WidthRequest = width;
                cropArea.HeightRequest = height;
                cropArea.Margin = margin;
            }
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            try
            {
                var cropX = (int)((cropArea.Margin.Left - originalImageRect.X) / imageScale);
                var cropY = (int)((cropArea.Margin.Top - originalImageRect.Y) / imageScale);
                var cropWidth = (int)(cropArea.Width / imageScale);
                var cropHeight = (int)(cropArea.Height / imageScale);

                cropX = Math.Max(0, Math.Min(cropX, originalBitmap.Width - cropWidth));
                cropY = Math.Max(0, Math.Min(cropY, originalBitmap.Height - cropHeight));
                cropWidth = Math.Min(cropWidth, originalBitmap.Width - cropX);
                cropHeight = Math.Min(cropHeight, originalBitmap.Height - cropY);

                var croppedBitmap = new SKBitmap(cropWidth, cropHeight);
                using (var canvas = new SKCanvas(croppedBitmap))
                {
                    var sourceRect = new SKRect(cropX, cropY, cropX + cropWidth, cropY + cropHeight);
                    var destRect = new SKRect(0, 0, cropWidth, cropHeight);
                    canvas.DrawBitmap(originalBitmap, sourceRect, destRect);
                }

                var tempPath = Path.Combine(FileSystem.CacheDirectory, "cropped_image.png");
                using (var image = SKImage.FromBitmap(croppedBitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(tempPath))
                {
                    data.SaveTo(stream);
                }

                var previousPage = Navigation.NavigationStack.ElementAtOrDefault(Navigation.NavigationStack.Count - 2) as ImageEditorPage;
                previousPage?.SetNewImage(tempPath);
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при обрезке изображения: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}