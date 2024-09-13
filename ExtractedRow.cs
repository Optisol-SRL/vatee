using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vatee;

public class ExtractedRow
{
    public int Page { get; set; }
    public string Index { get; set; }
    public string UploadDate { get; set; }
    public string SellerFiscal { get; set; }
    public string SellerName { get; set; }
    public string BuyerFiscal { get; set; }
    public string BuyerName { get; set; }
    public string InvoiceNum { get; set; }
    public string IssueDate { get; set; }
    public string TaxDate { get; set; }
    public string DeliveryDate { get; set; }
    public string DueDate { get; set; }
    public string InvoiceType { get; set; }
    public string SelectionDate { get; set; }
    public string VatQuota { get; set; }
    public string BaseValue { get; set; }
    public string VatValue { get; set; }

    public List<string> GetInvoiceFields => new()
    {
        Index, UploadDate, SellerFiscal, SellerName, BuyerFiscal, BuyerName, InvoiceNum, IssueDate, TaxDate,
        DeliveryDate, DueDate, InvoiceType, SelectionDate
    };

    public List<string> GetVatFields => new() { VatQuota, BaseValue, VatValue };

    public bool IsEmpty => GetInvoiceFields.Union(GetVatFields).All(string.IsNullOrWhiteSpace);

    public bool IsVatOnly => GetInvoiceFields.All(string.IsNullOrWhiteSpace)
                             && GetVatFields.Any(x => !string.IsNullOrWhiteSpace(x));
}



