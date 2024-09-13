using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace Vatee;

public static class ExcelGen
{
    public static void GenerateForInvoices(List<InvoiceModel> invoices, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Facturi");

            int currentRow = 1;

            var headers = new List<string>
            {
                "Rând în fișier", "Nr. Pagină","Rând în pagină" , "Rând în factură", "Avertismente", "Index", "Data inreg.", "CIF emitent", "Denumire emitent", "CIF beneficiar", "Denumire beneficiar",
                "Nr. factur" /*sic*/, "Data emitere", "Data exigib", "Data livrare", "Data scadent", "Tip fact.", "Data selectie",
                "Cota TVA", "Baza", "TVA", 
            };

            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
            }

            foreach (var invoice in invoices)
            {
                var invIdx = 1;
                foreach (var vatQuota in invoice.VatQuotas)
                {
                    currentRow++;
                    int currentColumn = 1;

                    worksheet.Cell(currentRow, currentColumn++).Value = vatQuota.GlobalIndex;
                    worksheet.Cell(currentRow, currentColumn++).Value = vatQuota.Page;
                    worksheet.Cell(currentRow, currentColumn++).Value = vatQuota.PageIndex;
                    worksheet.Cell(currentRow, currentColumn++).Value = invIdx++;
                    
                    var rowWarnings = invoice.Warnings.Union(invoice.VatQuotas.SelectMany(x => x.Warnings)).ToList();
                    if (rowWarnings.Any())
                    {
                        worksheet.Cell(currentRow, currentColumn).Value = "DA";
                        var comment = worksheet.Cell(currentRow, currentColumn).GetComment();
                        comment.AddText(string.Join("\n", rowWarnings));
                        comment.Visible = false;
                        currentColumn += 1;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, currentColumn++).Value = "NU";
                    }

                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.UploadId;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.UploadDate.ToString("dd.MM.yyyy");
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.SellerFiscal;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.SellerName;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.BuyerFiscal;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.BuyerName;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.InvoiceNum;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.IssueDate.ToString("dd.MM.yyyy");
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.TaxDate?.ToString("dd.MM.yyyy") ?? "";
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.DeliveryDate?.ToString("dd.MM.yyyy") ?? "";
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.DueDate?.ToString("dd.MM.yyyy") ?? "";
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.InvoiceType;
                    worksheet.Cell(currentRow, currentColumn++).Value = invoice.SelectionDate.ToString("dd.MM.yyyy");

                    worksheet.Cell(currentRow, currentColumn++).Value = vatQuota.VatQuota;
                    worksheet.Cell(currentRow, currentColumn++).Value = vatQuota.BaseValue;
                    worksheet.Cell(currentRow, currentColumn).Value = vatQuota.VatValue;
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }
}