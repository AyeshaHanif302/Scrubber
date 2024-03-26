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
    public ICD_10Page(List<string> selectedFile, List<byte[]> encryptedContent, string selectedFolderPath, bool is838, bool is837, bool isboth)
	{
		InitializeComponent();
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedContent = encryptedContent;
        IsType835Checked = is838;
        IsType837Checked = is837;
        IsTypebothChecked = isboth;
    }

    #region Navigation
    private async void Next_Clicked(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedContent, SelectedFolderPath));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedContent, SelectedFolderPath, TxtHIICD10Codes.Text));
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

    private async void Location_Clicked(object sender, EventArgs e)
    {
        int pagesToPop = 2;

        for (int counter = 1; counter < pagesToPop; counter++)
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
            }
            else
            {
                break;
            }
        }
        await Navigation.PopAsync();
    }
    private async void Encryption_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async void Files_Clicked(object sender, EventArgs e)
    {
        if (IsType835Checked)
        {
            await Navigation.PushAsync(new File835Page(SelectedFile, EncryptedContent, SelectedFolderPath));
        }
        else if (IsType837Checked)
        {
            await Navigation.PushAsync(new File837Page(SelectedFile, EncryptedContent, SelectedFolderPath, TxtHIICD10Codes.Text));
        }
        else if (IsTypebothChecked)
        {
            await Navigation.PushAsync(new ElementsPage(SelectedFile, EncryptedContent, SelectedFolderPath, KeyString, TxtHIICD10Codes.Text));
        }
    }
   
    #endregion
}