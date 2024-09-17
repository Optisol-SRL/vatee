using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Vatee.Invoice;

namespace Vatee.CashRegister;

public class CashRegisterExtractor : IDisposable
{
    private const float RowHeight = 13;
    private const float PtPerInch = 72;

    private readonly PdfDocument _document;
    private readonly IPdfDebugger _debugger;

    public CashRegisterExtractor(ValidPdfResult result)
    {
        _document = PdfDocument.Open(result.Bytes);
        _debugger = PdfDebugger.Create(result.FileName);
    }

    public ExtractionResult<CashRegisterRow> Extract()
    {
        var allRows = new List<CashRegisterRow>();

        for (var i = 1; i < _document.NumberOfPages + 1; i++)
        {
            var page = _document.GetPage(i);
            Logger.WriteLine($"{page.Width} x {page.Height}");
            var words = page.GetWords().ToList();

            _debugger.CopyPage(page);

            var pageRows = ExtractPage(i, words);
            allRows.AddRange(pageRows);
        }

        _debugger.WriteFile();

        return new ExtractionResult<CashRegisterRow>
        {
            Status = allRows.Any() ? ExtractionStatus.Success : ExtractionStatus.NoEntries,
            Rows = allRows,
        };
    }

    private List<CashRegisterRow> ExtractPage(int pageNum, List<Word> pageWords)
    {
        var pageHeader = (pageNum == 1 ? 1.67f : 0.31f) * PtPerInch;
        var tableHeader = 0.57f * PtPerInch;
        var pageHeight = 595f;

        // bottom of first row
        var startingY = pageHeight - pageHeader - tableHeader - RowHeight;

        var rows = new List<CashRegisterRow>();

        while (startingY > 0.35f * PtPerInch)
        {
            var row = ExtractRow(pageNum, startingY, pageWords);
            if (row.IsEmpty)
            {
                break;
            }

            rows.Add(row);
            startingY -= RowHeight;
        }

        foreach (var row in rows)
        {
            Logger.WriteLine(
                $"{row.RegisterId}\t{row.ReportId}\t{row.ReportDate}\t{row.Vat19Base}"
                    + $"\t{row.Vat19Value}\t{row.Vat9Base}\t{row.Vat9Value}\t{row.Vat5Base}"
                    + $"\t{row.Vat5Value}\t{row.Vat0Base}\t{row.Vat0Value}\t"
            );
        }

        return rows;
    }

    private CashRegisterRow ExtractRow(int pageNum, float y, List<Word> pageWords)
    {
        var startingX = 20f;

        string readCell(float width)
        {
            var text = PdfUtils.ExtractTextInRect(pageWords, startingX, y, width, RowHeight, _debugger);
            startingX += width;
            return text;
        }

        // ReSharper disable once UseObjectOrCollectionInitializer
        var res = new CashRegisterRow { Page = pageNum };

        res.RegisterId = readCell(70);
        res.ReportId = readCell(54);
        res.ReportDate = readCell(55);
        res.Vat19Base = readCell(1.1f * PtPerInch);
        res.Vat19Value = readCell(1.21f * PtPerInch);
        res.Vat9Base = readCell(1f * PtPerInch);
        res.Vat9Value = readCell(1.04f * PtPerInch);
        res.Vat5Base = readCell(1.04f * PtPerInch);
        res.Vat5Value = readCell(1.04f * PtPerInch);
        res.Vat0Base = readCell(1.04f * PtPerInch);
        res.Vat0Value = readCell(1.17f * PtPerInch);

        return res;
    }

    public static bool IsCashRegisterPdf(IEnumerable<Word> firstPageWords)
    {
        var headerText = PdfUtils.ExtractTextInRect(firstPageWords, 280f, 500f, 400, 100, null);
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return false;
        }

        return Utils.FlattenRomanianDiacritics(headerText).Contains("Sistemul informatic national RO e-Case de marcat");
    }

    public void Dispose()
    {
        _debugger?.Dispose();
        _document?.Dispose();
    }
}
