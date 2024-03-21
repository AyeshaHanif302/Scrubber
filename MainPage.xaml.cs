using CommunityToolkit.Maui.Storage;

namespace Scrubber
{
    public partial class MainPage : ContentPage
    {
        List<string> SelectedFiles = new List<string>();
        static List<byte[]> EncryptedContents = new List<byte[]>();
        static string SelectedFolderPath;
        static bool IsType835Checked;
        static bool IsType837Checked;
        public MainPage()
        {
            InitializeComponent();
            Next.IsEnabled = false;
            type835.IsChecked = true;
            Browse.TextChanged += OnEntryTextChanged;
            Destination.TextChanged += OnEntryTextChanged;
        }

        #region Navigation and Checkbox Handler
        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            Next.IsEnabled = !string.IsNullOrWhiteSpace(Browse.Text) && !string.IsNullOrWhiteSpace(Destination.Text);
            Next.BackgroundColor = Next.IsEnabled ? Color.FromHex("#20B2AA") : Color.FromHex("#F0F8FF");
        }
        private void OnRadioButtonChecked(object sender, CheckedChangedEventArgs e)
        {
            if (sender == type835)
            {
                IsType835Checked = e.Value;
            }
            else if (sender == type837)
            {
                IsType837Checked = e.Value;
            }
        }
        private void Clear_Clicked(object sender, EventArgs e)
        {
            Destination.Text = string.Empty;
            Browse.Text = string.Empty;
            type835.IsChecked = true;
            type837.IsChecked = false;
        }
        private async void Next_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SecurityConfigPage(SelectedFiles, EncryptedContents, SelectedFolderPath, IsType835Checked, IsType837Checked));
        }

        #endregion

        #region Directory Selection
        private async void BrowseFile_Clicked(object sender, EventArgs e)
        {
            try
            {
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                    { DevicePlatform.WinUI, new[] { ".txt" } }
                    });

                var fileOptions = new PickOptions
                {
                    PickerTitle = "Please select text files",
                    FileTypes = fileTypes
                };

                var responses = await FilePicker.PickMultipleAsync(fileOptions);

                if (responses != null && responses.Count() > 0)
                {
                    SelectedFiles.Clear();
                    EncryptedContents.Clear();

                    foreach (var response in responses)
                    {
                        var stream = await response.OpenReadAsync();
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            var fileBytes = memoryStream.ToArray();

                            SelectedFiles.Add(response.FileName);
                            EncryptedContents.Add(fileBytes);
                        }
                    }

                    Browse.Text = string.Join(", ", SelectedFiles);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Ok");
            }
        }   
        private async void Destination_Clicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FolderPicker.PickAsync(default);

                if (result != null)
                {
                    var selectedFolderPath = result.Folder.Path;
                    Destination.Text = selectedFolderPath;
                    SelectedFolderPath = selectedFolderPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error selecting folder: {ex.Message}", "Ok");
            }
        }
       
        #endregion

    }
}