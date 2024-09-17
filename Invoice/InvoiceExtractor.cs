using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Vatee.Invoice;

public class InvoiceExtractor : IDisposable
{
    private const float RowHeight = 20;

    private readonly PdfDocument _document;
    private readonly IPdfDebugger _debugger;

    public InvoiceExtractor(ValidPdfResult result)
    {
        _document = PdfDocument.Open(result.Bytes);
        _debugger = PdfDebugger.Create(result.FileName);
    }

    public ExtractionResult<InvoiceRow> Extract()
    {
        var allRows = new List<InvoiceRow>();

        for (var i = 1; i < _document.NumberOfPages + 1; i++)
        {
            var page = _document.GetPage(i);
            var words = page.GetWords().ToList();

            _debugger.CopyPage(page);

            var pageRows = ExtractPage(i, words);
            allRows.AddRange(pageRows);
        }

        _debugger.WriteFile();

        return new ExtractionResult<InvoiceRow>
        {
            Status = allRows.Any() ? ExtractionStatus.Success : ExtractionStatus.NoEntries,
            Rows = allRows,
        };
    }

    private List<InvoiceRow> ExtractPage(int pageNum, List<Word> pageWords)
    {
        var startingY = pageNum == 1 ? 352f : 490f;

        var rows = new List<InvoiceRow>();

        while (startingY > 40f)
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
                $"{row.Index}\t{row.UploadDate}\t{row.SellerFiscal}\t{row.SellerName}"
                    + $"\t{row.BuyerFiscal}\t{row.BuyerName}\t{row.InvoiceNum}\t{row.IssueDate}"
                    + $"\t{row.TaxDate}\t{row.DeliveryDate}\t{row.DueDate}\t{row.InvoiceType}"
                    + $"\t{row.SelectionDate}\t{row.VatQuota}\t{row.BaseValue}\t{row.VatValue}"
            );
        }

        return rows;
    }

    private InvoiceRow ExtractRow(int pageNum, float y, List<Word> pageWords)
    {
        var startingX = 25f;

        string readCell(float width)
        {
            var text = PdfUtils.ExtractTextInRect(pageWords, startingX, y, width, RowHeight, _debugger);
            startingX += width;
            return text;
        }

        // ReSharper disable once UseObjectOrCollectionInitializer
        var res = new InvoiceRow { Page = pageNum };

        res.Index = readCell(48);
        res.UploadDate = readCell(51f);
        res.SellerFiscal = readCell(47);
        res.SellerName = readCell(72);
        res.BuyerFiscal = readCell(53);
        res.BuyerName = readCell(60);
        res.InvoiceNum = readCell(70);
        res.IssueDate = readCell(55);
        res.TaxDate = readCell(45);
        res.DeliveryDate = readCell(49);
        res.DueDate = readCell(45);
        res.InvoiceType = readCell(20);
        res.SelectionDate = readCell(50);
        res.VatQuota = readCell(22);
        res.BaseValue = readCell(62);
        res.VatValue = readCell(62);

        return res;
    }

    public static bool IsInvoicePdf(IEnumerable<Word> firstPageWords)
    {
        var headerText = PdfUtils.ExtractTextInRect(firstPageWords, 280f, 500f, 400, 100, null);
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return false;
        }

        return Utils.FlattenRomanianDiacritics(headerText).Contains("Sistemul national RO e-Factura");
    }

    public void Dispose()
    {
        _debugger?.Dispose();
        _document?.Dispose();
    }
}
