using FHIRAPI.Configuration;
using FHIRAPI.Constants;
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
    private readonly FhirMappingSettings _settings;

    /// <summary>Created by dependency injection.</summary>
    public AdministrativeMapper(CodeSystemMapper codes, FhirMappingSettings settings)
    {
        _codes = codes;
        _settings = settings;
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
            patient.Identifier.Add(new Identifier(_settings.MrnSystem, source.Mrn)
            {
                Type = new CodeableConcept(CodeSystems.V2IdentifierType, "MR", "Medical record number")
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

    /// <summary>EHR insurance -> CRD Coverage (member id, payer id, plan, group, period).</summary>
    public Coverage ToCoverage(EhrInsuranceDto source)
    {
        var coverage = new Coverage
        {
            Id = source.CoverageId,
            Status = string.IsNullOrEmpty(source.Status) ? "active" : source.Status.ToLowerInvariant(),
            SubscriberId = source.MemberId,
            Beneficiary = new ResourceReference("Patient", source.PatientId),
            Relationship = CodeSystemMapper.Relationship(source.RelationshipToSubscriber),
            Type = string.IsNullOrEmpty(source.CoverageType) ? null : new CodeableConcept(CodeSystems.ActCode, source.CoverageType)
        };
        coverage.AddProfile(FhirProfiles.CrdCoverage);

        if (source.EffectiveDate != null || source.TerminationDate != null)
            coverage.Period = new Period { Start = source.EffectiveDate, End = source.TerminationDate };

        coverage.Identifier.Add(new Identifier(_settings.MemberIdSystem, source.MemberId)
        {
            Type = new CodeableConcept(CodeSystems.V2IdentifierType, "MB", "Member Number")
        });

        // The Payer Gateway routes to the right payer using this payer id
        coverage.Payor.Add(new ResourceReference
        {
            Identifier = new Identifier(_settings.PayerIdSystem, source.PayerId),
            Display = source.PayerName
        });

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

        if (!string.IsNullOrEmpty(source.Npi))
            practitioner.Identifier.Add(new Identifier(CodeSystems.Npi, source.Npi));

        var name = new HumanName { Family = source.LastName };
        AddIfPresent(name.Given, source.FirstName);
        practitioner.Name.Add(name);
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

    /// <summary>EHR location -> US Core Location (place-of-service code as the type).</summary>
    public Location ToLocation(EhrLocationDto source)
    {
        var location = new Location
        {
            Id = source.LocationId,
            Status = "active",
            Name = source.Name,
            Address = ToAddress(source.Address),
            ManagingOrganization = string.IsNullOrEmpty(source.OrganizationId) ? null : new ResourceReference("Organization", source.OrganizationId)
        };
        location.AddProfile(FhirProfiles.Location);

        if (!string.IsNullOrEmpty(source.PlaceOfServiceCode))
            location.Type.Add(new CodeableConcept(CodeSystems.PlaceOfService, source.PlaceOfServiceCode));
        return location;
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
