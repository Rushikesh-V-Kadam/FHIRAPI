namespace FHIRAPI.Models.Fhir;

/// <summary>
/// One answer to a question. Only one value[x] is set.
/// </summary>
public class QuestionnaireResponseAnswer
{
    /// <summary>Holds the DTR information-origin extension (auto / override / manual).</summary>
    public List<Extension> Extension { get; set; } = new();

    public bool? ValueBoolean { get; set; }
    public decimal? ValueDecimal { get; set; }
    public int? ValueInteger { get; set; }
    public string? ValueDate { get; set; }
    public string? ValueDateTime { get; set; }
    public string? ValueString { get; set; }
    public Coding? ValueCoding { get; set; }
    public Quantity? ValueQuantity { get; set; }

    /// <summary>Questions nested under this answer.</summary>
    public List<QuestionnaireResponseItem> Item { get; set; } = new();
}
