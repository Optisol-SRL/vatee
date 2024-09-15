using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClosedXML.Excel;

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
                Filters = new List<FileDialogFilter>()
                {
                    new FileDialogFilter { Name = "Fisiere PDF", Extensions = { "pdf" } },
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
                ExtractionResult extractionResult = await ProcessFileAsync(_selectedFilePath);

                if (!extractionResult.MatchedDocument)
                {
                    await ShowMessageDialog(
                        "Nu am putut extrage informatii din fisier. Asigura-te ca este un fisier cu detalii e-Factura P300 la luna iulie"
                    );
                    return;
                }

                List<InvoiceModel> invoices = InvoiceProcessor.NormalizeInvoices(extractionResult.Rows);

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
                    await ShowMessageDialog("Nu am putut salva rezultatele");
                }

                if (!invoices.Any())
                {
                    await ShowMessageDialog("Nu am putut gasit nicio inregistrare in fisier.");
                }

                ExcelGen.GenerateForInvoices(invoices, saveFilePath);
                await OpenFileWithOS(saveFilePath);
            }
            catch (Exception)
            {
                await ShowMessageDialog("Nu am putut procesa fisierul");
            }
            finally
            {
                SaveFileButton.IsEnabled = true;
                StatusTextBlock.Text = "";
                ProgressBar.IsVisible = false;
            }
        }

        private async Task<ExtractionResult> ProcessFileAsync(string filePath)
        {
            return await Task.Run(() => Extraction.Extract(filePath));
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
