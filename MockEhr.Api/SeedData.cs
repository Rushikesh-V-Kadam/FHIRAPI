using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api;

/// <summary>
/// Fictional DIALYSIS test data, modelled on the provider's paper "Authorization/Referral Request Form".
/// The data is in Data/TestData, one JSON file per resource (patients.json, insurances.json, orders.json ...), so it
/// can be read and changed without touching code. It is copied to the data folder on the first start and again by
/// POST /api/admin/reset. All names, ids, NPIs, authorization numbers and results are made up.
///
/// Patients and what each order shows with the mock payer (localhost:5080):
///   pat-1  Robert Hayes      RENEWAL of in-center HD and Retacrit; Parsabiv new; Zemplar no PA; Sensipar not covered
///   pat-2  Linda Morales     home PD: training course (PA only) + home dialysis (adaptive questionnaire)
///   pat-3  James Carter      NEW start: HD x52 (partial approval), Venofer approved, Aranesp pended (iron deficient)
///   pat-4  Susan Nguyen      TRANSIENT patient visiting the Lakeside unit, 90937 x9
///   pat-5  Michael Torres    URGENT dialysis for AKI (G0491), referred by the hospitalist
///   pat-6  Patricia Bell     home hemodialysis (NxStage, 90999) + training sessions x30 (partial, max 25)
///   pat-7  Thomas Reed       insured by a payer the gateway does not know -> "payer not configured"
///   pat-8  Angela Brooks     NEW start of in-center HD via AV graft + Retacrit new
///   pat-9  David Kim         home PD (CAPD) RENEWAL, diabetic
///   pat-10 Gloria Ramirez    Lakeside in-center HD RENEWAL + Venofer (iron deficient, TSAT 15%)
///   pat-11 William Johnson   TWO coverages (primary Gold PPO + secondary supplement); Mircera + Parsabiv
///   pat-12 Karen Mitchell    URGENT AKI dialysis after rhabdomyolysis, hospital discharge summary
///   pat-13 Joseph Lee        TRANSIENT visitor at Lakeside, 90937 x6
///   pat-14 Nancy Adams       coverage CANCELLED on 2026-09-30 -> no active coverage for the order
///   pat-15 Steven Clark      DEPENDENT on his wife's plan (subscriber Rachel Clark); home HD training + NxStage
///   CRD -> PAS without a questionnaire (the payer needs prior auth but no documentation, so there is no DTR step):
///   pat-16 Maria Gonzalez    AV fistula creation 36821 x1 -> approved; vein mapping report can go with PAS
///   pat-17 Charles Wilson    AV graft creation 36830 x1 -> approved
///   pat-18 Laura Bennett     PD catheter 49421 + PD training course 90989 -> two lines, both approved
///   pat-19 Henry Walker      home HD training 90993 x30 -> partially approved (plan limit 25 sessions)
/// </summary>
public static class SeedData
{
    /// <summary>Fills an empty EhrData with the test data files of the folder.</summary>
    public static void Load(EhrData s, string testDataFolder)
    {
        if (!Directory.Exists(testDataFolder))
        {
            throw new InvalidOperationException("Test data folder not found: " + testDataFolder +
                " (setting MockEhr:TestDataFolder).");
        }
        EhrDataFiles.Read(s, testDataFolder, EhrStore.FileJson);
    }

    /// <summary>
    /// The row of table PA_PAYER for the local mock payer (localhost:5080), as GET api/prior-auth-data/payers returns it.
    /// In the real EHR the payers are rows the EHR team inserts; the mock adds this one by itself.
    /// </summary>
    public static Dictionary<string, object?> MockPayerRow()
    {
        Dictionary<string, object?> row = new Dictionary<string, object?>();
        row["payerName"] = "MockPayer";
        row["displayName"] = "Mock Da Vinci Payer";
        row["enabled"] = true;
        row["baseUrl"] = "http://localhost:5080";
        row["cdsServicesPath"] = "cds-services";
        row["fhirPath"] = "fhir";
        row["authType"] = "none";
        row["authTokenUrl"] = null;
        row["authClientId"] = null;
        row["authClientSecret"] = null;
        row["authScope"] = null;
        row["signCdsHooksRequests"] = true;
        row["useSubscriptions"] = true;
        row["notificationSecret"] = null;
        row["crdTimeoutSeconds"] = 300;
        row["timeoutSeconds"] = 300;
        row["dtrMode"] = "Forms";
        row["dtrAppLaunchUrl"] = null;
        return row;
    }

    /// <summary>
    /// Adds a document and its file content (Base64). Sets size and contentUrl. Also used by DocumentsController.
    /// </summary>
    public static void AddDocument(EhrData s, EhrDocumentDto document, string contentBase64)
    {
        byte[] bytes = Convert.FromBase64String(contentBase64);
        document.SizeBytes = bytes.Length;
        document.ContentUrl = "/api/documents/" + document.DocumentId + "/content";
        s.Documents.Add(document);

        EhrDocumentContentDto content = new EhrDocumentContentDto();
        content.DocumentId = document.DocumentId;
        content.ContentType = document.ContentType;
        content.ContentBase64 = contentBase64;
        s.DocumentContent.Add(content);
    }
}
