using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Patient (US Core Patient profile).
/// </summary>
public class Patient : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Patient";

    public List<Identifier> Identifier { get; set; } = new();
    public bool? Active { get; set; }
    public List<HumanName> Name { get; set; } = new();
    public List<ContactPoint> Telecom { get; set; } = new();
    /// <summary>male | female | other | unknown</summary>
    public string? Gender { get; set; }
    /// <summary>YYYY-MM-DD</summary>
    public string? BirthDate { get; set; }
    public List<Address> Address { get; set; } = new();
}
