using Microsoft.Maui.Controls.Shapes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubber;

public partial class File835Page : ContentPage
{
    private List<string> selectedColumns = new List<string>();
    private byte[] EncryptedBytes;
    private bool isEncrypted = false;
    private byte[] key;
    private byte[] iv;
    public File835Page(string SelectedFile, byte[] encryptedBytes, string SelectedFolderPath, string keyString, string ivString)
	{
		InitializeComponent();
        key = ParseHexString(keyString);
        iv = ParseHexString(ivString);
        Selectedfile.Text = SelectedFile;
        Selectedlocation.Text = SelectedFolderPath;
        EncryptedBytes = encryptedBytes;
    }

    // Method to parse hex string into byte array
    private byte[] ParseHexString(string hexString)
    {
        hexString = hexString.Replace("-", ""); // Remove dashes if present
        int length = hexString.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }

    //Encrypt file
    private async void Encrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Length > 0)
            {
                EncryptedBytes = await EncryptFiles(EncryptedBytes);
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
    private byte[] EncryptText(byte[] textBytes)
    {
        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Padding = PaddingMode.ISO10126;

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

    private async Task<byte[]> EncryptFiles(byte[] fileBytes)
    {
        try
        {
            string fileContent = Encoding.UTF8.GetString(fileBytes);
            var message = string.Empty;
            var finalLines = new List<string>();
            var processedFiles = new List<byte[]>();

            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var currentLine in lines)
            {
                await Task.Delay(50);
                finalLines.Clear();

                //var fileData = await File.ReadAllTextAsync($"{Selectedfile}\\{currentLine}");

                var fileData = fileContent;

                fileData = fileData.Replace("\r", string.Empty).Replace("\n", string.Empty);

                if (fileData.Length > 105)
                {
                    var records = new List<string>();

                    var elSeparator = fileData.Substring(3, 1); // Element Separator
                    var cpSeparator = fileData.Substring(104, 1);// Component Separator
                    var sgSeparator = fileData.Substring(105, 1); //Segment Separator

                    foreach (Match m in Regex.Matches(fileData, $"ST\\{elSeparator}835(.*?)\\{sgSeparator}SE\\{elSeparator}(.*?)\\{sgSeparator}"))
                    {
                        records.Add(m.Groups[0].Value);
                    }

                    var fileType = string.Empty;

                    //835 file
                    if (fileData.Contains($"ST{elSeparator}835{elSeparator}"))
                        fileType = "835";

                    if (records.Count > 0 && !string.IsNullOrWhiteSpace(fileType))
                    {
                        var allLines = fileData.Split(sgSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();

                        var newLine = string.Empty;

                        finalLines.Add($"{allLines[0]}{sgSeparator}");
                        finalLines.Add($"{allLines[1]}{sgSeparator}");

                        foreach (var record in records)
                        {
                            var recordLines = record.Split(sgSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
                            var loopId = string.Empty;

                            if (fileType == "835")
                            {
                                foreach (var line in recordLines)
                                {
                                    var values = line.Split(elSeparator).ToList();

                                    if (line.StartsWith($"N1{elSeparator}") && line.Contains($"{elSeparator}PR{elSeparator}"))
                                    {
                                        loopId = "1000A";

                                        if (payerName835.IsChecked)
                                        {
                                            newLine = string.Empty;

                                            foreach (var item in values.Select((value, index) => new { value = value, index }))
                                            {                                               
                                                switch (item.index)
                                                {
                                                    case 0:
                                                        newLine = item.value;
                                                        break;
                                                    case 2:
                                                        byte[] valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        byte[] encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                        break;
                                                    default:
                                                        newLine += $"{elSeparator}{item.value}";
                                                        break;
                                                }
                                            }

                                            finalLines.Add($"{newLine}{sgSeparator}");
                                        }
                                        else
                                            finalLines.Add($"{line}{sgSeparator}");

                                        //string combinedLines = string.Join(Environment.NewLine, finalLines);
                                        //return Encoding.UTF8.GetBytes(combinedLines);
                                    }
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "1000A" && payerAddress835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    byte[] valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                    byte[] encryptedBytes = EncryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "1000A" && payerAddress835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                case 2:
                                                case 3:
                                                case 4:
                                                case 7:
                                                    byte[] valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                    byte[] encryptedBytes = EncryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"CLP{elSeparator}"))
                                    {
                                        loopId = "2100";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}QC{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] encryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && patientname835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;

                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}IL{elSeparator}") && loopId == "2100" )
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] encryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && subscriberName835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;

                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}74{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] encryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredName835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}82{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] encryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderName835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                        encryptedBytes = EncryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}EA{elSeparator}") && loopId == "2100" && medicalRecordNo835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 2:
                                                    byte[] valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                    byte[] encryptedBytes = EncryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"SVC{elSeparator}"))
                                    {
                                        loopId = "2110";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}6R{elSeparator}") && loopId == "2110" && serviceLineItemControlNo835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 2:
                                                    byte[] valueBytes = Encoding.UTF8.GetBytes(item.value);
                                                    byte[] encryptedBytes = EncryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Convert.ToBase64String(encryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else
                                        finalLines.Add($"{line}{sgSeparator}");
                                }
                            }
                        }

                        finalLines.Add($"{allLines[allLines.Count - 2]}{sgSeparator}");
                        finalLines.Add($"{allLines[allLines.Count - 1]}{sgSeparator}");
                    }
                    else
                        message = message + $"File {currentLine} have inavlid/unsupported data.\n";
                }
                else
                    message = message + $"File {currentLine} has invalid length.\n";

                if (finalLines.Count > 0)
                {
                    // Write encrypted content to file
                    string combinedLines = string.Join(Environment.NewLine, finalLines);
                    byte[] combinedBytesToFile = Encoding.UTF8.GetBytes(combinedLines);

                    // Return the combined content of all processed files
                    return combinedBytesToFile;
                }

            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during file encryption: " + ex.Message);
            return null;
        }
    }

    //Decrypt file
    private async void Decrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Length > 0)
            {
                EncryptedBytes = await DecryptFile(EncryptedBytes);
                isEncrypted = false;

                await DisplayAlert("Info", "File Decrypted", "Ok");

            }
            else
            {
                await DisplayAlert("Info", "No file selected to decrypt", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }
    private async Task<byte[]> DecryptFile(byte[] encryptedBytes)
    {
        try
        {
            string fileContent = Encoding.UTF8.GetString(encryptedBytes);
            var message = string.Empty;
            var finalLines = new List<string>();
            var processedFiles = new List<byte[]>();

            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var currentLine in lines)
            {
                await Task.Delay(50);
                finalLines.Clear();

                //var fileData = await File.ReadAllTextAsync($"{Selectedfile}\\{currentLine}");

                var fileData = fileContent;

                fileData = fileData.Replace("\r", string.Empty).Replace("\n", string.Empty);

                if (fileData.Length > 105)
                {
                    var records = new List<string>();

                    var elSeparator = fileData.Substring(3, 1); // Element Separator
                    var cpSeparator = fileData.Substring(104, 1);// Component Separator
                    var sgSeparator = fileData.Substring(105, 1); //Segment Separator

                    foreach (Match m in Regex.Matches(fileData, $"ST\\{elSeparator}835(.*?)\\{sgSeparator}SE\\{elSeparator}(.*?)\\{sgSeparator}"))
                    {
                        records.Add(m.Groups[0].Value);
                    }

                    var fileType = string.Empty;

                    //835 file
                    if (fileData.Contains($"ST{elSeparator}835{elSeparator}"))
                        fileType = "835";

                    if (records.Count > 0 && !string.IsNullOrWhiteSpace(fileType))
                    {
                        var allLines = fileData.Split(sgSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();

                        var newLine = string.Empty;

                        finalLines.Add($"{allLines[0]}{sgSeparator}");
                        finalLines.Add($"{allLines[1]}{sgSeparator}");

                        foreach (var record in records)
                        {
                            var recordLines = record.Split(sgSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
                            var loopId = string.Empty;

                            if (fileType == "835")
                            {
                                foreach (var line in recordLines)
                                {
                                    var values = line.Split(elSeparator).ToList();

                                    if (line.StartsWith($"N1{elSeparator}") && line.Contains($"{elSeparator}PR{elSeparator}"))
                                    {
                                        loopId = "1000A";

                                        if (payerName835.IsChecked)
                                        {
                                            newLine = string.Empty;

                                            foreach (var item in values.Select((value, index) => new { value = value, index }))
                                            {
                                                switch (item.index)
                                                {
                                                    case 0:
                                                        newLine = item.value;
                                                        break;
                                                    case 2:
                                                        byte[] valueBytes = Convert.FromBase64String(item.value); 
                                                        byte[] decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                        break;
                                                    default:
                                                        newLine += $"{elSeparator}{item.value}";
                                                        break;
                                                }
                                            }

                                            finalLines.Add($"{newLine}{sgSeparator}");
                                        }
                                        else
                                            finalLines.Add($"{line}{sgSeparator}");

                                        //string combinedLines = string.Join(Environment.NewLine, finalLines);
                                        //return Encoding.UTF8.GetBytes(combinedLines);
                                    }
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "1000A" && payerAddress835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    byte[] valueBytes = Convert.FromBase64String(item.value);
                                                    byte[] decryptedBytes = DecryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "1000A" && payerAddress835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                case 2:
                                                case 3:
                                                case 4:
                                                case 7:
                                                    byte[] valueBytes = Convert.FromBase64String(item.value);
                                                    byte[] decryptedBytes = DecryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"CLP{elSeparator}"))
                                    {
                                        loopId = "2100";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}QC{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] decryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && patientname835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;

                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}IL{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] decryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && subscriberName835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;

                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}74{elSeparator}") && loopId == "2100" )
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] decryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredName835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}82{elSeparator}") && loopId == "2100")
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            byte[] valueBytes = null;
                                            byte[] decryptedBytes = null;

                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 3:
                                                case 4:
                                                case 5:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderName835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId835.IsChecked)
                                                    {
                                                        valueBytes = Convert.FromBase64String(item.value);
                                                        decryptedBytes = DecryptText(valueBytes);
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}EA{elSeparator}") && loopId == "2100" && medicalRecordNo835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 2:
                                                    byte[] valueBytes = Convert.FromBase64String(item.value);
                                                    byte[] decryptedBytes = DecryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"SVC{elSeparator}"))
                                    {
                                        loopId = "2110";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}6R{elSeparator}") && loopId == "2110" && serviceLineItemControlNo835.IsChecked)
                                    {
                                        newLine = string.Empty;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 2:
                                                    byte[] valueBytes = Convert.FromBase64String(item.value);
                                                    byte[] decryptedBytes = DecryptText(valueBytes);
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{Encoding.UTF8.GetString(decryptedBytes)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                default:
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else
                                        finalLines.Add($"{line}{sgSeparator}");
                                }
                            }
                        }

                        finalLines.Add($"{allLines[allLines.Count - 2]}{sgSeparator}");
                        finalLines.Add($"{allLines[allLines.Count - 1]}{sgSeparator}");
                    }
                    else
                        message = message + $"File {currentLine} have inavlid/unsupported data.\n";
                }
                else
                    message = message + $"File {currentLine} has invalid length.\n";

                if (finalLines.Count > 0)
                {
                    // Write encrypted content to file
                    string combinedLines = string.Join(Environment.NewLine, finalLines);
                    byte[] combinedBytesToFile = Encoding.UTF8.GetBytes(combinedLines);

                    // Return the combined content of all processed files
                    return combinedBytesToFile;
                }

            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during file encryption: " + ex.Message);
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
                aesAlg.Padding = PaddingMode.ISO10126;

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
                    var filePath = System.IO.Path.Combine(Selectedlocation.Text, fileName);

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
            patientname835, patientpolicyId835, subscriberName835, subscriberPolicyId835,payerName835,
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
            patientname835, patientpolicyId835, subscriberName835, subscriberPolicyId835,payerName835, 
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