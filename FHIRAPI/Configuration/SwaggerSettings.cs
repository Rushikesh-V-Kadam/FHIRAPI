namespace FHIRAPI.Configuration;

/// <summary>
/// What Swagger UI shows (appsettings section "Swagger"). Only the Swagger page changes: hidden endpoints keep
/// working, because the Payer Gateway and DTR apps call them.
/// </summary>
public class SwaggerSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "Swagger";

    /// <summary>
    /// true = show the sections "DTR forms (QuestionnaireResponse)" and "SMART launch context" in Swagger UI.
    /// false (the default for now) = hide them; they are not needed yet and will be enabled later.
    /// </summary>
    public bool ShowDtrSections { get; set; }
}
