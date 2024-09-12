using System.Threading.Tasks;
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
            var openFileDialog = new OpenFileDialog();
            openFileDialog.AllowMultiple = false;

            var result = await openFileDialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                _selectedFilePath = result[0];
                SelectedFilePathTextBlock.Text = _selectedFilePath;
                SaveFileButton.IsEnabled = true;
            }
            else
            {
                SelectedFilePathTextBlock.Text = "No file selected";
                SaveFileButton.IsEnabled = false;
            }
        }

        private async void SaveFileButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                await ShowMessageDialog("Please select a file first.");
                return;
            }

            // Here you can perform some processing on the file
            string processedData = await ProcessFileAsync(_selectedFilePath);
            

            // var saveFileDialog = new SaveFileDialog();
            // saveFileDialog.DefaultExtension = "pdf";
            // saveFileDialog.Filters.Add(new FileDialogFilter() { Name = "Text Files", Extensions = { "txt" } });
            // saveFileDialog.Filters.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            //
            // var savePath = await saveFileDialog.ShowAsync(this);
            //
            // if (savePath != null)
            // {
            //     File.WriteAllText(savePath, processedData);
            //     await ShowMessageDialog($"File saved successfully at {savePath}");
            // }
        }

        private async Task<string> ProcessFileAsync(string filePath)
        {
            Extraction.Extract(filePath);

            return null;
        }

        private async Task ShowMessageDialog(string message)
        {
            var dialog = new Window
            {
                Width = 300,
                Height = 150,
                Content = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            await dialog.ShowDialog(this);
        }
    }
}
