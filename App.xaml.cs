namespace Scrubber
{
    public partial class App : Application
    {
        public static List<string> SelectedFiles { get; set; }
        public static List<byte[]> EncryptedContents { get; set; }
        public static string SelectedFolderPath { get; set; }
        public static bool IsType835Checked { get; set; }
        public static bool IsType837Checked { get; set; }
        public static bool IsTypebothChecked { get; set; }
        public static string EncryptionKey { get; set; }
        public static string ICD10Codes { get; set; }
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            const int newWidth = 1000;
            const int newHeight = 800;

            window.Width = newWidth;
            window.Height = newHeight;

            return window;
        }
    }
}