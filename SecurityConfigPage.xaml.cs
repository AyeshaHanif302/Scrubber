using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Scrubber;

public partial class SecurityConfigPage : ContentPage
{
    private byte[] EncryptedBytes;
    private string SelectedFile;
    private string SelectedFolderPath;
    private bool IsType835Checked;
    private bool IsType837Checked;
    private readonly IConfiguration _configuration;

    public SecurityConfigPage(string selectedFile, byte[] encryptedBytes, string selectedFolderPath, bool isType835Checked, bool isType837Checked)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
        IsType835Checked = isType835Checked;
        IsType837Checked = isType837Checked;
        Next.IsEnabled = false;
      
        // _configuration = configuration;

        //string key = _configuration["ScrubberKey"];

        //if (!string.IsNullOrWhiteSpace(key))
        //{
        //    keyTextBox.Text = key.Trim();
        //    generateKeyButton.IsEnabled = false;
        //}
        //else
        //    generateKeyButton.IsEnabled = true;

    }
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        Next.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text) || !string.IsNullOrWhiteSpace(ivTextBox.Text);
        Next.BackgroundColor = Next.IsEnabled ? Color.FromRgba(32, 178, 170, 255) : Color.FromRgba(240, 248, 255, 255);
    }

    //Generate Key
    private string GenerateScrubberKey()
    {
        var data = new byte[32];
        RandomNumberGenerator.Fill(data);
        return Convert.ToBase64String(data);
    }
    //private void SaveKeyToConfiguration(string key)
    //{
    //    // Read the configuration from the appsettings.json file
    //    var configBuilder = new ConfigurationBuilder()
    //        .SetBasePath(AppContext.BaseDirectory)
    //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    //    var configuration = configBuilder.Build();

    //    // Update the configuration
    //    var section = configuration.GetSection("AppSettings");
    //    section["ScrubberKey"] = key;
    //}
    //private byte[] GenerateRandomKey()
    //{
    //    byte[] key = new byte[32]; // 256 bits
    //    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
    //    {
    //        rng.GetBytes(key);
    //    }
    //    return key;
    //}
    //private byte[] GenerateRandomIV()
    //{
    //    byte[] iv = new byte[16]; // 128 bits
    //    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
    //    {
    //        rng.GetBytes(iv);
    //    }
    //    return iv;
    //}
    private void GenerateKey_Click(object sender, EventArgs e)
    {
        string generatedKey = GenerateScrubberKey();
        string keyString = (generatedKey).Replace("-", "");
        keyTextBox.Text = keyString;

        generateKeyButton.IsEnabled = false;
    }
    private async void Next_Clicked(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedBytes, SelectedFolderPath, keyTextBox.Text, ivTextBox.Text));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedBytes, SelectedFolderPath, keyTextBox.Text));
        }
        else
        {
            await DisplayAlert("Error", "Please select a radio button.", "OK");
        }
    }
}