namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Who performed a Procedure.
/// </summary>
public class ProcedurePerformer
{
    public ResourceReference Actor { get; set; } = new();
}
