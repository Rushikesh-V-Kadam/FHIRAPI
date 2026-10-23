using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Mapping;

/// <summary>
/// EHR -> FHIR for administrative data: Patient, Coverage, Practitioner, PractitionerRole, Organization, Location.
/// Registered as a singleton.
/// </summary>
public class AdministrativeMapper
{
    private readonly CodeSystemMapper _codes;

    /// <summary>Created by dependency injection.</summary>
    public AdministrativeMapper(CodeSystemMapper codes)
    {
        _codes = codes;
    }

    /// <summary>EHR patient -> US Core Patient (MRN, name, contact, address, race, ethnicity).</summary>
    public Patient ToPatient(EhrPatientDto source)
    {
        var patient = new Patient
        {
            Id = source.PatientId,
            Active = source.Active ?? true,
            Gender = CodeSystemMapper.Gender(source.Gender),
            BirthDate = source.DateOfBirth
        };
        patient.AddProfile(FhirProfiles.Patient);

        if (!string.IsNullOrEmpty(source.Mrn))
        {
            // CLIENT-EHR: the client has no identifier system for its MRNs, so the MRN is sent without a "system"
            // (Identifier.system is 0..1 in FHIR R4). TODO: add the system if a real payer requires it (US Core asks for one).
            patient.Identifier.Add(new Identifier
            {
                Type = new CodeableConcept(CodeSystems.V2IdentifierType, "MR", "Medical record number"),
                Value = source.Mrn
            });
        }

        var name = new HumanName { Use = "official", Family = source.LastName };
        AddIfPresent(name.Given, source.FirstName);
        AddIfPresent(name.Given, source.MiddleName);
        patient.Name.Add(name);

        if (!string.IsNullOrEmpty(source.Phone))
            patient.Telecom.Add(new ContactPoint { System = "phone", Value = source.Phone, Use = "home" });
        if (!string.IsNullOrEmpty(source.Email))
            patient.Telecom.Add(new ContactPoint { System = "email", Value = source.Email });

        var address = ToAddress(source.Address);
        if (address != null) patient.Address.Add(address);

        if (source.Race != null) patient.Extension.Add(RaceOrEthnicity(FhirProfiles.UsCoreRace, source.Race));
        if (source.Ethnicity != null) patient.Extension.Add(RaceOrEthnicity(FhirProfiles.UsCoreEthnicity, source.Ethnicity));
        return patient;
    }

    /// <summary>
    /// EHR insurance -> CRD Coverage (member id, payer id, plan, group, period).
    /// Coverage.id is the EHR's coverage id (unique in the EHR). The policy number (member id) is optional.
    /// </summary>
    public Coverage ToCoverage(EhrInsuranceDto source)
    {
        var coverage = new Coverage
        {
            // CLIENT-EHR: the client's own unique coverage id
            Id = string.IsNullOrWhiteSpace(source.CoverageId) ? null : source.CoverageId.Trim(),
            Status = string.IsNullOrEmpty(source.Status) ? "active" : source.Status.ToLowerInvariant(),
            SubscriberId = string.IsNullOrWhiteSpace(source.MemberId) ? null : source.MemberId,
            Beneficiary = new ResourceReference("Patient", source.PatientId),
            Relationship = CodeSystemMapper.Relationship(source.RelationshipToSubscriber),
            Type = string.IsNullOrEmpty(source.CoverageType) ? null : new CodeableConcept(CodeSystems.ActCode, source.CoverageType)
        };
        coverage.AddProfile(FhirProfiles.CrdCoverage);

        if (source.EffectiveDate != null || source.TerminationDate != null)
            coverage.Period = new Period { Start = source.EffectiveDate, End = source.TerminationDate };

        // Member id (identifier of type MB), sent without a "system" (payers do not give one; Identifier.system is 0..1).
        // An insurance without member id gets no identifier (an identifier without value is not valid FHIR).
        if (!string.IsNullOrWhiteSpace(source.MemberId))
        {
            var memberIdentifier = new Identifier
            {
                Type = new CodeableConcept(CodeSystems.V2IdentifierType, "MB", "Member Number"),
                Value = source.MemberId
            };
            coverage.Identifier.Add(memberIdentifier);
        }

        // The payer: its name, and the payer id the EHR keeps with the insurance (sent by the Payer Gateway as the
        // payer id in PAS and CDex), sent without a "system". An insurance without payer id gets only the name.
        var payor = new ResourceReference { Display = source.PayerName };
        if (!string.IsNullOrWhiteSpace(source.PayerId))
        {
            payor.Identifier = new Identifier { Value = source.PayerId };
        }
        coverage.Payor.Add(payor);

        if (!string.IsNullOrEmpty(source.PlanId))
        {
            coverage.Class.Add(new CoverageClass
            {
                Type = new CodeableConcept(CodeSystems.CoverageClass, "plan"),
                Value = source.PlanId,
                Name = source.PlanName
            });
        }
        if (!string.IsNullOrEmpty(source.GroupNumber))
        {
            coverage.Class.Add(new CoverageClass
            {
                Type = new CodeableConcept(CodeSystems.CoverageClass, "group"),
                Value = source.GroupNumber
            });
        }
        return coverage;
    }

    /// <summary>EHR practitioner -> US Core Practitioner (NPI + name).</summary>
    public Practitioner ToPractitioner(EhrPractitionerDto source)
    {
        var practitioner = new Practitioner { Id = source.PractitionerId };
        practitioner.AddProfile(FhirProfiles.Practitioner);

        // CLIENT-EHR: the client's EHR has no practitioner id; it finds a practitioner by NPI, so the NPI is the id
        // (GET api/practitioners/{npi}). When the answer has no separate npi field, the id is used as the NPI, but only
        // when it is a valid NPI (10 digits, correct check digit), so an ordinary EHR id is never sent as an NPI.
        string? npi = source.Npi;
        if (string.IsNullOrEmpty(npi) && NpiNumbers.IsValid(source.PractitionerId))
        {
            npi = source.PractitionerId;
        }
        if (!string.IsNullOrEmpty(npi))
        {
            practitioner.Identifier.Add(new Identifier(CodeSystems.Npi, npi));
        }

        // Practitioner.name is 0..* in R4: first / last name when the EHR splits them; else the one-field name of the
        // client's EHR as HumanName.text (not split, so no part is guessed); else no name.
        if (!string.IsNullOrEmpty(source.LastName) || !string.IsNullOrEmpty(source.FirstName))
        {
            var name = new HumanName { Family = source.LastName };
            AddIfPresent(name.Given, source.FirstName);
            practitioner.Name.Add(name);
        }
        else if (!string.IsNullOrWhiteSpace(source.Name))
        {
            // CLIENT-EHR: the client's EHR gives the name in one field.
            practitioner.Name.Add(new HumanName { Text = source.Name.Trim() });
        }
        return practitioner;
    }

    /// <summary>EHR practitioner -> US Core PractitionerRole with id "role-{practitionerId}" (organization + specialty).</summary>
    public PractitionerRole ToPractitionerRole(EhrPractitionerDto source)
    {
        var role = new PractitionerRole
        {
            Id = $"role-{source.PractitionerId}",
            Practitioner = new ResourceReference("Practitioner", source.PractitionerId),
            Organization = string.IsNullOrEmpty(source.OrganizationId) ? null : new ResourceReference("Organization", source.OrganizationId)
        };
        role.AddProfile(FhirProfiles.PractitionerRole);

        var specialty = _codes.Concept(source.Specialty, defaultSystem: "NUCC");
        if (specialty != null) role.Specialty.Add(specialty);
        return role;
    }

    /// <summary>EHR organization -> US Core Organization (NPI first, then tax id).</summary>
    public Organization ToOrganization(EhrOrganizationDto source)
    {
        var organization = new Organization { Id = source.OrganizationId, Active = source.Active ?? true, Name = source.Name };
        organization.AddProfile(FhirProfiles.Organization);

        if (!string.IsNullOrEmpty(source.Npi))
            organization.Identifier.Add(new Identifier(CodeSystems.Npi, source.Npi));
        if (!string.IsNullOrEmpty(source.TaxId))
            organization.Identifier.Add(new Identifier(CodeSystems.TaxId, source.TaxId));
        if (!string.IsNullOrEmpty(source.Phone))
            organization.Telecom.Add(new ContactPoint { System = "phone", Value = source.Phone });

        var address = ToAddress(source.Address);
        if (address != null) organization.Address.Add(address);
        return organization;
    }

    /// <summary>
    /// EHR location -> US Core Location (place-of-service code as the type, the facility NPI as identifier).
    /// managingOrganization is the facility Organization of this location (Organization/facility-{locationId}), which
    /// carries the NPI and points to the parent organization (see ToFacilityOrganization).
    /// </summary>
    public Location ToLocation(EhrLocationDto source)
    {
        var location = new Location
        {
            Id = source.LocationId,
            Status = "active",
            Name = source.Name,
            Address = ToAddress(source.Address),
            // CLIENT-EHR: points to the facility Organization (with the facility NPI), not to the parent organization
            ManagingOrganization = new ResourceReference("Organization", FacilityIds.ToOrganizationId(source.LocationId))
        };
        location.AddProfile(FhirProfiles.Location);

        if (!string.IsNullOrEmpty(source.Npi))
        {
            location.Identifier.Add(new Identifier(CodeSystems.Npi, source.Npi));
        }

        if (!string.IsNullOrEmpty(source.PlaceOfServiceCode))
            location.Type.Add(new CodeableConcept(CodeSystems.PlaceOfService, source.PlaceOfServiceCode));
        return location;
    }

    /// <summary>
    /// EHR location (+ its parent organization) -> facility Organization (Organization/facility-{locationId}).
    /// The client keeps each facility's NPI, name and address on the location and only the tax id on the parent
    /// organization. The facility Organization has: the location's name and address, the facility NPI, the parent's
    /// tax id (facilities bill under the parent's tax id), and partOf = the parent organization.
    /// It is what a PAS request uses as Claim.provider. parent may be null (no parent, or not found in the EHR).
    /// </summary>
    public Organization ToFacilityOrganization(EhrLocationDto location, EhrOrganizationDto? parent)
    {
        // CLIENT-EHR: the facility NPI comes from the location; the parent organization gives only its tax id.
        Organization organization = new Organization();
        organization.Id = FacilityIds.ToOrganizationId(location.LocationId);
        organization.Active = true;
        organization.Name = location.Name;
        organization.AddProfile(FhirProfiles.Organization);

        if (!string.IsNullOrEmpty(location.Npi))
        {
            organization.Identifier.Add(new Identifier(CodeSystems.Npi, location.Npi));
        }
        if (parent != null && !string.IsNullOrEmpty(parent.TaxId))
        {
            organization.Identifier.Add(new Identifier(CodeSystems.TaxId, parent.TaxId));
        }

        Address? address = ToAddress(location.Address);
        if (address != null)
        {
            organization.Address.Add(address);
        }
        if (!string.IsNullOrEmpty(location.OrganizationId))
        {
            organization.PartOf = new ResourceReference("Organization", location.OrganizationId);
        }
        return organization;
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>EHR address -> FHIR Address, or null.</summary>
    public static Address? ToAddress(EhrAddress? source)
    {
        if (source == null) return null;
        var address = new Address { City = source.City, State = source.State, PostalCode = source.PostalCode, Country = source.Country };
        AddIfPresent(address.Line, source.Line1);
        AddIfPresent(address.Line, source.Line2);
        return address;
    }

    /// <summary>US Core race / ethnicity extension: ombCategory coding + text.</summary>
    private static Extension RaceOrEthnicity(string url, EhrCode code)
    {
        var extension = new Extension { Url = url };
        extension.Children.Add(new Extension
        {
            Url = "ombCategory",
            ValueCoding = new Coding(CodeSystems.OmbRaceEthnicity, code.Code, code.Display)
        });
        extension.Children.Add(new Extension { Url = "text", ValueString = code.Display ?? code.Code });
        return extension;
    }

    /// <summary>Adds the value to the list when it is not empty.</summary>
    private static void AddIfPresent(List<string> list, string? value)
    {
        if (!string.IsNullOrEmpty(value)) list.Add(value);
    }
}
