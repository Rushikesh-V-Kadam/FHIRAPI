namespace MockEhr.Api.Models;

/// <summary>
/// An order, as the EHR returns it from GET api/orders/{orderId}. Plain JSON (camelCase), not FHIR.
/// orderType decides the FHIR resource the FHIR API builds: service = ServiceRequest, device = DeviceRequest,
/// medication = MedicationRequest. The payer decides on these values whether prior authorization is needed.
/// </summary>
public class EhrOrderDto
{
    /// <summary>EHR order id. Used in orderIds of every gateway request.</summary>
    /// <example>ord-1-hd</example>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>service | device | medication.</summary>
    /// <example>service</example>
    public string OrderType { get; set; } = "service";

    /// <summary>Patient the order is for.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Encounter in which the order was written.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>draft | active (signed) | on-hold | cancelled | completed | entered-in-error.</summary>
    /// <example>draft</example>
    public string Status { get; set; } = "draft";

    /// <summary>What is ordered: procedure, supply or drug code.</summary>
    /// <example>90935</example>
    public string Code { get; set; } = string.Empty;

    /// <summary>Code system of the code: CPT, HCPCS or RXNORM.</summary>
    /// <example>CPT</example>
    public string CodeSystem { get; set; } = string.Empty;

    /// <summary>Text of the code.</summary>
    /// <example>In-center hemodialysis, 3 times a week</example>
    public string? CodeDisplay { get; set; }

    /// <summary>Number of units / treatments asked for.</summary>
    /// <example>39</example>
    public decimal? Quantity { get; set; }

    /// <summary>When the order was written (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-09-28T10:00:00Z</example>
    public string? OrderedDateTime { get; set; }

    /// <summary>First date of service, yyyy-MM-dd.</summary>
    /// <example>2026-10-05</example>
    public string? RequestedStartDate { get; set; }

    /// <summary>Last date of service, yyyy-MM-dd.</summary>
    /// <example>2027-01-03</example>
    public string? RequestedEndDate { get; set; }

    /// <summary>Clinician who ordered (the attending nephrologist).</summary>
    /// <example>prac-1</example>
    public string? OrderingPractitionerId { get; set; }

    /// <summary>Organization that will give the service.</summary>
    /// <example>org-1</example>
    public string? PerformerOrganizationId { get; set; }

    /// <summary>Clinician who will give the service, if known.</summary>
    /// <example>prac-1</example>
    public string? PerformerPractitionerId { get; set; }

    /// <summary>CMS place of service code, e.g. 65 = ESRD treatment facility, 12 = home.</summary>
    /// <example>65</example>
    public string? PlaceOfServiceCode { get; set; }

    /// <summary>Location where the service is given.</summary>
    /// <example>loc-main</example>
    public string? LocationId { get; set; }

    /// <summary>Diagnoses the order is for (codeSystem ICD10CM), most important first.</summary>
    public List<EhrCode> Diagnoses { get; set; } = new();

    /// <summary>Insurance to bill. Empty = the patient's first active insurance.</summary>
    /// <example>cov-1</example>
    public string? CoverageId { get; set; }

    /// <summary>routine | urgent | asap | stat. Empty = routine.</summary>
    /// <example>routine</example>
    public string? Priority { get; set; }

    /// <summary>Free-text note of the clinician.</summary>
    /// <example>Continue HD 3x/week, 4 hours, left arm AV fistula.</example>
    public string? Notes { get; set; }

    /// <summary>Only for orderType = medication.</summary>
    public EhrMedicationDetailsDto? Medication { get; set; }

    // ------------------------------------------------------------ authorization request form header

    /// <summary>new | renewal (form: Request Type).</summary>
    /// <example>new</example>
    public string RequestType { get; set; } = "new";

    /// <summary>permanent | transient (form: Patient Type; transient = visiting patient).</summary>
    /// <example>permanent</example>
    public string PatientType { get; set; } = "permanent";

    /// <summary>Authorization number being renewed (form: Previous Authorization #).</summary>
    /// <example>PA20260706451203</example>
    public string? PreviousAuthorizationNumber { get; set; }

    /// <summary>First date of service of the previous authorization, yyyy-MM-dd (form: Previous DOS).</summary>
    /// <example>2026-07-06</example>
    public string? PreviousStartDate { get; set; }

    /// <summary>Last date of service of the previous authorization, yyyy-MM-dd (form: Previous DOS).</summary>
    /// <example>2026-10-04</example>
    public string? PreviousEndDate { get; set; }

    /// <summary>Treatments given under the previous authorization (form: # of Treatments next to Previous DOS).</summary>
    /// <example>39</example>
    public int? PreviousTreatments { get; set; }

    /// <summary>Physician who referred the patient (form: Referring Physician). The ordering practitioner is the attending nephrologist.</summary>
    /// <example>prac-4</example>
    public string? ReferringPractitionerId { get; set; }
}
