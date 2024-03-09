using System.Security.Cryptography;
using System.Text;

namespace Scrubber;

public partial class File835Page : ContentPage
{
    private List<string> selectedColumns = new List<string>();
    private byte[] EncryptedBytes;
    private bool isEncrypted = false;
    private byte[] key;
    private byte[] iv;
    public File835Page(string SelectedFile, byte[] encryptedBytes, string SelectedFolderPath)
	{
		InitializeComponent();
        key = GenerateRandomKey();
        iv = GenerateRandomIV();
        Selectedfile.Text = SelectedFile;
        Selectedlocation.Text = SelectedFolderPath;
        EncryptedBytes = encryptedBytes;
    }

    //Generate KEY
    private byte[] GenerateRandomKey()
    {
        byte[] key = new byte[32]; // 256 bits
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }
        return key;
    }
    private byte[] GenerateRandomIV()
    {
        byte[] iv = new byte[16]; // 128 bits
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }

    //Encrypt file
    private async void Encrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Length > 0)
            {
                EncryptedBytes = EncryptFile(EncryptedBytes);
                isEncrypted = true;

                await DisplayAlert("Info", "File Encrypted", "Ok");

            }
            else
            {
                await DisplayAlert("Info", "No file selected to encrypt", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }
    private byte[] EncryptFile(byte[] fileBytes)
    {
        try
        {
            // Split the file content into lines
            string fileContent = Encoding.UTF8.GetString(fileBytes);
            string[] lines = fileContent.Split('\n');

            // Encrypt each line independently
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(':');

                // Use a StringBuilder to build the encrypted line
                StringBuilder encryptedLine = new StringBuilder(parts[0]); // Append the first part (before the first ':')

                // Check if the column is in the list of selected columns
                if (selectedColumns.Contains(parts[0].Trim()))
                {
                    // Ensure that there are at least two parts for each column
                    if (parts.Length >= 2)
                    {
                        // Extract the text after ':' and encrypt it
                        string textToEncrypt = parts[1];
                        byte[] encryptedTextBytes = EncryptText(Encoding.UTF8.GetBytes(textToEncrypt));

                        // Append the encrypted text to the StringBuilder
                        encryptedLine.Append(':').Append(Convert.ToBase64String(encryptedTextBytes));
                    }
                }
                else
                {
                    // Append the unchanged part to the StringBuilder
                    encryptedLine.Append(':').Append(parts[1]);
                }

                // Join the parts back into a single string
                lines[i] = encryptedLine.ToString();
            }

            // Join the lines back into a single string
            string encryptedContent = string.Join("\n", lines);

            return Encoding.UTF8.GetBytes(encryptedContent);
        }
        catch (Exception ex)
        {
            // Log or output the error details
            Console.WriteLine("Error during file encryption: " + ex.Message);
            return null;
        }
    }
    private byte[] EncryptText(byte[] textBytes)
    {
        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Padding = PaddingMode.ISO10126; // Use ZeroPadding

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(textBytes, 0, textBytes.Length);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during text encryption: " + ex.Message);
            return null;
        }
    }

    // Decrypt file
    private async void Decrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Length > 0)
            {
                EncryptedBytes = DecryptFile(EncryptedBytes);
                isEncrypted = false;

                await Shell.Current.DisplayAlert("Info", "File Decrypted", "Ok");
            }
            else
            {
                await Shell.Current.DisplayAlert("Info", "No file selected to decrypt", "Ok");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "Ok");
        }
    }
    private byte[] DecryptFile(byte[] encryptedBytes)
    {
        try
        {
            // Convert the encrypted content to string
            string encryptedContent = Encoding.UTF8.GetString(encryptedBytes);

            // Split the file content into lines
            string[] lines = encryptedContent.Split('\n');

            // Decrypt each line independently
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(':');

                // Use a StringBuilder to build the decrypted line
                StringBuilder decryptedLine = new StringBuilder(parts[0]); // Append the first part (before the first ':')

                // Check if the column is in the list of selected columns
                if (selectedColumns.Contains(parts[0].Trim()))
                {
                    // Ensure that there are at least two parts for each column
                    if (parts.Length >= 2)
                    {
                        // Extract the encrypted text after ':' and decrypt it
                        string encryptedText = parts[1];
                        byte[] decryptedTextBytes = DecryptText(Convert.FromBase64String(encryptedText));

                        // Append the decrypted text to the StringBuilder
                        decryptedLine.Append(':').Append(Encoding.UTF8.GetString(decryptedTextBytes));
                    }
                }
                else
                {
                    // Append the unchanged part to the StringBuilder
                    decryptedLine.Append(':').Append(parts[1]);
                }

                // Join the parts back into a single string
                lines[i] = decryptedLine.ToString();
            }

            // Join the lines back into a single string
            string decryptedContent = string.Join("\n", lines);

            return Encoding.UTF8.GetBytes(decryptedContent);
        }
        catch (Exception ex)
        {
            // Log or output the error details
            Console.WriteLine("Error during file decryption: " + ex.Message);
            return null;
        }
    }
    private byte[] DecryptText(byte[] encryptedTextBytes)
    {
        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Padding = PaddingMode.ISO10126; // Use ZeroPadding

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);
                        csDecrypt.FlushFinalBlock();
                    }

                    return msDecrypt.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during text decryption: " + ex.Message);
            return null;
        }
    }

    //Download file
    private async void Download_Clicked(object sender, EventArgs e)
    {
        try
        {
            byte[] fileBytesForDownload = null;

            if (isEncrypted)
            {
                fileBytesForDownload = EncryptedBytes;
            }
            else if (EncryptedBytes != null)
            {
                fileBytesForDownload = new byte[EncryptedBytes.Length];
                Array.Copy(EncryptedBytes, fileBytesForDownload, EncryptedBytes.Length);
            }

            if (fileBytesForDownload != null && fileBytesForDownload.Length > 0)
            {
                if (!(string.IsNullOrWhiteSpace(Selectedfile.Text)) && !(string.IsNullOrWhiteSpace(Selectedlocation.Text)))
                {
                    var fileName = string.IsNullOrWhiteSpace(Selectedfile.Text) ? "defaultFileName" : Selectedfile.Text;
                    var filePath = Path.Combine(Selectedlocation.Text, fileName);

                    File.WriteAllBytes(filePath, fileBytesForDownload);

                    await DisplayAlert("Info", "File downloaded to " + filePath, "Ok");
                }
                else
                {
                    await DisplayAlert("Info", "No file path available", "Ok");
                }
            }
            else
            {
                await DisplayAlert("Info", "No file to download", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }

    //Seleted checkbox
    void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        CheckBox checkbox = (CheckBox)sender;

        string columnName = checkbox.AutomationId;

        if (checkbox.IsChecked)
        {
            if (!selectedColumns.Contains(columnName))
            {
                selectedColumns.Add(columnName);
            }
        }
        else
        {
            selectedColumns.Remove(columnName);
        }
    }
    private void SelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        List<CheckBox> checkboxes835 = new List<CheckBox>
        {
            name835, policyId835, subscriberName835, subscriberPolicyId835,payerName835,
            payerAddress835, medicalRecordNo835,correctedInsuredName835,
            correctedInsuredPolicyId835,renderingProviderName835,
            renderingProviderPolicyId835,serviceLineItemControlNo835
        };

        foreach (var checkbox in checkboxes835)
        {
            checkbox.IsChecked = ((CheckBox)sender).IsChecked;
        }
    }
    private void ClearAll_Clicked(object sender, EventArgs e)
    {
        List<CheckBox> checkboxes = new List<CheckBox>
        {
            selectAll,
            name835, policyId835, subscriberName835, subscriberPolicyId835,payerName835, 
            payerAddress835, medicalRecordNo835,correctedInsuredName835, 
            correctedInsuredPolicyId835,renderingProviderName835, 
            renderingProviderPolicyId835,serviceLineItemControlNo835
        };

        foreach (var checkbox in checkboxes)
        {
            checkbox.IsChecked = false;
        }
    }
}