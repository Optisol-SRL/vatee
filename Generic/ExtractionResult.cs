using System.Collections.Generic;

namespace Vatee;

public class ExtractionResult<T>
    where T : class
{
    public ExtractionStatus Status { get; set; }
    public List<T> Rows { get; set; } = new();
}

public enum ExtractionStatus
{
    Success,
    NoTemplateMatch,
    NoEntries,
}
