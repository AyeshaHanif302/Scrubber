namespace Scrubber
{
    public partial class App : Application
    {
        public static string SelectedFile { get; set; }
        public static string SelectedFolderPath { get; set; }
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
        }
    }
}