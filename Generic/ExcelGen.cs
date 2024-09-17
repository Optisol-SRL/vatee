using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Vatee.CashRegister;
using Vatee.Invoice;

namespace Vatee;

public static class ExcelGen
{
    public static byte[] Generate(Preprocessing.Output preprocessed)
    {
        using var workbook = new XLWorkbook();

        if (preprocessed.Invoices != null && preprocessed.Invoices.Any())
        {
            InvoiceExcelGen.GenerateForInvoices(preprocessed.Invoices, workbook);
        }

        if (preprocessed.CashRegisterReports != null && preprocessed.CashRegisterReports.Any())
        {
            CashRegisterExcelGen.GenerateForCashRegister(preprocessed.CashRegisterReports, workbook);
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return ms.ToArray();
    }
}
