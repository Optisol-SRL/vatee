using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Vatee;

public class VateeControl : UserControl
{
    protected async Task ShowMessageDialog(string message)
    {
        var dialog = new Window
        {
            Width = 400,
            Height = 200,
            Content = new TextBlock
            {
                Text = message,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(10),
            },
        };
        await dialog.ShowDialog(GetMainWindow());
    }

    protected async Task OpenFileWithOS(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception)
        {
            await ShowMessageDialog("Nu am putut deschide în Excel fișierul cu rezultate. Va trebui să îl deschizi tu manual.");
        }
    }

    protected MainWindow GetMainWindow()
    {
        return this.FindControl<Window>("MainWindow") as MainWindow;
    }
}
