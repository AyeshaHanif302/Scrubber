using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubber;

public partial class File837Page : ContentPage
{
    private List<string> selectedColumns = new List<string>();
    private List<byte[]> EncryptedBytes;
    private bool isEncrypted = false;
    private byte[] key;
    private List<string> SelectedFiles;
    private string SelectedFolderPath;
    private string ICD10Codes;
    private List<byte[]> EncryptedFiles;

    public File837Page(List<string> selectedFiles, List<byte[]> encryptedBytes, string selectedFolderPath, string ICD10codes)
	{
		InitializeComponent();
        key = Convert.FromBase64String(GlobalVariables.EncryptionKey);
        SelectedFiles = selectedFiles;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
        ICD10Codes = ICD10codes;
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

                int skippedFilesCount = 0;

                foreach (var fileBytes in EncryptedBytes)
                {
                    if (IsAlreadyEncrypted(fileBytes))
                    {
                        skippedFilesCount++;
                        continue;
                    }

                    var encryptedFileBytes = await EncryptFiles(fileBytes);
                    if (encryptedFileBytes != null)
                    {
                        EncryptedFiles.Add(encryptedFileBytes);
                    }
                    else
                    {
                        skippedFilesCount++;
                    }
                }

                if (skippedFilesCount > 0)
                {
                    await DisplayAlert("Info", $"{skippedFilesCount} file(s) already encrypted and skipped.", "Ok");
                }
                else
                {
                    isEncrypted = true;
                    await DisplayAlert("Info", "Files Encrypted", "Ok");
                }

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

                    foreach (Match m in Regex.Matches(fileData, $"ST\\{elSeparator}837(.*?)\\{sgSeparator}SE\\{elSeparator}(.*?)\\{sgSeparator}"))
                        records.Add(m.Groups[0].Value);

                    var fileType = string.Empty;

                    //837 file
                    if (fileData.Contains($"ST{elSeparator}837{elSeparator}"))
                        fileType = "837";

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

                            if (fileType == "837")
                            {
                                foreach (var line in recordLines)
                                {
                                    var values = line.Split(elSeparator).ToList();

                                    if (line.StartsWith($"HL{elSeparator}") && line.Contains($"{elSeparator}22{elSeparator}"))
                                    {
                                        loopId = "2000B";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}IL{elSeparator}") )
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    break;                                                   
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"DMG{elSeparator}") && loopId == "2000B" && subscriberName837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberName837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2000B" && subscriberAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2000B" && subscriberAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberAddress837.IsChecked)
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
                                    else if (line.StartsWith("REF") && line.Contains($"{elSeparator}SY{elSeparator}") && loopId == "2000B" && patientpolicyId837.IsChecked)
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
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                                case 2:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}PR{elSeparator}"))
                                    {
                                        loopId = "2010BB";

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
                                                    if (item.value != null && item.value.Length > 0 && payerName837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2010BB" && payerAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && payerAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2010BB" && payerAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && payerAddress837.IsChecked)
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
                                    else if (line.StartsWith($"PAT{elSeparator}") || (line.StartsWith($"HL{elSeparator}") && line.Contains($"{elSeparator}23{elSeparator}")))
                                    {
                                        loopId = "2000C";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && loopId == "2000C" && patientname837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientname837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"DMG{elSeparator}") && loopId == "2000C" && patientname837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientname837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2000C" && patientaddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientaddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2000C" && patientaddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientaddress837.IsChecked)
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
                                    else if (line.StartsWith($"CLM{elSeparator}"))
                                    {
                                        loopId = "2300";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABK{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
                                    {
                                        newLine = string.Empty;

                                        var isABK = false;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();

                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABK")
                                                                            isABK = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABK)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                default:
                                                                    newLine += $"{cpSeparator}{cpitem.value}";
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABJ{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
                                    {
                                        newLine = string.Empty;

                                        var isABJ = false;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();

                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABJ")
                                                                            isABJ = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABJ)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABF{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
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
                                                case 5:
                                                case 6:
                                                case 7:
                                                case 8:
                                                case 9:
                                                case 10:
                                                case 11:
                                                case 12:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();
                                                        var isABF = false;
                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABF")
                                                                            isABF = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABF)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("REF") && line.Contains($"{elSeparator}EA{elSeparator}") && loopId == "2300" && medicalRecordNo837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && medicalRecordNo837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}71{elSeparator}") && loopId == "2300" && attendingProviderName837.IsChecked)
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
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && attendingProviderName837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}82{elSeparator}") && loopId == "2300" && renderingProviderName837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderName837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}77{elSeparator}") && loopId == "2300" && attendingProviderName837.IsChecked)
                                    {
                                        loopId = "2310E";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2310E" && serviceFacilityAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceFacilityAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{EncryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2310E" && serviceFacilityAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceFacilityAddress837.IsChecked)
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
                                    else if (line.StartsWith($"SV2{elSeparator}"))
                                    {
                                        loopId = "2400";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}6R{elSeparator}") && loopId == "2400" && serviceLineItemControlNo837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceLineItemControlNo837.IsChecked)
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
                    string combinedLines = string.Join(Environment.NewLine, finalLines);
                    byte[] combinedBytesToFile = Encoding.UTF8.GetBytes($"ENCRYPTED{Environment.NewLine}{combinedLines}");

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
    private string EncodeText(string text)
    {
        var charArray = text.ToCharArray();

        text = string.Empty;

        foreach (var ch in charArray)
        {
            switch (ch)
            {
                case 'a':
                    {
                        text += 'z';
                    }
                    break;
                case 'A':
                    {
                        text += 'Z';
                    }
                    break;
                case 'b':
                    {
                        text += 'y';
                    }
                    break;
                case 'B':
                    {
                        text += 'Y';
                    }
                    break;
                case 'c':
                    {
                        text += 'x';
                    }
                    break;
                case 'C':
                    {
                        text += 'X';
                    }
                    break;
                case 'd':
                    {
                        text += 'w';
                    }
                    break;
                case 'D':
                    {
                        text += 'W';
                    }
                    break;
                case 'e':
                    {
                        text += 'v';
                    }
                    break;
                case 'E':
                    {
                        text += 'V';
                    }
                    break;
                case 'f':
                    {
                        text += 'u';
                    }
                    break;
                case 'F':
                    {
                        text += 'U';
                    }
                    break;
                case 'g':
                    {
                        text += 't';
                    }
                    break;
                case 'G':
                    {
                        text += 'T';
                    }
                    break;
                case 'h':
                    {
                        text += 's';
                    }
                    break;
                case 'H':
                    {
                        text += 'S';
                    }
                    break;
                case 'i':
                    {
                        text += 'r';
                    }
                    break;
                case 'I':
                    {
                        text += 'R';
                    }
                    break;
                case 'j':
                    {
                        text += 'q';
                    }
                    break;
                case 'J':
                    {
                        text += 'Q';
                    }
                    break;
                case 'k':
                    {
                        text += 'p';
                    }
                    break;
                case 'K':
                    {
                        text += 'P';
                    }
                    break;
                case 'l':
                    {
                        text += 'o';
                    }
                    break;
                case 'L':
                    {
                        text += 'O';
                    }
                    break;
                case 'm':
                    {
                        text += 'n';
                    }
                    break;
                case 'M':
                    {
                        text += 'N';
                    }
                    break;
                case '0':
                    {
                        text += '9';
                    }
                    break;
                case '1':
                    {
                        text += '8';
                    }
                    break;
                case '2':
                    {
                        text += '7';
                    }
                    break;
                case '3':
                    {
                        text += '6';
                    }
                    break;
                case '4':
                    {
                        text += '5';
                    }
                    break;

                case 'z':
                    {
                        text += 'a';
                    }
                    break;
                case 'Z':
                    {
                        text += 'A';
                    }
                    break;
                case 'y':
                    {
                        text += 'b';
                    }
                    break;
                case 'Y':
                    {
                        text += 'B';
                    }
                    break;
                case 'x':
                    {
                        text += 'c';
                    }
                    break;
                case 'X':
                    {
                        text += 'C';
                    }
                    break;
                case 'w':
                    {
                        text += 'd';
                    }
                    break;
                case 'W':
                    {
                        text += 'D';
                    }
                    break;
                case 'v':
                    {
                        text += 'e';
                    }
                    break;
                case 'V':
                    {
                        text += 'E';
                    }
                    break;
                case 'u':
                    {
                        text += 'f';
                    }
                    break;
                case 'U':
                    {
                        text += 'F';
                    }
                    break;
                case 't':
                    {
                        text += 'g';
                    }
                    break;
                case 'T':
                    {
                        text += 'g';
                    }
                    break;
                case 's':
                    {
                        text += 'h';
                    }
                    break;
                case 'S':
                    {
                        text += 'H';
                    }
                    break;
                case 'r':
                    {
                        text += 'i';
                    }
                    break;
                case 'R':
                    {
                        text += 'I';
                    }
                    break;
                case 'q':
                    {
                        text += 'j';
                    }
                    break;
                case 'Q':
                    {
                        text += 'J';
                    }
                    break;
                case 'p':
                    {
                        text += 'k';
                    }
                    break;
                case 'P':
                    {
                        text += 'K';
                    }
                    break;
                case 'o':
                    {
                        text += 'l';
                    }
                    break;
                case 'O':
                    {
                        text += 'L';
                    }
                    break;
                case 'n':
                    {
                        text += 'm';
                    }
                    break;
                case 'N':
                    {
                        text += 'M';
                    }
                    break;
                case '9':
                    {
                        text += '0';
                    }
                    break;
                case '8':
                    {
                        text += '1';
                    }
                    break;
                case '7':
                    {
                        text += '2';
                    }
                    break;
                case '6':
                    {
                        text += '3';
                    }
                    break;
                case '5':
                    {
                        text += '4';
                    }
                    break;
                default:
                    {
                        text += ch;
                    }
                    break;
            }
        }

        var random = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        decimal index = Math.Round((decimal)text.Length / 2);

        var part1 = string.Join("", text.Substring(0, (int)index).Reverse());
        var part2 = string.Join("", text.Substring((int)index).Reverse());
        var rand1 = new string(Enumerable.Repeat(chars, 2).Select(s => s[random.Next(s.Length)]).ToArray());
        var rand2 = new string(Enumerable.Repeat(chars, 2).Select(s => s[random.Next(s.Length)]).ToArray());

        return $"{rand1}{part1}{part2}{rand2}";
    }
    private bool IsAlreadyEncrypted(byte[] fileBytes)
    {
        try
        {
            string fileContent = Encoding.UTF8.GetString(fileBytes);

            if (fileContent.StartsWith("ENCRYPTED"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while checking if file is already encrypted: " + ex.Message);
            return false;
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
                    byte[] decryptedFileBytes = await RemoveEncryptedMarker(fileBytes);

                    var decryptedContent = await DecryptFiles(decryptedFileBytes);

                    EncryptedFiles.Add(decryptedContent);
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

                var fileData = fileContent;

                fileData = fileData.Replace("\r", string.Empty).Replace("\n", string.Empty);

                if (fileData.Length > 105)
                {
                    var records = new List<string>();

                    var elSeparator = fileData.Substring(3, 1); // Element Separator
                    var cpSeparator = fileData.Substring(104, 1);// Component Separator
                    var sgSeparator = fileData.Substring(105, 1); //Segment Separator

                    foreach (Match m in Regex.Matches(fileData, $"ST\\{elSeparator}837(.*?)\\{sgSeparator}SE\\{elSeparator}(.*?)\\{sgSeparator}"))
                        records.Add(m.Groups[0].Value);

                    var fileType = string.Empty;

                    if (fileData.Contains($"ST{elSeparator}837{elSeparator}"))
                        fileType = "837";

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

                            if (fileType == "837")
                            {
                                foreach (var line in recordLines)
                                {
                                    var values = line.Split(elSeparator).ToList();

                                    if (line.StartsWith($"HL{elSeparator}") && line.Contains($"{elSeparator}22{elSeparator}"))
                                    {
                                        loopId = "2000B";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}IL{elSeparator}"))
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
                                                    newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && subscriberPolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"DMG{elSeparator}") && loopId == "2000B" && subscriberName837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberName837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2000B" && subscriberAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2000B" && subscriberAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && subscriberAddress837.IsChecked)
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
                                    else if (line.StartsWith("REF") && line.Contains($"{elSeparator}SY{elSeparator}") && loopId == "2000B" && patientpolicyId837.IsChecked)
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
                                                    newLine += $"{elSeparator}{item.value}";
                                                    break;
                                                case 2:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}PR{elSeparator}"))
                                    {
                                        loopId = "2010BB";

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
                                                    if (item.value != null && item.value.Length > 0 && payerName837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2010BB" && payerAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && payerAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2010BB" && payerAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && payerAddress837.IsChecked)
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
                                    else if (line.StartsWith($"PAT{elSeparator}") || (line.StartsWith($"HL{elSeparator}") && line.Contains($"{elSeparator}23{elSeparator}")))
                                    {
                                        loopId = "2000C";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"NM1{elSeparator}") && loopId == "2000C" && patientname837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientname837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && patientpolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"DMG{elSeparator}") && loopId == "2000C" && patientname837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientname837.IsChecked)
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
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2000C" && patientaddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientaddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2000C" && patientaddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && patientaddress837.IsChecked)
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
                                    else if (line.StartsWith($"CLM{elSeparator}"))
                                    {
                                        loopId = "2300";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABK{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
                                    {
                                        newLine = string.Empty;

                                        var isABK = false;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();

                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABK")
                                                                            isABK = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABK)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                default:
                                                                    newLine += $"{cpSeparator}{cpitem.value}";
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABJ{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
                                    {
                                        newLine = string.Empty;

                                        var isABJ = false;

                                        foreach (var item in values.Select((value, index) => new { value = value, index }))
                                        {
                                            switch (item.index)
                                            {
                                                case 0:
                                                    newLine = item.value;
                                                    break;
                                                case 1:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();

                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABJ")
                                                                            isABJ = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABJ)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABF{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(ICD10Codes))
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
                                                case 5:
                                                case 6:
                                                case 7:
                                                case 8:
                                                case 9:
                                                case 10:
                                                case 11:
                                                case 12:
                                                    {
                                                        var cpValues = item.value.Split(cpSeparator).ToList();
                                                        var isABF = false;
                                                        foreach (var cpitem in cpValues.Select((value, index) => new { value = (string.IsNullOrWhiteSpace(value) ? null : value), index }))
                                                        {
                                                            switch (cpitem.index)
                                                            {
                                                                case 0: //HI01-01 Code List Qualifier Code
                                                                    {
                                                                        if (cpitem.value == "ABF")
                                                                            isABF = true;

                                                                        newLine += $"{elSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                                case 1: //HI01-02 Industry Code
                                                                    {
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && ICD10Codes.Contains(cpitem.value.Trim()) && isABF)
                                                                            newLine += $"{cpSeparator}{EncodeText(cpitem.value)}";
                                                                        else
                                                                            newLine += $"{cpSeparator}{cpitem.value}";
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith("REF") && line.Contains($"{elSeparator}EA{elSeparator}") && loopId == "2300" && medicalRecordNo837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && medicalRecordNo837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}71{elSeparator}") && loopId == "2300" && attendingProviderName837.IsChecked)
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
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && attendingProviderName837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}82{elSeparator}") && loopId == "2300" && renderingProviderName837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderName837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                                case 9:
                                                    if (item.value != null && item.value.Length > 0 && renderingProviderPolicyId837.IsChecked)
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
                                    else if (line.StartsWith($"NM1{elSeparator}") && line.Contains($"{elSeparator}77{elSeparator}") && loopId == "2300" && attendingProviderName837.IsChecked)
                                    {
                                        loopId = "2310E";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N3{elSeparator}") && loopId == "2310E" && serviceFacilityAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceFacilityAddress837.IsChecked)
                                                    {
                                                        newLine += !string.IsNullOrWhiteSpace(item.value) ? $"{elSeparator}{DecryptText(item.value)}" : $"{elSeparator}{item.value}";
                                                    }
                                                    else
                                                    {
                                                        newLine += $"{elSeparator}{item.value}";
                                                    }
                                                    break;
                                            }
                                        }

                                        finalLines.Add($"{newLine}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"N4{elSeparator}") && loopId == "2310E" && serviceFacilityAddress837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceFacilityAddress837.IsChecked)
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
                                    else if (line.StartsWith($"SV2{elSeparator}"))
                                    {
                                        loopId = "2400";
                                        finalLines.Add($"{line}{sgSeparator}");
                                    }
                                    else if (line.StartsWith($"REF{elSeparator}") && line.Contains($"{elSeparator}6R{elSeparator}") && loopId == "2400" && serviceLineItemControlNo837.IsChecked)
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
                                                    if (item.value != null && item.value.Length > 0 && serviceLineItemControlNo837.IsChecked)
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
            Console.WriteLine(ex.Message);
            return null;
        }
    }
    private Task<byte[]> RemoveEncryptedMarker(byte[] fileBytes)
    {
        try
        {
            string fileContent = Encoding.UTF8.GetString(fileBytes);

            if (fileContent.Contains("ENCRYPTED"))
            {
                fileContent = fileContent.Replace("ENCRYPTED", "");

                byte[] modifiedFileBytes = Encoding.UTF8.GetBytes(fileContent);

                return Task.FromResult(modifiedFileBytes);
            }
            else
            {
                return Task.FromResult(fileBytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during removing encrypted marker: " + ex.Message);
            return Task.FromResult<byte[]>(null);
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
        List<CheckBox> checkboxes837 = new List<CheckBox>
        {
            patientname837, patientaddress837, patientpolicyId837, subscriberName837,
            subscriberAddress837, subscriberPolicyId837, payerName837,
            payerAddress837, medicalRecordNo837, attendingProviderName837,
            serviceFacilityAddress837, renderingProviderName837,
            renderingProviderPolicyId837, serviceLineItemControlNo837
        };

        foreach (var checkbox in checkboxes837)
        {
            checkbox.IsChecked = ((CheckBox)sender).IsChecked;
        }
    }
    private void ClearAll_Clicked(object sender, EventArgs e)
    {
        List<CheckBox> checkboxes837 = new List<CheckBox>
        {
            selectAll,
            patientname837, patientaddress837, patientpolicyId837, subscriberName837,
            subscriberAddress837, subscriberPolicyId837, payerName837,
            payerAddress837, medicalRecordNo837, attendingProviderName837,
            serviceFacilityAddress837, renderingProviderName837,
            renderingProviderPolicyId837, serviceLineItemControlNo837
        };

        foreach (var checkbox in checkboxes837)
        {
            checkbox.IsChecked = false;
        }
    }

    #endregion

    #region Navigation
    private async void Location_Tapped(object sender, EventArgs e)
    {
        int pagesToPop = 3;

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
    private async void Encryption_Tapped(object sender, EventArgs e)
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
    private async void ICD10_Tapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private void Files_Tapped(object sender, EventArgs e)
    {

    }

    #endregion
}