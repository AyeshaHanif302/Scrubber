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
    //private readonly IConfiguration _configuration;

    public SecurityConfigPage(string selectedFile, byte[] encryptedBytes, string selectedFolderPath, bool isType835Checked, bool isType837Checked)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
        IsType835Checked = isType835Checked;
        IsType837Checked = isType837Checked;
        Next.IsEnabled = false;

    }
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        Next.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text);
        Next.BackgroundColor = Next.IsEnabled ? Color.FromRgba(32, 178, 170, 255) : Color.FromRgba(240, 248, 255, 255);
    }

    //Generate Key
    private string GenerateScrubberKey()
    {
        var data = new byte[32];
        RandomNumberGenerator.Fill(data);
        return Convert.ToBase64String(data);
    }
    private void GenerateKey_Click(object sender, EventArgs e)
    {
        string generatedKey = GenerateScrubberKey();
        string keyString = (generatedKey).Replace("-", "");
        keyTextBox.Text = keyString;

        generateKeyButton.IsEnabled = false;
    }
    private async void Next_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ICD_10Page(SelectedFile, EncryptedBytes, SelectedFolderPath, keyTextBox.Text, IsType835Checked, IsType837Checked));
    }
}