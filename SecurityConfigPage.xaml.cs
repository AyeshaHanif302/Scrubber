using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;

namespace Scrubber;

public partial class SecurityConfigPage : ContentPage
{
    private List<byte[]> EncryptedContent;
    private List<string> SelectedFile;
    private string SelectedFolderPath;
    private bool IsType835Checked;
    private bool IsType837Checked;
    private bool IsTypebothChecked;
    public SecurityConfigPage(List<string> selectedFile, List<byte[]> encryptedContent, string selectedFolderPath, bool is835, bool is837, bool isboth)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedContent = encryptedContent;
        IsType835Checked = is835;
        IsType837Checked = is837;
        IsTypebothChecked = isboth;
        btnNext.IsEnabled = false;

        string savedKey = Preferences.Get("EncryptionKey", string.Empty);
        if (string.IsNullOrEmpty(savedKey))
        {
            savedKey = GenerateScrubberKey();
            Preferences.Set("EncryptionKey", savedKey);
        }
        keyTextBox.Text = savedKey;

        // Store the encryption key globally
        GlobalVariables.EncryptionKey = savedKey;

    }

    #region Navigation
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        btnNext.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text);
        btnNext.BackgroundColor = btnNext.IsEnabled ? Color.FromRgb(173, 216, 230) : Color.FromRgb(211, 211, 211);

        nvgICD10.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text);
        nvgICD10.BackgroundColor = nvgICD10.IsEnabled ? Color.FromRgb(240, 248, 255) : Color.FromRgb(211, 211, 211);

        nvgFiles.IsEnabled = nvgICD10.IsEnabled;
        nvgFiles.BackgroundColor = nvgFiles.IsEnabled ? Color.FromRgb(240, 248, 255) : Color.FromRgb(211, 211, 211);
    }
    private async void Next_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ICD_10Page(SelectedFile, EncryptedContent, SelectedFolderPath, IsType835Checked, IsType837Checked, IsTypebothChecked));
    }

    private async void Location_Tapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async void Encryption_Tapped(object sender, EventArgs e)
    {
    }
    private async void ICD10_Tapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ICD_10Page(SelectedFile, EncryptedContent, SelectedFolderPath, IsType835Checked, IsType837Checked, IsTypebothChecked));
    }
    private async void Files_Tapped(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedContent, SelectedFolderPath));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedContent, SelectedFolderPath, App.ICD10Codes));
        }
        else if (IsTypebothChecked)
        {
            await Navigation.PushAsync(new ElementsPage(SelectedFile, EncryptedContent, SelectedFolderPath, keyTextBox.Text, App.ICD10Codes));
        }
    }

    #endregion

    #region Generate Encryption Key
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

        // Save the generated key
        Preferences.Set("EncryptionKey", keyString);

        generateKeyButton.IsEnabled = false;
    }
    
    #endregion
}