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

    protected async Task<bool> ShowYesNoDialog(string question)
    {
        bool result = false;
        var yesButton = new Button { Content = "Da" };
        var noButton = new Button { Content = "Nu" };

        var dialog = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel
            {
                Margin = new Thickness(10),
                Children =
                {
                    new TextBlock
                    {
                        Text = question,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 20),
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 20,
                        Children = { yesButton, noButton },
                    },
                },
            },
        };

        yesButton.Click += (_, __) =>
        {
            result = true;
            dialog.Close();
        };

        noButton.Click += (_, __) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(GetMainWindow());
        return result;
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
        return this.VisualRoot as MainWindow;
    }
}
