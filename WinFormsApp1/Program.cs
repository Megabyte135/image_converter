using BitmapEncode;
using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageConverterApp
{
    public partial class MainForm : Form
    {
        private PictureBox inputPictureBox;
        private PictureBox outputPictureBox;
        private ComboBox pixelFormatComboBox;
        private Button convertButton;
        private TextBox resultTextBox;
        private string inputFilePath;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Image Converter";
            this.Width = 800;
            this.Height = 600;

            inputPictureBox = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(inputPictureBox);

            outputPictureBox = new PictureBox
            {
                Location = new Point(300, 20),
                Size = new Size(250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(outputPictureBox);

            Button loadImageButton = new Button
            {
                Text = "Load Image",
                Location = new Point(20, 290),
                Width = 100
            };
            loadImageButton.Click += LoadImageButton_Click;
            this.Controls.Add(loadImageButton);

            pixelFormatComboBox = new ComboBox
            {
                Location = new Point(150, 290),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var format in Enum.GetValues(typeof(PixelFormat)))
            {
                pixelFormatComboBox.Items.Add(format);
            }
            pixelFormatComboBox.SelectedIndex = 0;
            this.Controls.Add(pixelFormatComboBox);

            convertButton = new Button
            {
                Text = "Convert",
                Location = new Point(320, 290),
                Width = 100
            };
            convertButton.Click += ConvertButton_Click;
            this.Controls.Add(convertButton);

            resultTextBox = new TextBox
            {
                Location = new Point(20, 330),
                Width = 530,
                Height = 200,
                Multiline = true,
                ReadOnly = true
            };
            this.Controls.Add(resultTextBox);
        }


        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.ico",
                Title = "Select an Image"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                inputFilePath = openFileDialog.FileName;

                // Загружаем изображение в память
                using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                {
                    inputPictureBox.Image = Image.FromStream(fs);
                }
            }
        }


        private void ConvertButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PixelFormat selectedFormat = (PixelFormat)pixelFormatComboBox.SelectedItem;
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), "output.bmp");

            try
            {
                // Создаем и сохраняем изображение
                CustomBitmap customBitmap = new(inputFilePath, selectedFormat);
                customBitmap.ToImage(outputFilePath);

                // Создаем объект BitmapMeta
                BitmapMeta<CustomBitmap> meta = new(inputFilePath, outputFilePath, customBitmap);

                // Освобождаем предыдущие изображения
                outputPictureBox.Image?.Dispose();

                // Отображаем выходное изображение
                using (var fs = new FileStream(outputFilePath, FileMode.Open, FileAccess.Read))
                {
                    outputPictureBox.Image = Image.FromStream(fs);
                }

                // Заполняем текстовое поле
                resultTextBox.Text = string.Join(Environment.NewLine, new[]
                {
            $"Original Size: {meta.OriginalSize} bytes",
            $"Compressed Size: {meta.CompressedSize} bytes",
            $"Entropy: {meta.CalculateEntropy():F4}",
            $"Redundancy: {meta.CalculateRedundancy():P2}",
            $"Compression Rate: {meta.CalculateCompressionRate():F2}%"
        });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during conversion: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
