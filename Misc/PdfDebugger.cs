using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
}

public class PdfDebugger : IPdfDebugger
{
    private readonly string _debugPath;
    private readonly string _filename;
    private PdfPageBuilder _page;
    private readonly PdfDocumentBuilder _builder;
    private readonly PdfDocumentBuilder.AddedFont _font;
    private static Lazy<byte[]> OpenSansFont = new Lazy<byte[]>(() =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Vatee.Resources.OpenSans-Regular.ttf";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        return ms.ToArray();
    });

    public PdfDebugger(string debugPath, string filename)
    {
        _filename = filename;
        _debugPath = debugPath;
        _builder = new PdfDocumentBuilder();
        _font = _builder.AddTrueTypeFont(OpenSansFont.Value);
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
        File.WriteAllBytes(Path.Combine(_debugPath, _filename), bytes);
    }

    public void Dispose()
    {
        _builder?.Dispose();
    }

    public static IPdfDebugger Create(string filename)
    {
        var debugPath = Environment.GetEnvironmentVariable("VATEE_DEBUG_PATH");
        return !string.IsNullOrWhiteSpace(debugPath) ? new PdfDebugger(debugPath, filename) : new NoOpPdfDebugger();
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

    public void Dispose() { }
}
