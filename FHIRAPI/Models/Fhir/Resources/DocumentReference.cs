using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR DocumentReference (US Core): a clinical document such as a note or an X-ray report.
/// The file itself is either embedded or linked to /fhir/r4/Binary/{documentId}.
/// </summary>
public class DocumentReference : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "DocumentReference";

    public List<Identifier> Identifier { get; set; } = new();
    public string Status { get; set; } = "current";
    /// <summary>Document type (LOINC).</summary>
    public CodeableConcept? Type { get; set; }
    public List<CodeableConcept> Category { get; set; } = new();
    public ResourceReference? Subject { get; set; }
    public string? Date { get; set; }
    public List<ResourceReference> Author { get; set; } = new();
    public string? Description { get; set; }
    public List<DocumentReferenceContent> Content { get; set; } = new();
}
