using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Vatee;

public interface IPdfDebugger : IDisposable
{
    void CopyPage(Page originalPage);
    void DrawRectangle(PdfRectangle rect);
    void WriteFile();
    void WriteLine(string line);
}

public class PdfDebugger : IPdfDebugger
{
    private readonly string _debugPath;
    private PdfPageBuilder _page;
    private PdfDocumentBuilder _builder;
    private PdfDocumentBuilder.AddedFont _font;

    public PdfDebugger(string debugPath)
    {
        _debugPath = debugPath;
        _builder = new PdfDocumentBuilder();
        _font = _builder.AddStandard14Font(Standard14Font.Helvetica);
    }

    public void CopyPage(Page originalPage)
    {
        var newPage = _builder.AddPage(originalPage.Width, originalPage.Height);

        foreach (var word in originalPage.GetWords())
        {
            // don't want to bother with fonts for this
            var cleanText = Utils.FlattenRomanianDiacritics(word.Text);
            newPage.AddText(cleanText, word.Letters.First().FontSize, word.BoundingBox.BottomLeft, _font);
        }

        _page = newPage;
    }

    public void DrawRectangle(PdfRectangle rect)
    {
        var color = GetNextColor();
        _page.SetStrokeColor(color.R, color.G, color.B);

        _page.DrawLine(rect.BottomLeft, new PdfPoint(rect.BottomRight.X, rect.BottomRight.Y));
        _page.DrawLine(rect.BottomRight, new PdfPoint(rect.TopRight.X, rect.TopRight.Y));
        _page.DrawLine(rect.TopRight, new PdfPoint(rect.TopLeft.X, rect.TopLeft.Y));
        _page.DrawLine(rect.TopLeft, new PdfPoint(rect.BottomLeft.X, rect.BottomLeft.Y));
    }

    public void WriteFile()
    {
        var bytes = _builder.Build();
        File.WriteAllBytes(_debugPath, bytes);
    }

    public void WriteLine(string line)
    {
        Console.WriteLine(line);
    }

    public void Dispose()
    {
        _builder?.Dispose();
    }

    public static IPdfDebugger Create()
    {
        var debugPath = Environment.GetEnvironmentVariable("VATEE_DEBUG_PATH");
        return !string.IsNullOrWhiteSpace(debugPath) ? new PdfDebugger(debugPath) : new NoOpPdfDebugger();
    }

    private static int colorIndex = 0;

    private static readonly (byte R, byte G, byte B)[] Colors = new[]
    {
        ((byte)255, (byte)0, (byte)0), // Red
        ((byte)0, (byte)255, (byte)0), // Green
        ((byte)0, (byte)0, (byte)255), // Blue
        ((byte)255, (byte)255, (byte)0), // Yellow
        ((byte)255, (byte)0, (byte)255), // Magenta
        ((byte)0, (byte)255, (byte)255), // Cyan
        ((byte)255, (byte)128, (byte)0), // Orange
        ((byte)128, (byte)0, (byte)255), // Purple
        ((byte)0, (byte)128, (byte)0), // Dark Green
        (
            (byte)0,
            (byte)0,
            (byte)128
        ) // Navy Blue
        ,
    };

    private static (byte R, byte G, byte B) GetNextColor()
    {
        var color = Colors[colorIndex];
        colorIndex = (colorIndex + 1) % Colors.Length;
        return color;
    }
}

public class NoOpPdfDebugger : IPdfDebugger
{
    public void CopyPage(Page originalPage) { }

    public void DrawRectangle(PdfRectangle rect) { }

    public void WriteFile() { }

    public void WriteLine(string line) { }

    public void Dispose() { }
}
