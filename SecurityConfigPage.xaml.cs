using System.Security.Cryptography;

namespace Scrubber;

public partial class SecurityConfigPage : ContentPage
{
    private List<byte[]> EncryptedContent;
    private List<string> SelectedFile;
    private string SelectedFolderPath;
    private bool IsType835Checked;
    private bool IsType837Checked;
    public SecurityConfigPage(List<string> selectedFile, List<byte[]> encryptedContent, string selectedFolderPath, bool isType835Checked, bool isType837Checked)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedContent = encryptedContent;
        IsType835Checked = isType835Checked;
        IsType837Checked = isType837Checked;
        Next.IsEnabled = false;
    }
    
    #region Navigation
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        Next.IsEnabled = !string.IsNullOrWhiteSpace(keyTextBox.Text);
        Next.BackgroundColor = Next.IsEnabled ? Color.FromRgba(32, 178, 170, 255) : Color.FromRgba(240, 248, 255, 255);
    }
    private async void Next_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ICD_10Page(SelectedFile, EncryptedContent, SelectedFolderPath, keyTextBox.Text, IsType835Checked, IsType837Checked));
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

        generateKeyButton.IsEnabled = false;
    }
    
    #endregion
}