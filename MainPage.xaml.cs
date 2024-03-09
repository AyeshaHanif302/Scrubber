using CommunityToolkit.Maui.Storage;
using Microsoft.Maui;
using static System.Net.Mime.MediaTypeNames;

namespace Scrubber
{
    public partial class MainPage : ContentPage
    {
        private byte[] EncryptedBytes;
        private bool isType835Checked = false;
        private bool isType837Checked = false;
        public MainPage()
        {
            InitializeComponent();
            Next.IsEnabled = false;
            type835.IsChecked = true;
            Browse.TextChanged += OnEntryTextChanged;
            Destination.TextChanged += OnEntryTextChanged;
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            Next.IsEnabled = !string.IsNullOrWhiteSpace(Browse.Text) && !string.IsNullOrWhiteSpace(Destination.Text);
            Next.BackgroundColor = Next.IsEnabled ? Color.FromHex("#20B2AA") : Color.FromHex("#F0F8FF");
        }

        //File browsing
        private async void BrowseFile_Clicked(object sender, EventArgs e)
        {
            try
            {
                var fileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".txt" } },
                });

                var fileoptions = new PickOptions
                {
                    PickerTitle = "Please select a text file",
                    FileTypes = fileTypes
                };

                var response = await FilePicker.PickAsync(fileoptions);

                if (response != null)
                {
                    var stream = await response.OpenReadAsync();
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        EncryptedBytes = memoryStream.ToArray();
                    }

                    Browse.Text = response.FileName;
                    App.SelectedFile = response.FileName;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "Ok");
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
            if (isType835Checked)
            {
                await Navigation.PushAsync(new File835Page(App.SelectedFile, EncryptedBytes, App.SelectedFolderPath));
            }
            else if (isType837Checked)
            {
                await Navigation.PushAsync(new File837Page(App.SelectedFile, EncryptedBytes, App.SelectedFolderPath));
            }
            else
            {
                await DisplayAlert("Error", "Please select a radio button.", "OK");
                //await Navigation.PushAsync(new ElementPage(App.SelectedFile, EncryptedBytes, App.SelectedFolderPath));
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
                    App.SelectedFolderPath = selectedFolderPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error selecting folder: {ex.Message}", "Ok");
            }
        }
        private void OnRadioButtonChecked(object sender, CheckedChangedEventArgs e)
        {
            if (sender == type835)
            {
                isType835Checked = e.Value;
            }
            else if (sender == type837)
            {
                isType837Checked = e.Value;
            }
        }


        //[Obsolete]
        //private async void ShowCustomAlert(string message)
        //{
        //    var progressBar = new ProgressBar
        //    {
        //        Progress = 1,
        //        HeightRequest = 50, 
        //        HorizontalOptions = LayoutOptions.FillAndExpand,
        //        BackgroundColor = Color.FromHex("#20B2AA") 
        //    };

        //    var customAlert = new ContentPage
        //    {
        //        Content = new StackLayout
        //        {
        //            Padding = new Thickness(20),
        //            BackgroundColor = Color.FromHex("#FFFFFF"), 
        //            Children =
        //            {
        //                new Label
        //                {
        //                    Text = message,
        //                    TextColor = Color.FromHex("#000000"), 
        //                    FontSize = 16,
        //                    Margin = new Thickness(0, 0, 0, 10)
        //                },
        //                progressBar, 
        //                new Button
        //                {
        //                    Text = "OK",
        //                    BackgroundColor = Color.FromHex("#20B2AA"), 
        //                    TextColor = Color.FromHex("#FFFFFF"),
        //                    HeightRequest = 50,
        //                    WidthRequest = 60,
        //                    Command = new Command(async () =>
        //                    {
        //                        await Navigation.PopModalAsync();
        //                    })
        //                }
        //            }
        //        },
        //        HeightRequest = 200,
        //        WidthRequest = 400
        //    };

        //    await Navigation.PushModalAsync(new NavigationPage(customAlert));

        //    int timerDurationInSeconds = 5;
        //    int updateIntervalMilliseconds = 100;
        //    int steps = timerDurationInSeconds * 1000 / updateIntervalMilliseconds;

        //    for (int i = 0; i < steps; i++)
        //    {
        //        await Task.Delay(updateIntervalMilliseconds);
        //        progressBar.Progress = 1 - (double)i / steps;
        //    }

        //    await Navigation.PopModalAsync();
        //}

    }
}