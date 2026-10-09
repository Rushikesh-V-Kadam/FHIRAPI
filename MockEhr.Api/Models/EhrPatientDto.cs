namespace MockEhr.Api.Models;

/// <summary>
/// A patient, as the EHR returns it from GET api/patients/{patientId}. Plain JSON (camelCase), not FHIR.
/// The FHIR API turns it into a US Core Patient.
/// </summary>
public class EhrPatientDto
{
    /// <summary>EHR patient id. The same id is used in every gateway request.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Medical record number (becomes Patient.identifier, type MR).</summary>
    /// <example>MRN-100001</example>
    public string? Mrn { get; set; }

    /// <summary>Given name.</summary>
    /// <example>Robert</example>
    public string? FirstName { get; set; }

    /// <summary>Middle name, if any.</summary>
    /// <example>James</example>
    public string? MiddleName { get; set; }

    /// <summary>Family name.</summary>
    /// <example>Hayes</example>
    public string? LastName { get; set; }

    /// <summary>male | female | other | unknown (m / f / o are also understood).</summary>
    /// <example>male</example>
    public string? Gender { get; set; }

    /// <summary>Date of birth, yyyy-MM-dd.</summary>
    /// <example>1959-03-14</example>
    public string? DateOfBirth { get; set; }

    /// <summary>Home address.</summary>
    public EhrAddress? Address { get; set; }

    /// <summary>Home phone.</summary>
    /// <example>214-555-0101</example>
    public string? Phone { get; set; }

    /// <summary>Email address.</summary>
    /// <example>robert.hayes@example.org</example>
    public string? Email { get; set; }

    /// <summary>Race as a CDC race and ethnicity code (codeSystem CDCREC), e.g. 2106-3 White.</summary>
    public EhrCode? Race { get; set; }

    /// <summary>Ethnicity as a CDC race and ethnicity code (codeSystem CDCREC), e.g. 2186-5 Not Hispanic or Latino.</summary>
    public EhrCode? Ethnicity { get; set; }

    /// <summary>false = the patient record is inactive. Empty = true.</summary>
    /// <example>true</example>
    public bool? Active { get; set; }
}
