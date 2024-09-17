using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Vatee
{
    public partial class MainWindow : Window
    {
        private string _selectedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filters = new List<FileDialogFilter>
                {
                    new() { Name = "Fisiere", Extensions = { "pdf", "zip" } },
                },
                AllowMultiple = false,
            };

            var result = await openFileDialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                _selectedFilePath = result[0];
                SelectedFilePathTextBlock.Text = Path.GetFileName(_selectedFilePath);
                SaveFileButton.IsEnabled = true;
            }
            else
            {
                SelectedFilePathTextBlock.Text = "Nu ai ales fisierul sursa";
                SaveFileButton.IsEnabled = false;
            }
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

                var saveFileDialog = new SaveFileDialog
                {
                    DefaultExtension = "xlsx",
                    InitialFileName = Path.GetFileNameWithoutExtension(_selectedFilePath) + ".xlsx",
                    Filters =
                    {
                        new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } },
                    },
                };

                var saveFilePath = await saveFileDialog.ShowAsync(this);
                if (string.IsNullOrWhiteSpace(saveFilePath))
                {
                    await ShowMessageDialog("Nu am putut salva rezultatele. Nu am găsit calea specificată pentru salvare.");
                    return;
                }

                await File.WriteAllBytesAsync(saveFilePath, excelResult);
                await OpenFileWithOS(saveFilePath);
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

        private async void OnLinkTapped(object sender, RoutedEventArgs e)
        {
            var url = "https://in-dosar.ro/utilitare/detalii-p300-in-excel";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Handle any exceptions here, e.g. log the error or show a message to the user
                Logger.WriteLine(ex.ToString());
            }
        }

        private async Task ShowMessageDialog(string message)
        {
            var dialog = new Window
            {
                Width = 400,
                Height = 200,
                Content = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Thickness(10),
                },
            };
            await dialog.ShowDialog(this);
        }

        private async Task OpenFileWithOS(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception)
            {
                await ShowMessageDialog("Am extras fișierul, dar nu l-am putut deschide în Excel. Va trebui să îl deschizi tu manual.");
            }
        }
    }
}
