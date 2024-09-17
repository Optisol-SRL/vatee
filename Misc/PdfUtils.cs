using System.Collections.Generic;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Vatee;

public static class PdfUtils
{
    public static string ExtractTextInRect(IEnumerable<Word> pageWords, float x, float y, float width, float height, IPdfDebugger debugger)
    {
        var cellText = new List<string>();
        var padding = 3;

        var rect = new PdfRectangle(x - padding, y - 1, x + width + padding, y + height + 1);

        debugger?.DrawRectangle(rect);

        foreach (var word in pageWords)
        {
            if (rect.Contains(word.BoundingBox))
            {
                cellText.Add(word.Text);
            }
        }

        var extractedText = string.Join(" ", cellText);
        Logger.WriteLine($"Extracted text from cell at ({x}, {y}): {extractedText}");

        return extractedText;
    }
}
