using System.IO;

namespace MauiApp1
{
    public partial class GalleryPage : ContentPage
    {
        private List<GalleryItem> galleryItems = new List<GalleryItem>();

        public GalleryPage()
        {
            InitializeComponent();

            layoutPicker.SelectedIndexChanged += OnLayoutChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var items = Preferences.Get("RecentImages", "");
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(items))
            {
                list = items.Split('|').ToList();
            }

            galleryItems = list.Select(path => new GalleryItem
            {
                FilePath = path,
                FileName = Path.GetFileName(path)
            }).ToList();

            collectionView.ItemsSource = galleryItems;

            collectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
        }

        private void OnLayoutChanged(object sender, EventArgs e)
        {
            if (layoutPicker.SelectedIndex == -1)
                return;

            var selectedLayout = layoutPicker.Items[layoutPicker.SelectedIndex];

            if (selectedLayout == "Вертикальный список")
            {
                collectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            }
            else if (selectedLayout == "Сетка 3x3")
            {
                collectionView.ItemsLayout = new GridItemsLayout(3, ItemsLayoutOrientation.Vertical);
            }
            else if (selectedLayout == "Сетка 4x4")
            {
                collectionView.ItemsLayout = new GridItemsLayout(4, ItemsLayoutOrientation.Vertical);
            }
        }
    }

    public class GalleryItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }
}
