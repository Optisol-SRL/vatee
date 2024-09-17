using System;
using Vatee;
using Vatee.CashRegister;
using Vatee.Invoice;

public class PdfExtraction
{
    public static ExtractionGroup Extract(FileInspectionResult inspectionResult)
    {
        if (inspectionResult.Result != FileInspectionResult.ResultType.Success)
        {
            throw new Exception("Attempt to extract with non-success result");
        }

        if (inspectionResult.InvoicePdf == null && inspectionResult.CashRegisterPdf == null)
        {
            throw new Exception("Attempt to extract with no valid files");
        }

        ExtractionResult<InvoiceRow> invoiceRes = null;
        if (inspectionResult.InvoicePdf != null)
        {
            using var invoiceExtractor = new InvoiceExtractor(inspectionResult.InvoicePdf);
            invoiceRes = invoiceExtractor.Extract();
        }

        ExtractionResult<CashRegisterRow> cashRegisterRes = null;
        if (inspectionResult.CashRegisterPdf != null)
        {
            using var cashRegisterExtractor = new CashRegisterExtractor(inspectionResult.CashRegisterPdf);
            cashRegisterRes = cashRegisterExtractor.Extract();
        }

        return new ExtractionGroup { InvoiceResult = invoiceRes, CashRegisterResult = cashRegisterRes };
    }
}

public class ExtractionGroup
{
    public ExtractionResult<InvoiceRow> InvoiceResult { get; set; }
    public ExtractionResult<CashRegisterRow> CashRegisterResult { get; set; }

    public bool IsEmpty =>
        InvoiceResult is not { Status: ExtractionStatus.Success } && CashRegisterResult is not { Status: ExtractionStatus.Success };
}
