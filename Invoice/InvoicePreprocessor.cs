using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vatee.Invoice;

public class InvoicePreprocessor
{
    public static List<InvoiceModel> NormalizeInvoices(List<InvoiceRow> extractedRows)
    {
        List<InvoiceModel> invoices = new();
        InvoiceModel currentInvoice = null;

        var globalIndex = 1;
        var currentPage = 1;
        var currentPageIndex = 1;

        foreach (var row in extractedRows)
        {
            if (currentPage != row.Page)
            {
                currentPage = row.Page;
                currentPageIndex = 1;
            }

            if (!row.IsVatOnly)
            {
                currentInvoice = new InvoiceModel
                {
                    Page = row.Page,
                    PageIndex = currentPageIndex,
                    GlobalIndex = globalIndex,
                    UploadId = row.Index,
                };

                ParseInvoiceFields(row, currentInvoice);
                invoices.Add(currentInvoice);
            }

            if (currentInvoice == null)
            {
                continue;
            }

            var vatQuota = new VatQuotaModel
            {
                Page = row.Page,
                PageIndex = currentPageIndex,
                GlobalIndex = globalIndex,
            };

            ParseVatFields(row, vatQuota);
            currentInvoice.VatQuotas.Add(vatQuota);

            globalIndex += 1;
            currentPageIndex += 1;
        }

        return invoices;
    }

    private static void ParseInvoiceFields(InvoiceRow row, InvoiceModel invoice)
    {
        var culture = new CultureInfo("ro-RO");

        if (DateOnly.TryParseExact(row.UploadDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var uploadDate))
        {
            invoice.UploadDate = uploadDate;
        }
        else
        {
            invoice.Warnings.Add("Dată încărcare invalidă.");
        }

        invoice.UploadId = row.Index?.Trim();
        if (string.IsNullOrWhiteSpace(invoice.UploadId))
        {
            invoice.Warnings.Add("Lipseste Indexul facturii.");
        }

        invoice.SellerFiscal = row.SellerFiscal?.Trim();
        if (string.IsNullOrWhiteSpace(invoice.SellerFiscal))
        {
            invoice.Warnings.Add("Lipseste CIF emitent.");
        }

        invoice.SellerName = row.SellerName?.Trim();
        invoice.BuyerFiscal = row.BuyerFiscal?.Trim();
        if (string.IsNullOrWhiteSpace(invoice.BuyerFiscal))
        {
            invoice.Warnings.Add("Lipseste CIF beneficiar.");
        }

        invoice.BuyerName = row.BuyerName?.Trim();

        invoice.InvoiceNum = row.InvoiceNum?.Trim();
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNum))
        {
            invoice.Warnings.Add("Lipseste numarul facturii.");
        }

        if (DateOnly.TryParseExact(row.IssueDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var issueDate))
        {
            invoice.IssueDate = issueDate;
        }
        else
        {
            invoice.Warnings.Add("Dată emitere invalidă.");
        }

        if (string.IsNullOrWhiteSpace(row.TaxDate))
        {
            invoice.TaxDate = null;
        }
        else if (DateOnly.TryParseExact(row.TaxDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var taxDate))
        {
            invoice.TaxDate = taxDate;
        }
        else
        {
            invoice.Warnings.Add("Dată exigibilitate invalidă.");
        }

        if (string.IsNullOrWhiteSpace(row.DeliveryDate))
        {
            invoice.DeliveryDate = null;
        }
        else if (DateOnly.TryParseExact(row.DeliveryDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var deliveryDate))
        {
            invoice.DeliveryDate = deliveryDate;
        }
        else
        {
            invoice.Warnings.Add("Dată livrare invalidă.");
        }

        if (string.IsNullOrWhiteSpace(row.DueDate))
        {
            invoice.DueDate = null;
        }
        else if (DateOnly.TryParseExact(row.DueDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var dueDate))
        {
            invoice.DueDate = dueDate;
        }
        else
        {
            invoice.Warnings.Add("Dată scadență invalidă.");
        }

        var validInvoiceTypes = new[] { "380", "381", "384", "389", "751" };
        invoice.InvoiceType = row.InvoiceType;
        if (!validInvoiceTypes.Contains(row.InvoiceType))
        {
            invoice.Warnings.Add($"Tip factură invalid '{row.InvoiceType}'.");
        }

        if (DateOnly.TryParseExact(row.SelectionDate, "dd.MM.yyyy", culture, DateTimeStyles.None, out var selectionDate))
        {
            invoice.SelectionDate = selectionDate;
        }
        else
        {
            invoice.Warnings.Add($"Dată selecție invalidă.");
        }
    }

    private static void ParseVatFields(InvoiceRow row, VatQuotaModel vatQuota)
    {
        var culture = new CultureInfo("ro-RO");

        if (decimal.TryParse(row.VatQuota, NumberStyles.Number, culture, out var vatQuotaValue))
        {
            vatQuota.VatQuota = vatQuotaValue;
        }
        else
        {
            vatQuota.Warnings.Add("Cotă TVA invalidă.");
        }

        if (decimal.TryParse(row.BaseValue, NumberStyles.Number, culture, out var baseValue))
        {
            vatQuota.BaseValue = baseValue;
        }
        else
        {
            vatQuota.Warnings.Add("Bază TVA invalidă.");
        }

        if (decimal.TryParse(row.VatValue, NumberStyles.Number, culture, out var vatValue))
        {
            vatQuota.VatValue = vatValue;
        }
        else
        {
            vatQuota.Warnings.Add("Valoare TVA invalidă.");
        }
    }
}

public class InvoiceModel
{
    public int Page { get; set; }
    public int PageIndex { get; set; }
    public int GlobalIndex { get; set; }
    public string UploadId { get; set; }

    public DateOnly UploadDate { get; set; }
    public string SellerFiscal { get; set; }
    public string SellerName { get; set; }
    public string BuyerFiscal { get; set; }
    public string BuyerName { get; set; }
    public string InvoiceNum { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? TaxDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string InvoiceType { get; set; }
    public DateOnly SelectionDate { get; set; }
    public List<VatQuotaModel> VatQuotas { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class VatQuotaModel
{
    public int Page { get; set; }
    public int PageIndex { get; set; }
    public int GlobalIndex { get; set; }

    public decimal VatQuota { get; set; }
    public decimal BaseValue { get; set; }
    public decimal VatValue { get; set; }

    public List<string> Warnings { get; set; } = new();
}
