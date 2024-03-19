using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubber;

public partial class File837Page : ContentPage
{
    private List<string> selectedColumns = new List<string>();
    private byte[] EncryptedBytes;
    private bool isEncrypted = false;
    private byte[] key;
    private string SelectedFile;
    private string SelectedFolderPath;

    public File837Page(string selectedFile, byte[] encryptedBytes, string selectedFolderPath, string keyString)
	{
		InitializeComponent();
        key = Convert.FromBase64String(keyString);
        SelectedFile = selectedFile;
        SelectedFolderPath = selectedFolderPath;
        EncryptedBytes = encryptedBytes;
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

                    foreach (Match m in Regex.Matches(fileData, $"ST\\{elSeparator}837(.*?)\\{sgSeparator}SE\\{elSeparator}(.*?)\\{sgSeparator}"))
                        records.Add(m.Groups[0].Value);

                    var fileType = string.Empty;

                    //835 file
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
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABK{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(TxtHIICD10Codes.Text))
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
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && TxtHIICD10Codes.Text.Contains(cpitem.value.Trim()) && isABK)
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
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABJ{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(TxtHIICD10Codes.Text))
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
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && TxtHIICD10Codes.Text.Contains(cpitem.value.Trim()) && isABJ)
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
                                    else if (line.StartsWith("HI") && line.Contains($"{elSeparator}ABF{cpSeparator}") && loopId == "2300" && !string.IsNullOrWhiteSpace(TxtHIICD10Codes.Text))
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
                                                                        if (!string.IsNullOrWhiteSpace(cpitem.value) && TxtHIICD10Codes.Text.Contains(cpitem.value.Trim()) && isABF)
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
                //aesAlg.IV = iv;
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

   // Download file
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
                if (!(string.IsNullOrWhiteSpace(SelectedFile)) && !(string.IsNullOrWhiteSpace(SelectedFolderPath)))
                {
                    var fileName = string.IsNullOrWhiteSpace(SelectedFile) ? "defaultFileName" : SelectedFile;
                    var filePath = Path.Combine(SelectedFolderPath, fileName);

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
}