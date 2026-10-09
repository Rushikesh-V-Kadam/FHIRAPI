using System.Text.Json;
using FHIRAPI.Constants;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Mapping;

/// <summary>
/// FHIR -> EHR for DTR forms: turns a FHIR QuestionnaireResponse into the EHR's flat form record
/// (one row per answer), so the form can be shown in the patient chart. Registered as a singleton.
/// </summary>
public class QuestionnaireResponseMapper
{
    /// <summary>
    /// Builds the EHR form record.
    /// </summary>
    /// <param name="form">The parsed QuestionnaireResponse.</param>
    /// <param name="fullJson">The complete FHIR JSON; the EHR keeps it unchanged in FhirJson.</param>
    /// <param name="patientId">Patient of the form.</param>
    /// <param name="orderId">Order the form was filled for (from qr-context), if any.</param>
    /// <param name="coverageId">Coverage the form was filled for (from qr-context), if any.</param>
    /// <param name="ehrResponseId">Id the EHR gave this form earlier; empty for a new form.</param>
    public EhrQuestionnaireResponseDto ToEhr(QuestionnaireResponse form, string fullJson, string patientId,
        string? orderId, string? coverageId, string? ehrResponseId)
    {
        // "http://example.org/Questionnaire/cpap|1.0" -> url + version
        var canonicalParts = (form.Questionnaire ?? string.Empty).Split('|');

        var record = new EhrQuestionnaireResponseDto
        {
            ResponseId = ehrResponseId ?? string.Empty,
            PatientId = patientId,
            OrderId = orderId,
            CoverageId = coverageId,
            QuestionnaireUrl = canonicalParts[0],
            QuestionnaireVersion = canonicalParts.Length > 1 ? canonicalParts[1] : null,
            Status = form.Status,
            AuthoredDateTime = form.Authored,
            AuthorPractitionerId = form.Author?.GetId(),
            FhirJson = fullJson
        };

        AddAnswers(form.Item, record.Answers);
        return record;
    }

    /// <summary>Walks the question tree (groups and nested questions) and adds one EHR row per answer.</summary>
    private static void AddAnswers(List<QuestionnaireResponseItem> items, List<EhrQuestionnaireAnswerDto> output)
    {
        foreach (var item in items)
        {
            foreach (var answer in item.Answer)
            {
                var (valueType, value) = GetValue(answer);
                output.Add(new EhrQuestionnaireAnswerDto
                {
                    LinkId = item.LinkId,
                    QuestionText = item.Text,
                    ValueType = valueType,
                    Value = value,
                    Origin = GetOrigin(answer),
                    AnsweredByPractitionerId = GetOriginAuthor(answer)
                });

                // questions nested under this answer
                AddAnswers(answer.Item, output);
            }

            // questions inside this group
            AddAnswers(item.Item, output);
        }
    }

    /// <summary>Returns the EHR value type name and the value as JSON.</summary>
    private static (string ValueType, JsonElement Value) GetValue(QuestionnaireResponseAnswer answer)
    {
        if (answer.ValueBoolean != null) return ("boolean", JsonSerializer.SerializeToElement(answer.ValueBoolean));
        if (answer.ValueInteger != null) return ("integer", JsonSerializer.SerializeToElement(answer.ValueInteger));
        if (answer.ValueDecimal != null) return ("decimal", JsonSerializer.SerializeToElement(answer.ValueDecimal));
        if (answer.ValueDate != null) return ("date", JsonSerializer.SerializeToElement(answer.ValueDate));
        if (answer.ValueDateTime != null) return ("datetime", JsonSerializer.SerializeToElement(answer.ValueDateTime));
        if (answer.ValueQuantity != null) return ("decimal", JsonSerializer.SerializeToElement(answer.ValueQuantity.Value));
        if (answer.ValueCoding != null)
        {
            var coding = new { code = answer.ValueCoding.Code, system = answer.ValueCoding.System, display = answer.ValueCoding.Display };
            return ("coding", JsonSerializer.SerializeToElement(coding));
        }
        return ("string", JsonSerializer.SerializeToElement(answer.ValueString ?? string.Empty));
    }

    /// <summary>DTR information-origin "source": auto | override | manual.</summary>
    private static string? GetOrigin(QuestionnaireResponseAnswer answer)
    {
        var origin = answer.Extension.FirstOrDefault(e => e.Url == DtrExtensions.InformationOrigin);
        return origin?.Children.FirstOrDefault(e => e.Url == "source")?.ValueCode;
    }

    /// <summary>DTR information-origin "author" -> practitioner id who changed the answer, if given.</summary>
    private static string? GetOriginAuthor(QuestionnaireResponseAnswer answer)
    {
        var origin = answer.Extension.FirstOrDefault(e => e.Url == DtrExtensions.InformationOrigin);
        var author = origin?.Children.FirstOrDefault(e => e.Url == "author");
        var practitioner = author?.Children.FirstOrDefault(e => e.Url == "practitioner");
        return practitioner?.ValueReference?.GetId();
    }
}
