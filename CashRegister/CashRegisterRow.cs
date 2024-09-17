using System;
using System.Collections.Generic;
using System.Linq;

namespace Vatee.CashRegister;

public class CashRegisterRow
{
    public int Page { get; set; }
    public string RegisterId { get; set; }
    public string ReportId { get; set; }
    public string ReportDate { get; set; }
    public string Vat19Base { get; set; }
    public string Vat19Value { get; set; }
    public string Vat9Base { get; set; }
    public string Vat9Value { get; set; }
    public string Vat5Base { get; set; }
    public string Vat5Value { get; set; }
    public string Vat0Base { get; set; }
    public string Vat0Value { get; set; }

    public string GetBaseByPercentage(int percentage) =>
        percentage switch
        {
            19 => Vat19Base,
            9 => Vat9Base,
            5 => Vat5Base,
            0 => Vat0Base,
            _ => throw new ArgumentOutOfRangeException(nameof(percentage), percentage, null),
        };

    public string GetValueByPercentage(int percentage) =>
        percentage switch
        {
            19 => Vat19Value,
            9 => Vat9Value,
            5 => Vat5Value,
            0 => Vat0Value,
            _ => throw new ArgumentOutOfRangeException(nameof(percentage), percentage, null),
        };

    public bool IsEmpty =>
        new List<string>
        {
            RegisterId,
            ReportId,
            ReportDate,
            Vat19Base,
            Vat19Value,
            Vat9Base,
            Vat9Value,
            Vat5Base,
            Vat5Value,
            Vat0Base,
            Vat0Value,
        }.All(x => string.IsNullOrWhiteSpace(x));
}
