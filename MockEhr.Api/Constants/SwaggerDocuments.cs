namespace MockEhr.Api.Constants;

/// <summary>
/// Names of the two Swagger documents of the mock EHR API. The name is part of the URL of the JSON file:
/// /swagger/{name}/swagger.json. Pick one in Swagger UI with "Select a definition" (top right).
/// </summary>
public static class SwaggerDocuments
{
    /// <summary>Every endpoint of the mock (sections 1 to 5): /swagger/v1/swagger.json.</summary>
    public const string Everything = "v1";

    /// <summary>
    /// Only what the client's real EHR must provide (sections 1 to 3): /swagger/ehr-team/swagger.json.
    /// This is the file to give to the EHR team.
    /// </summary>
    public const string EhrTeam = "ehr-team";
}
