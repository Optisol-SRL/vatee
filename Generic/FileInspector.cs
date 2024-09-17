using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Vatee.CashRegister;
using Vatee.Invoice;

namespace Vatee;

public static class FileInspector
{
    public static async Task<FileInspectionResult> Inspect(string path)
    {
        var (read, bytes) = await TryReadAllBytes(path).ConfigureAwait(false);
        if (!read)
        {
            return new FileInspectionResult { Result = FileInspectionResult.ResultType.ErrorReadFile };
        }

        var extension = Path.GetExtension(path);
        var filename = Path.GetFileName(path);

        switch (extension)
        {
            case ".pdf":
            {
                var res = InspectGenericPdf(bytes);
                if (res == PdfInspectionResult.Failed)
                {
                    // some software they use will actually save the zip as a PDF - handle that for them
                    goto case ".zip";
                }

                switch (res)
                {
                    case PdfInspectionResult.InvoicePdf:
                        return new FileInspectionResult
                        {
                            Result = FileInspectionResult.ResultType.Success,
                            InvoicePdf = new ValidPdfResult { FileName = filename, Bytes = bytes },
                        };
                    case PdfInspectionResult.CashRegisterPdf:
                        return new FileInspectionResult
                        {
                            Result = FileInspectionResult.ResultType.Success,
                            CashRegisterPdf = new ValidPdfResult { FileName = filename, Bytes = bytes },
                        };
                    case PdfInspectionResult.UnknownTemplate:
                        return new FileInspectionResult { Result = FileInspectionResult.ResultType.ErrorPdfUnknownTemplate };
                    case PdfInspectionResult.Failed:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            case ".zip":
            {
                var res = InspectArchive(bytes);
                return new FileInspectionResult
                {
                    InvoicePdf = res.InvoicePdf,
                    CashRegisterPdf = res.CashRegisterPdf,
                    Result = res.Result switch
                    {
                        ArchiveInspectionResult.ResultType.Success => FileInspectionResult.ResultType.Success,
                        ArchiveInspectionResult.ResultType.ErrorGeneric => FileInspectionResult.ResultType.ErrorGeneric,
                        ArchiveInspectionResult.ResultType.NoFilesFound => FileInspectionResult.ResultType.ErrorArchiveNoFiles,
                        ArchiveInspectionResult.ResultType.TooManyFiles => FileInspectionResult.ResultType.ErrorArchiveTooManyFiles,
                        _ => throw new ArgumentOutOfRangeException(),
                    },
                };
            }
            default:
                return new FileInspectionResult { Result = FileInspectionResult.ResultType.ErrorUnknownType };
        }
    }

    private static ArchiveInspectionResult InspectArchive(byte[] bytes)
    {
        try
        {
            using var ms = new MemoryStream(bytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

            var invoiceCandidates = archive.Entries.Where(r => r.Name.StartsWith("P300_Facturi_") && r.Name.EndsWith(".pdf")).ToList();
            var cashRegisterCandidates = archive.Entries.Where(r => r.Name.StartsWith("P300_Amef_") && r.Name.EndsWith(".pdf")).ToList();

            if (invoiceCandidates.Count > 1 || cashRegisterCandidates.Count > 1)
            {
                return new ArchiveInspectionResult { Result = ArchiveInspectionResult.ResultType.TooManyFiles };
            }

            if (invoiceCandidates.Count == 0 && cashRegisterCandidates.Count == 0)
            {
                return new ArchiveInspectionResult { Result = ArchiveInspectionResult.ResultType.NoFilesFound };
            }

            var invoiceEntry = invoiceCandidates.FirstOrDefault();
            ValidPdfResult invoiceRes = null;
            if (invoiceEntry != null)
            {
                var invoiceBytes = invoiceEntry.GetBytes();
                if (InspectInvoicePdf(invoiceBytes))
                {
                    invoiceRes = new ValidPdfResult { FileName = invoiceEntry.Name, Bytes = invoiceBytes };
                }
            }

            var cashRegisterEntry = cashRegisterCandidates.FirstOrDefault();
            ValidPdfResult cashRegisterRes = null;
            if (cashRegisterEntry != null)
            {
                var cashRegisterBytes = cashRegisterEntry.GetBytes();
                if (InspectCashRegisterPdf(cashRegisterBytes))
                {
                    cashRegisterRes = new ValidPdfResult { FileName = cashRegisterEntry.Name, Bytes = cashRegisterBytes };
                }
            }

            if (invoiceRes == null && cashRegisterRes == null)
            {
                return new ArchiveInspectionResult { Result = ArchiveInspectionResult.ResultType.NoFilesFound };
            }

            return new ArchiveInspectionResult
            {
                Result = ArchiveInspectionResult.ResultType.Success,
                InvoicePdf = invoiceRes,
                CashRegisterPdf = cashRegisterRes,
            };
        }
        catch (Exception e)
        {
            Logger.WriteLine(e.ToString());
            return new ArchiveInspectionResult { Result = ArchiveInspectionResult.ResultType.ErrorGeneric };
        }
    }

    private static PdfInspectionResult InspectGenericPdf(byte[] bytes)
    {
        try
        {
            using var document = PdfDocument.Open(bytes);
            var firstPage = document.GetPage(1);
            var firstPageWords = firstPage.GetWords().ToList();
            if (InvoiceExtractor.IsInvoicePdf(firstPageWords))
            {
                return PdfInspectionResult.InvoicePdf;
            }

            if (CashRegisterExtractor.IsCashRegisterPdf(firstPageWords))
            {
                return PdfInspectionResult.CashRegisterPdf;
            }

            return PdfInspectionResult.UnknownTemplate;
        }
        catch (Exception e)
        {
            Logger.WriteLine(e.ToString());
            return PdfInspectionResult.Failed;
        }
    }

    private static bool InspectInvoicePdf(byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);
        var firstPage = document.GetPage(1);
        var firstPageWords = firstPage.GetWords().ToList();
        return InvoiceExtractor.IsInvoicePdf(firstPageWords);
    }

    private static bool InspectCashRegisterPdf(byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);
        var firstPage = document.GetPage(1);
        var firstPageWords = firstPage.GetWords().ToList();
        return CashRegisterExtractor.IsCashRegisterPdf(firstPageWords);
    }

    private static async Task<(bool, byte[])> TryReadAllBytes(string path)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            return (true, bytes);
        }
        catch (Exception e)
        {
            Logger.WriteLine(e.ToString());
            return (false, null);
        }
    }
}

public enum PdfInspectionResult
{
    InvoicePdf,
    CashRegisterPdf,
    Failed,
    UnknownTemplate,
}

public class ArchiveInspectionResult
{
    public ResultType Result { get; set; }
    public ValidPdfResult InvoicePdf { get; set; }
    public ValidPdfResult CashRegisterPdf { get; set; }

    public enum ResultType
    {
        Success,
        ErrorGeneric,
        NoFilesFound,
        TooManyFiles,
    }
}

public class FileInspectionResult
{
    public ResultType Result { get; set; }
    public ValidPdfResult InvoicePdf { get; set; }
    public ValidPdfResult CashRegisterPdf { get; set; }

    public enum ResultType
    {
        Success,
        ErrorReadFile,
        ErrorUnknownType,
        ErrorArchiveNoFiles,
        ErrorArchiveTooManyFiles,
        ErrorPdfUnknownTemplate,
        ErrorGeneric,
    }
}

public class ValidPdfResult
{
    public string FileName { get; set; }
    public byte[] Bytes { get; set; }
}
