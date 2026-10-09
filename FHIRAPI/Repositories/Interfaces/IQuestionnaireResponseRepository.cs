namespace FHIRAPI.Repositories.Interfaces;

/// <summary>
/// Storage for DTR forms (QuestionnaireResponses).
/// Two implementations: EhrApiQuestionnaireResponseRepository (the EHR database, through the EHR data API) and
/// InMemoryQuestionnaireResponseRepository (local testing). Chosen by Storage:QuestionnaireResponseStore.
/// </summary>
public interface IQuestionnaireResponseRepository
{
    /// <summary>Returns the form with this id, or null.</summary>
    Task<QuestionnaireResponseRecord?> GetAsync(string id, CancellationToken ct);

    /// <summary>Returns all forms of a patient, newest first.</summary>
    Task<List<QuestionnaireResponseRecord>> GetByPatientAsync(string patientId, CancellationToken ct);

    /// <summary>Inserts the form, or updates it when the id already exists.</summary>
    Task SaveAsync(QuestionnaireResponseRecord record, CancellationToken ct);
}
