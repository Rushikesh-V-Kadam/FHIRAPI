namespace MockEhr.Api.Models;

/// <summary>
/// The payer's decision for one line (one order) of a prior authorization request.
/// </summary>
public class PriorAuthLineDto
{
    /// <summary>Line number in the request (1, 2 ...).</summary>
    /// <example>1</example>
    public int Sequence { get; set; }

    /// <summary>Order of this line.</summary>
    /// <example>ord-1-hd</example>
    public string? OrderId { get; set; }

    /// <summary>Code of the service, supply or drug.</summary>
    /// <example>90935</example>
    public string? Code { get; set; }

    /// <summary>Code system of the code (URI).</summary>
    /// <example>http://www.ama-assn.org/go/cpt</example>
    public string? CodeSystem { get; set; }

    /// <summary>approved | partially-approved | denied | pended | not-required | cancelled | contact-payer | modified.</summary>
    /// <example>approved</example>
    public string Decision { get; set; } = "pended";

    /// <summary>X12 278 review action code: A1 certified in total, A2 certified partial, A3 not certified, A4 pended, A6 modified, C cancelled, CT contact payer, NA no action required.</summary>
    /// <example>A1</example>
    public string? X12ActionCode { get; set; }

    /// <summary>Units asked for.</summary>
    /// <example>39</example>
    public decimal? RequestedQuantity { get; set; }

    /// <summary>Units approved.</summary>
    /// <example>39</example>
    public decimal? ApprovedQuantity { get; set; }

    /// <summary>Reason text of the payer.</summary>
    /// <example>Line 1: Certified in total.</example>
    public string? Reason { get; set; }

    /// <summary>First day the line is authorized, yyyy-MM-dd.</summary>
    /// <example>2026-10-05</example>
    public string? ValidFrom { get; set; }

    /// <summary>Last day the line is authorized, yyyy-MM-dd.</summary>
    /// <example>2027-04-03</example>
    public string? ValidTo { get; set; }
}
