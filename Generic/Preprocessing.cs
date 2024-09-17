using System.Collections.Generic;
using Vatee.CashRegister;
using Vatee.Invoice;

public class Preprocessing
{
    public static Output Preprocess(ExtractionGroup extractionResult)
    {
        var output = new Output();
        if (extractionResult.InvoiceResult != null)
        {
            output.Invoices = InvoicePreprocessor.NormalizeInvoices(extractionResult.InvoiceResult.Rows);
        }

        if (extractionResult.CashRegisterResult != null)
        {
            output.CashRegisterReports = CashRegisterPreprocessor.NormalizeCashRegister(extractionResult.CashRegisterResult.Rows);
        }

        return output;
    }

    public class Output
    {
        public List<InvoiceModel> Invoices { get; set; }
        public List<CashRegisterModel> CashRegisterReports { get; set; }
    }
}
