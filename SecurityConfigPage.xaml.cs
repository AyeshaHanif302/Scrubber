using System.Security.Cryptography;

namespace Scrubber;

public partial class SecurityConfigPage : ContentPage
{
    private byte[] EncryptedBytes;
    private string SelectedFile;
    private string SelectedFolderPath;
    private bool IsType835Checked;
    private bool IsType837Checked;
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
        Next.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text) && !string.IsNullOrWhiteSpace(ivTextBox.Text);
        Next.BackgroundColor = Next.IsEnabled ? Color.FromHex("#20B2AA") : Color.FromHex("#F0F8FF");
    }

    //Generate Key
    private byte[] GenerateRandomKey()
    {
        byte[] key = new byte[32]; // 256 bits
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }
    private byte[] GenerateRandomIV()
    {
        byte[] iv = new byte[16]; // 128 bits
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }
    private void GenerateKey_Click(object sender, EventArgs e)
    {
        byte[] generatedKey = GenerateRandomKey();
        string keyString = BitConverter.ToString(generatedKey).Replace("-", "");
        keyTextBox.Text = keyString;

        byte[] generatedIV = GenerateRandomIV();
        string ivString = BitConverter.ToString(generatedIV).Replace("-", "");
        ivTextBox.Text = ivString;

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
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedBytes, SelectedFolderPath, keyTextBox.Text, ivTextBox.Text));
        }
        else
        {
            await DisplayAlert("Error", "Please select a radio button.", "OK");
        }
    }
}