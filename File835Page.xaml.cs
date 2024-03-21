using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubber;

public partial class File835Page : ContentPage
{
    private List<string> selectedColumns = new List<string>();
    private List<byte[]> EncryptedBytes;
    private bool isEncrypted = false;
    private byte[] key;
    private List<string> SelectedFiles;
    private string SelectedFolderPath;
    private List<byte[]> EncryptedFiles;

    public File835Page(List<string> selectedFile, List<byte[]> encryptedBytes, string selectedFolderPath, string keyString)
	{
		InitializeComponent();
        key = Convert.FromBase64String(keyString);
        SelectedFiles = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
        EncryptedFiles = new List<byte[]>();
    }

    #region Encryption
    private async void Encrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Count > 0)
            {
                EncryptedFiles.Clear();

                foreach (var fileBytes in EncryptedBytes)
                {
                     var encryptedFileBytes = await EncryptFiles(fileBytes);
                    EncryptedFiles.Add(encryptedFileBytes);
                }

                isEncrypted = true;
                await DisplayAlert("Info", "Files Encrypted", "Ok");
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
    private string EncryptText(string value)
    {
        // Get bytes of plaintext string
        byte[] plainBytes = Encoding.UTF8.GetBytes(value);

        // Get parameter sizes
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;
        int cipherSize = plainBytes.Length;

        // We write everything into one big array for easier encoding
        int encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;
        Span<byte> encryptedData = encryptedDataLength < 1024
                                  ? stackalloc byte[encryptedDataLength]
                                  : new byte[encryptedDataLength].AsSpan();

        // Copy parameters
        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        // Encrypt
        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Encode for transmission
        return Convert.ToBase64String(encryptedData);
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    
                                                    
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                       newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
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

    #endregion

    #region Decryption
    private async void Decrypt_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (EncryptedBytes != null && EncryptedBytes.Count > 0)
            {
                EncryptedFiles.Clear();

                foreach (var fileBytes in EncryptedBytes)
                {
                    var encryptedFileBytes = await DecryptFiles(fileBytes);
                    EncryptedFiles.Add(encryptedFileBytes);
                }

                isEncrypted = false;

                await DisplayAlert("Info", "Files Decrypted", "Ok");
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
    private async Task<byte[]> DecryptFiles(byte[] encryptedBytes)
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && correctedInsuredPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId835.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
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
    private string DecryptText(string encryptedBase64)
    {
        try
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedBase64);

            // Extract parameters
            int nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.AsSpan(0, 4));
            int tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.AsSpan(4 + nonceSize, 4));
            byte[] nonce = encryptedData.AsSpan(4, nonceSize).ToArray();
            byte[] tag = encryptedData.AsSpan(4 + nonceSize + 4, tagSize).ToArray();
            byte[] cipherBytes = encryptedData.AsSpan(4 + nonceSize + 4 + tagSize).ToArray();

            // Decrypt
            using var aes = new AesGcm(key);
            byte[] decryptedBytes = new byte[cipherBytes.Length];
            aes.Decrypt(nonce, cipherBytes, tag, decryptedBytes);

            // Convert decrypted bytes to string
            string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

            return decryptedText;
        }
        catch (FormatException ex)
        {
            // Handle invalid Base64 string
            Console.WriteLine("Error: The input is not a valid Base64 string.");
            Console.WriteLine(ex.Message);
            return null; // or throw an exception or handle the error according to your application's logic
        }
    }

    #endregion

    #region Download
    private async void Download_Clicked(object sender, EventArgs e)
    {
        try
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Convert.ToBase64String(key));

            if (isEncrypted)
            {
                if (EncryptedFiles != null && EncryptedFiles.Count > 0)
                {
                    await DownloadEncryptedFiles(EncryptedFiles, keyBytes);
                }
                else
                {
                    await DisplayAlert("Info", "No file to download", "Ok");
                }
            }
            else if (!isEncrypted)
            {
                 if (EncryptedFiles != null && EncryptedFiles.Count > 0)
                 {
                     await DownloadDecryptedFiles(EncryptedFiles);
                 }
                 else
                 {
                     await DisplayAlert("Info", "No file to download", "Ok");
                 }
            }
            else
            {
                await DisplayAlert("Info", "No file available for download", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }
    private async Task DownloadEncryptedFiles(List<byte[]> files, byte[] keyBytes)
    {
        try
        {
            if (SelectedFiles != null && SelectedFiles.Count > 0 && !(string.IsNullOrWhiteSpace(SelectedFolderPath)))
            {
                var folderName = "DownloadedFiles_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
                var folderPath = System.IO.Path.Combine(SelectedFolderPath, folderName);

                Directory.CreateDirectory(folderPath);

                var keyFilePath = System.IO.Path.Combine(folderPath, "encryption_key.txt");
                File.WriteAllBytes(keyFilePath, keyBytes);

                for (int i = 0; i < files.Count; i++)
                {
                    var selectedFile = SelectedFiles[i];
                    var fileName = string.IsNullOrWhiteSpace(selectedFile) ? "defaultFileName" : selectedFile;
                    var filePath = System.IO.Path.Combine(folderPath, fileName);

                    File.WriteAllBytes(filePath, files[i]);
                }

                await DisplayAlert("Info", "Files downloaded to " + folderPath, "Ok");
            }
            else
            {
                await DisplayAlert("Info", "No file path available", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }
    private async Task DownloadDecryptedFiles(List<byte[]> files)
    {
        try
        {
            if (SelectedFiles != null && SelectedFiles.Count > 0 && !string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                var folderName = "DecryptedFiles_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
                var folderPath = System.IO.Path.Combine(SelectedFolderPath, folderName);

                Directory.CreateDirectory(folderPath);

                for (int i = 0; i < files.Count; i++)
                {
                    var selectedFile = SelectedFiles[i];
                    var fileName = string.IsNullOrWhiteSpace(selectedFile) ? "defaultFileName" : selectedFile;
                    var filePath = System.IO.Path.Combine(folderPath, fileName);

                    File.WriteAllBytes(filePath, files[i]);
                }

                await DisplayAlert("Info", "files downloaded to " + folderPath, "Ok");
            }
            else
            {
                await DisplayAlert("Info", "No file path available", "Ok");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Ok");
        }
    }

    #endregion

    #region Checkbox Event Handlers
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

    #endregion

}