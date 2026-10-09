using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// The PAS Subscription the gateway created at a payer for one provider organization = one row of table PA_PAYER_SUBSCRIPTION.
/// Body of PUT api/prior-auth-data/payer-subscriptions/{payerName}/{organizationNpi}; returned by GET of the same path.
/// </summary>
public class PayerSubscriptionRow
{
    /// <summary>Key, part 1 (the same value as in the URL; upper / lower case does not matter). Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>Key, part 2: NPI of the provider organization (the same value as in the URL). Column ORGANIZATION_NPI, VARCHAR2(20).</summary>
    /// <example>1999999992</example>
    public string OrganizationNpi { get; set; } = string.Empty;

    /// <summary>The payer's id of the subscription. Column SUBSCRIPTION_ID, VARCHAR2(100).</summary>
    /// <example>b3c70904a6ae49e5b154bf6ad4f8666a</example>
    public string? SubscriptionId { get; set; }

    /// <summary>active | error. Column STATUS, VARCHAR2(10).</summary>
    /// <example>active</example>
    public string Status { get; set; } = "active";

    /// <summary>When the subscription was created (UTC, ISO-8601 with Z). Column CREATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:36:17.5000000Z</example>
    [Required]
    public DateTime CreatedAt { get; set; }
}
