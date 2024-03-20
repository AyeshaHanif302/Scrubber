namespace Scrubber;

public partial class ICD_10Page : ContentPage
{
    private byte[] EncryptedBytes;
    private string SelectedFile;
    private string SelectedFolderPath;
    private string KeyString;
    private bool IsType835Checked;
    private bool IsType837Checked;
    public ICD_10Page(string selectedFile, byte[] encryptedBytes, string selectedFolderPath, string keyString, bool is838, bool is837)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
        KeyString = keyString;
        IsType835Checked = is838;
        IsType837Checked = is837;
    }

    private async void Next_Clicked(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedBytes, SelectedFolderPath, KeyString));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedBytes, SelectedFolderPath, KeyString, TxtHIICD10Codes.Text));
        }
        else
        {
            await DisplayAlert("Error", "Please select a radio button.", "OK");
        }
    }
}