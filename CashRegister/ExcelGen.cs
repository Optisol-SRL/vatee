using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using Vatee.Invoice;

namespace Vatee.CashRegister;

public static class CashRegisterExcelGen
{
    public static void GenerateForCashRegister(List<CashRegisterModel> cashRegisterReports, XLWorkbook wb)
    {
        var worksheet = wb.Worksheets.Add("AMEF");

        int currentRow = 1;
        var headers = new List<string>
        {
            "Rând în fișier",
            "Nr. Pagină",
            "Rând în pagină",
            "Avertismente",
            "N.U.I - A.M.E.F",
            "Numarul de ordine al raportului Z",
            "Data emiterii raportului Z",
            "Valoarea totală zilnică a operațiunilor cu cota de 19%",
            "Valoarea TVA aferentă operațiunilor cu cota de 19%",
            "Valoarea totală zilnică a operațiunilor cu cota de 9%",
            "Valoarea TVA aferentă operațiunilor cu cota de 9%",
            "Valoarea totală zilnică a operațiunilor cu cota de 5%",
            "Valoarea TVA aferentă operațiunilor cu cota de 5%",
            "Valoarea totală zilnică a operațiunilor scutite",
            "Valoarea TVA aferentă operațiunilor scutite",
        };

        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(currentRow, i + 1).Value = headers[i];
        }

        foreach (var report in cashRegisterReports)
        {
            var invIdx = 1;
            currentRow++;
            int currentColumn = 1;

            worksheet.Cell(currentRow, currentColumn++).Value = report.GlobalIndex;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Page;
            worksheet.Cell(currentRow, currentColumn++).Value = report.PageIndex;

            if (report.Warnings.Any())
            {
                worksheet.Cell(currentRow, currentColumn).Value = "DA";
                var comment = worksheet.Cell(currentRow, currentColumn).GetComment();
                comment.AddText(string.Join("\n", report.Warnings));
                comment.Visible = false;
                currentColumn += 1;
            }
            else
            {
                worksheet.Cell(currentRow, currentColumn++).Value = "NU";
            }

            worksheet.Cell(currentRow, currentColumn++).Value = report.RegisterId;
            worksheet.Cell(currentRow, currentColumn++).Value = report.ReportId;
            worksheet.Cell(currentRow, currentColumn++).Value = report.ReportDate?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat19Base;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat19Value;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat9Base;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat9Value;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat5Base;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat5Value;
            worksheet.Cell(currentRow, currentColumn++).Value = report.Vat0Base;
            worksheet.Cell(currentRow, currentColumn).Value = report.Vat0Value;
        }

        worksheet.Columns().AdjustToContents();
    }
}
