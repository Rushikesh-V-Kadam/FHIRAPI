using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;

namespace FHIRAPI.Helpers;

// CLIENT-EHR: the client's EHR has no coverage id, so the FHIR API builds one (no database, nothing stored).
/// <summary>
/// Builds the FHIR id of a Coverage (a patient's insurance), and reads the patient id back out of it.
///
/// Why: the EHR has no id for an insurance (there is no coverage id column). FHIR still needs one, because the
/// Coverage is pointed to as "Coverage/{id}" (PAS Claim.insurance, the DTR launch context, the qr-context of a
/// QuestionnaireResponse) and the Payer Gateway saves it with prior auths and DTR forms to find the same insurance later.
/// So the id is built from the insurance itself:
///     "cov-" + {patientId} + "-" + the first 16 hex characters of SHA-256("{payerId}|{policy number}")
///     e.g. cov-pat-1-3f9a1c0e2b7d4a91
///   - the same insurance always gets the same id (never a random value), so ids saved earlier keep working
///   - the patient id is in the id in plain text, so GET Coverage/{id} can ask the EHR for that patient's insurances
///     and find the one with this id - nothing has to be stored (no database table, no in-memory list), and it keeps
///     working after a restart
///   - the payer id is part of the hash, so the same policy number at two payers gives two ids
///   - the policy number is hashed, so it does not appear in URLs or logs
///   - 16 hex characters = 64 bits; they only have to tell apart the few insurances of one patient
/// FHIR ids are at most 64 characters of letters, digits, "-" and ".", so the patient id can be at most 43 characters.
/// The policy number is the member id of the insurance (EhrInsuranceDto.MemberId; on the request form "Policy #").
/// It is optional (Coverage.subscriberId is 0..1 in FHIR R4): an insurance without one is hashed with an empty policy
/// number. Only if one patient had two insurances at the same payer, both without a policy number, would they get the
/// same id (then the first one is returned).
/// Note: if the payer gives the patient a new policy number (e.g. at renewal), the insurance gets a new id.
/// </summary>
public static class CoverageIds
{
    /// <summary>Start of every Coverage id.</summary>
    private const string Prefix = "cov-";

    /// <summary>How many hex characters of the SHA-256 hash are used.</summary>
    private const int HashLength = 16;

    /// <summary>Longest FHIR id.</summary>
    private const int MaxIdLength = 64;

    /// <summary>Longest patient id that still fits: 64 - "cov-" - "-" - hash.</summary>
    private const int MaxPatientIdLength = MaxIdLength - 4 - 1 - HashLength;

    /// <summary>Characters FHIR allows in an id.</summary>
    private static readonly Regex FhirIdCharacters = new Regex("^[A-Za-z0-9\\-.]+$", RegexOptions.Compiled);

    /// <summary>
    /// The Coverage id of an insurance. The patient id is required; payer id and policy number are optional
    /// (an empty one is used as empty text, so the id is still the same on every call).
    /// Throws FhirException 422 when the patient id is missing or cannot be part of a FHIR id (too long or other
    /// characters), because then the insurance cannot be given an id.
    /// </summary>
    public static string Build(string? patientId, string? payerId, string? policyNumber)
    {
        if (string.IsNullOrWhiteSpace(patientId))
        {
            throw new FhirException(422, IssueTypes.Required,
                "An insurance from the EHR has no patientId, so it cannot be given a Coverage id.");
        }
        string patient = patientId.Trim();
        if (patient.Length > MaxPatientIdLength || !FhirIdCharacters.IsMatch(patient))
        {
            throw new FhirException(422, IssueTypes.Invalid,
                "Patient id " + patient + " cannot be part of a Coverage id: it must be at most " + MaxPatientIdLength
                + " characters of letters, digits, '-' and '.'.");
        }
        // Payer id and policy number are compared without case, so "abc123" and "ABC123" give the same id.
        string normalizedPayerId = string.Empty;
        if (!string.IsNullOrWhiteSpace(payerId))
        {
            normalizedPayerId = payerId.Trim().ToUpperInvariant();
        }
        string normalizedPolicyNumber = string.Empty;
        if (!string.IsNullOrWhiteSpace(policyNumber))
        {
            normalizedPolicyNumber = policyNumber.Trim().ToUpperInvariant();
        }
        string key = normalizedPayerId + "|" + normalizedPolicyNumber;

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        string hex = Convert.ToHexString(hash).ToLowerInvariant();
        return Prefix + patient + "-" + hex.Substring(0, HashLength);
    }

    /// <summary>
    /// The patient id inside a Coverage id ("cov-pat-1-3f9a1c0e2b7d4a91" -> "pat-1"), or null when the id does not
    /// have this form (then no insurance can have this id).
    /// </summary>
    public static string? FindPatientId(string coverageId)
    {
        if (!coverageId.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }
        int lastDash = coverageId.LastIndexOf('-');
        int patientLength = lastDash - Prefix.Length;
        if (patientLength <= 0 || coverageId.Length - lastDash - 1 != HashLength)
        {
            return null;
        }
        return coverageId.Substring(Prefix.Length, patientLength);
    }
}
