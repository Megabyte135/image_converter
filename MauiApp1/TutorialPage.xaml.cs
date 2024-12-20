namespace MauiApp1
{
    public partial class TutorialPage : ContentPage
    {
        private int currentStep = 0;
        private readonly string[] stepsInfo =
        {
            "Добро пожаловать! Это приложение для конвертации и редактирования изображений.",
            "Вы можете обрезать изображение, применять фильтры, конвертировать и сохранять.",
            "Нажмите 'Продолжить', чтобы перейти к приложению."
        };

        public TutorialPage()
        {
            InitializeComponent();
            infoLabel.Text = stepsInfo[0];
        }

        private void NextButton_Clicked(object sender, EventArgs e)
        {
            currentStep++;
            if (currentStep < stepsInfo.Length)
            {
                infoLabel.Text = stepsInfo[currentStep];
            }
            else
            {
                EndTutorial();
            }
        }

        private void SkipButton_Clicked(object sender, EventArgs e)
        {
            EndTutorial();
        }

        private void EndTutorial()
        {
            Preferences.Set("IsTutorialShown", true);
            Application.Current.MainPage = new AppShell();
        }
    }
}
