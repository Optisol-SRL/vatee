using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Vatee;

public partial class TabPdfExtract : VateeControl
{
    public TabPdfExtract()
    {
        InitializeComponent();
    }

    private string _selectedFilePath;

    private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var res = await GetMainWindow()
            .StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    FileTypeFilter = [new FilePickerFileType("Fisiere") { Patterns = ["*.pdf", "*.zip"] }],
                    AllowMultiple = false,
                }
            );

        if (res.Count != 1)
        {
            SelectedFilePathTextBlock.Text = "Nu ai ales fisierul sursa";
            SaveFileButton.IsEnabled = false;
            return;
        }

        var file = res[0];
        _selectedFilePath = file.Path.AbsolutePath;
        SelectedFilePathTextBlock.Text = Path.GetFileName(_selectedFilePath);
        SaveFileButton.IsEnabled = true;
    }

    private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            await ShowMessageDialog("Nu ai ales un fisier din care sa extragem informatiile.");
            return;
        }

        SaveFileButton.IsEnabled = false;
        StatusTextBlock.Text = "Extragem datele....";
        ProgressBar.IsVisible = true;

        try
        {
            var inspectionResult = await FileInspector.Inspect(_selectedFilePath);
            if (inspectionResult.Result != FileInspectionResult.ResultType.Success)
            {
                var message = inspectionResult.Result switch
                {
                    FileInspectionResult.ResultType.ErrorReadFile =>
                        "Nu am putut citi fișierul. Asigură-te că nu este deschis în alt program.",
                    FileInspectionResult.ResultType.ErrorUnknownType =>
                        "Nu am putut citi acest tip de fișier. Tipurile suportate sunt .pdf și .zip.",
                    FileInspectionResult.ResultType.ErrorArchiveNoFiles =>
                        "Arhiva nu conține fișierele cu detalii pe care le recunoaștem (P300_Facturi_*.pdf, P300_Amef_*.pdf)",
                    FileInspectionResult.ResultType.ErrorArchiveTooManyFiles => "Arhiva conține mai mult de un fișier din fiecare tip.",
                    FileInspectionResult.ResultType.ErrorPdfUnknownTemplate =>
                        "Nu recunoaștem acest șablon de PDF. Programul funcționează doar cu detaliile P300 pentru e-Factura și AMEF.",
                    FileInspectionResult.ResultType.ErrorGeneric => "Nu am putut extrage informațiile din fișier.",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                await ShowMessageDialog(message);
                return;
            }

            var extractionResult = await Task.Run(() => PdfExtraction.Extract(inspectionResult));
            if (extractionResult.IsEmpty)
            {
                await ShowMessageDialog("Nu am putut extrage nicio inregistrare din fișier");
                return;
            }

            var preprocessingResult = await Task.Run(() => Preprocessing.Preprocess(extractionResult));
            var excelResult = await Task.Run(() => ExcelGen.Generate(preprocessingResult));

            var saveResult = await GetMainWindow()
                .StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        DefaultExtension = "xlsx",
                        SuggestedFileName = Path.GetFileNameWithoutExtension(_selectedFilePath) + ".xlsx",
                        FileTypeChoices = [new FilePickerFileType("Excel Files") { Patterns = ["*.xlsx"] }],
                    }
                );

            if (saveResult == null)
            {
                await ShowMessageDialog("Nu am putut salva rezultatele. Nu am găsit calea specificată pentru salvare.");
                return;
            }

            await File.WriteAllBytesAsync(saveResult.Path.AbsolutePath, excelResult);
            await OpenFileWithOS(saveResult.Path.AbsolutePath);
        }
        catch (Exception ex)
        {
            Logger.WriteLine(ex.ToString());
            await ShowMessageDialog("Nu am putut procesa fisierul");
        }
        finally
        {
            SaveFileButton.IsEnabled = true;
            StatusTextBlock.Text = "";
            ProgressBar.IsVisible = false;
        }
    }
}
