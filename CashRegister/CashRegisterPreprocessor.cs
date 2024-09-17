using System;
using System.Collections.Generic;
using System.Globalization;

namespace Vatee.CashRegister;

public class CashRegisterPreprocessor
{
    public static List<CashRegisterModel> NormalizeCashRegister(List<CashRegisterRow> extractedRows)
    {
        List<CashRegisterModel> cashRegisterReports = new();

        var globalIndex = 1;
        var currentPage = 1;
        var currentPageIndex = 1;

        var culture = new CultureInfo("ro-RO");

        foreach (var row in extractedRows)
        {
            if (currentPage != row.Page)
            {
                currentPage = row.Page;
                currentPageIndex = 1;
            }

            if (row.IsEmpty)
            {
                continue;
            }

            var currentReport = new CashRegisterModel
            {
                Page = row.Page,
                PageIndex = currentPageIndex,
                GlobalIndex = globalIndex,
            };

            currentReport.RegisterId = row.RegisterId?.Trim();
            if (string.IsNullOrWhiteSpace(currentReport.RegisterId))
            {
                currentReport.Warnings.Add("Nu am putut extrage NUI AMEF");
            }

            if (int.TryParse(row.ReportId, out var repId))
            {
                currentReport.ReportId = repId;
            }
            else
            {
                currentReport.Warnings.Add("Nu am putut extrage nr. ordine Z");
            }

            if (DateOnly.TryParseExact(row.ReportDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var reportDate))
            {
                currentReport.ReportDate = reportDate;
            }
            else
            {
                currentReport.Warnings.Add("Nu am putut extrage data raportului Z");
            }

            var percentages = new List<int> { 19, 9, 5, 0 };
            foreach (var percentage in percentages)
            {
                var baseStr = row.GetBaseByPercentage(percentage);
                decimal? parsedBase = null;
                if (!string.IsNullOrWhiteSpace(baseStr) && decimal.TryParse(baseStr, NumberStyles.Number, culture, out var baseDecimal))
                {
                    parsedBase = baseDecimal;
                }

                var valueStr = row.GetValueByPercentage(percentage);
                decimal? parsedValue = null;
                if (!string.IsNullOrWhiteSpace(valueStr) && decimal.TryParse(valueStr, NumberStyles.Number, culture, out var valueDecimal))
                {
                    parsedValue = valueDecimal;
                }

                if (parsedBase == null && parsedValue != null)
                {
                    currentReport.Warnings.Add($"Am extras valoarea TVA dar nu si baza pentru cota {percentage}%");
                }

                if (parsedBase != null && parsedValue == null)
                {
                    currentReport.Warnings.Add($"Am extras baza TVA dar nu si valoarea pentru cota {percentage}%");
                }

                currentReport.SetPairByPercentage(percentage, parsedBase, parsedValue);
            }

            cashRegisterReports.Add(currentReport);
            globalIndex += 1;
            currentPageIndex += 1;
        }

        return cashRegisterReports;
    }
}

public class CashRegisterModel
{
    public int Page { get; set; }
    public int PageIndex { get; set; }
    public int GlobalIndex { get; set; }

    public string RegisterId { get; set; }
    public int? ReportId { get; set; }
    public DateOnly? ReportDate { get; set; }
    public decimal? Vat19Base { get; set; }
    public decimal? Vat19Value { get; set; }
    public decimal? Vat9Base { get; set; }
    public decimal? Vat9Value { get; set; }
    public decimal? Vat5Base { get; set; }
    public decimal? Vat5Value { get; set; }
    public decimal? Vat0Base { get; set; }
    public decimal? Vat0Value { get; set; }

    public void SetPairByPercentage(int percentage, decimal? baseVal, decimal? valueVal)
    {
        switch (percentage)
        {
            case 19:
                Vat19Base = baseVal;
                Vat19Value = valueVal;
                break;
            case 9:
                Vat9Base = baseVal;
                Vat9Value = valueVal;
                break;
            case 5:
                Vat5Base = baseVal;
                Vat5Value = valueVal;
                break;
            case 0:
                Vat0Base = baseVal;
                Vat0Value = valueVal;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(percentage), percentage, null);
        }
    }

    public List<string> Warnings { get; set; } = new();
}
