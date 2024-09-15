using System;
using System.IO;
using System.Threading.Tasks;

namespace Vatee;

public class FileInspector
{
    public async Task<FileInspectionResult> Inspect(string path)
    {
        var (read, bytes) = await TryReadAllBytes(path).ConfigureAwait(false);
        if (!read)
        {
            return new FileInspectionResult { Result = FileInspectionResult.ResultType.ReadFileError };
        }

        var extension = Path.GetExtension(path);
    }

    public async Task InspectArchive(byte[] bytes) { }

    public async Task InspectInvoicePdf(byte[] bytes) { }

    public async Task InspectCashRegisterPdf(byte[] bytes) { }

    public async Task<(bool, byte[])> TryReadAllBytes(string path)
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

public class FileInspectionResult
{
    public ResultType Result { get; set; }
    public ValidPdfResult InvoicePdf { get; set; }
    public ValidPdfResult CashRegisterPdf { get; set; }

    public enum ResultType
    {
        ReadFileError,
        UnknownTypeError,
        GenericError,
    }
}

public class ValidPdfResult
{
    public string FileName { get; set; }
    public byte[] Bytes { get; set; }
}
