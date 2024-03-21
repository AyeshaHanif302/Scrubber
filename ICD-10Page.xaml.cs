namespace Scrubber;

public partial class ICD_10Page : ContentPage
{
    private List<byte[]> EncryptedContent;
    private List<string> SelectedFile;
    private string SelectedFolderPath;
    private string KeyString;
    private bool IsType835Checked;
    private bool IsType837Checked;
    private bool IsTypebothChecked;
    public ICD_10Page(List<string> selectedFile, List<byte[]> encryptedContent, string selectedFolderPath, string keyString, bool is838, bool is837, bool isboth)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedContent = encryptedContent;
        KeyString = keyString;
        IsType835Checked = is838;
        IsType837Checked = is837;
        IsTypebothChecked = isboth;
    }

    private async void Next_Clicked(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedContent, SelectedFolderPath, KeyString));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedContent, SelectedFolderPath, KeyString, TxtHIICD10Codes.Text));
        }
        else if (IsTypebothChecked)
        {
            await Navigation.PushAsync(new ElementsPage(SelectedFile, EncryptedContent, SelectedFolderPath, KeyString, TxtHIICD10Codes.Text));
        }
        else
        {
            await DisplayAlert("Error", "Please select a radio button.", "OK");
        }
    }
}