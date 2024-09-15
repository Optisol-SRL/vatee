using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using Vatee;

public class Extraction
{
    public static ExtractionResult Extract(string pdfPath)
    {
        using PdfDocument document = PdfDocument.Open(pdfPath);
        using var pdfDebugger = PdfDebugger.Create();
        var allRows = new List<ExtractedRow>();

        var firstPage = document.GetPage(1);
        var firstPageWords = firstPage.GetWords().ToList();

        for (var i = 1; i < document.NumberOfPages + 1; i++)
        {
            var page = document.GetPage(i);
            var words = page.GetWords().ToList();

            pdfDebugger.WriteLine($"Page dimensions: Width = {page.Width}, Height = {page.Height}");
            pdfDebugger.WriteLine($"Total words on page: {words.Count}");

            pdfDebugger.CopyPage(page);

            if (i == 1)
            {
                var headerText = ExtractCell(firstPageWords, 280f, 500f, 400, 100, pdfDebugger);
                Console.WriteLine(headerText);
                if (!Utils.FlattenRomanianDiacritics(headerText).Contains("Sistemul national RO e-Factura"))
                {
                    return new ExtractionResult { MatchedDocument = false, Rows = new List<ExtractedRow>() };
                }
            }

            var pageRows = ExtractPage(i, words, pdfDebugger);
            allRows.AddRange(pageRows);
        }

        pdfDebugger.WriteFile();

        return new ExtractionResult { MatchedDocument = true, Rows = allRows };
    }

    private const float RowHeight = 20;

    private static List<ExtractedRow> ExtractPage(int pageNum, List<Word> pageWords, IPdfDebugger pdfDebugger)
    {
        var startingY = pageNum == 1 ? 352f : 490f;

        var rows = new List<ExtractedRow>();

        while (startingY > 40f)
        {
            var row = ExtractRow(pageNum, startingY, pageWords, pdfDebugger);
            if (row.IsEmpty)
            {
                break;
            }

            rows.Add(row);
            startingY -= RowHeight;
        }

        foreach (var row in rows)
        {
            pdfDebugger.WriteLine(
                $"{row.Index}\t{row.UploadDate}\t{row.SellerFiscal}\t{row.SellerName}"
                    + $"\t{row.BuyerFiscal}\t{row.BuyerName}\t{row.InvoiceNum}\t{row.IssueDate}"
                    + $"\t{row.TaxDate}\t{row.DeliveryDate}\t{row.DueDate}\t{row.InvoiceType}"
                    + $"\t{row.SelectionDate}\t{row.VatQuota}\t{row.BaseValue}\t{row.VatValue}"
            );
        }

        return rows;
    }

    private static ExtractedRow ExtractRow(int pageNum, float y, List<Word> pageWords, IPdfDebugger debugger)
    {
        var startingX = 25f;

        string ReadCell(float width)
        {
            var text = ExtractCell(pageWords, startingX, y, width, RowHeight, debugger);
            startingX += width;
            return text;
        }

        var res = new ExtractedRow { Page = pageNum };

        res.Index = ReadCell(48);
        res.UploadDate = ReadCell(51f);
        res.SellerFiscal = ReadCell(47);
        res.SellerName = ReadCell(72);
        res.BuyerFiscal = ReadCell(53);
        res.BuyerName = ReadCell(60);
        res.InvoiceNum = ReadCell(70);
        res.IssueDate = ReadCell(55);
        res.TaxDate = ReadCell(45);
        res.DeliveryDate = ReadCell(49);
        res.DueDate = ReadCell(45);
        res.InvoiceType = ReadCell(20);
        res.SelectionDate = ReadCell(50);
        res.VatQuota = ReadCell(22);
        res.BaseValue = ReadCell(62);
        res.VatValue = ReadCell(62);

        return res;
    }

    private static string ExtractCell(IEnumerable<Word> pageWords, float x, float y, float width, float height, IPdfDebugger debugger)
    {
        var cellText = new List<string>();
        var padding = 3;

        var rect = new PdfRectangle(x - padding, y - 1, x + width + padding, y + height + 1);

        debugger.DrawRectangle(rect);

        foreach (var word in pageWords)
        {
            if (rect.Contains(word.BoundingBox))
            {
                cellText.Add(word.Text);
            }
        }

        var extractedText = string.Join(" ", cellText);
        debugger.WriteLine($"Extracted text from cell at ({x}, {y}): {extractedText}");

        return extractedText;
    }
}

public class ExtractionResult
{
    public bool MatchedDocument { get; set; }
    public List<ExtractedRow> Rows { get; set; }
}
