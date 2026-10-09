using System.Text;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api;

/// <summary>
/// Fictional DIALYSIS demo data, modelled on the provider's paper "Authorization/Referral Request Form":
///   header    patient, insurance plan / policy # / group #, request type (new / renewal), patient type (permanent /
///             transient), previous authorization # + previous DOS + # of treatments, requested DOS + # of treatments,
///             servicing facility (name, address, tax id, NPI), referring physician, attending nephrologist (NPI, phone, fax)
///   codes     ICD-10 N18.6, N17.9, D63.1, D50.9, N25.81; treatments G0491, 90935, 90937, 90945, 90999, 90989, 90993;
///             drugs Q5105, J0882, J0887, J1756, J0606, J0604, J2501 with a dose
/// Written to the JSON data file on the first start (and again by POST /api/admin/reset). All names, ids, NPIs,
/// authorization numbers and results are made up.
///
/// Patients and what each order shows with the mock payer (localhost:5080):
///   pat-1 Robert Hayes     RENEWAL of in-center HD and Retacrit; Parsabiv new; Zemplar no PA; Sensipar not covered
///   pat-2 Linda Morales    home PD: training course (PA only) + home dialysis (adaptive questionnaire)
///   pat-3 James Carter     NEW start: HD x52 (partial approval), Venofer approved, Aranesp pended (iron deficient)
///   pat-4 Susan Nguyen     TRANSIENT patient visiting the Lakeside unit, 90937 x9
///   pat-5 Michael Torres   URGENT dialysis for AKI (G0491), referred by the hospitalist
///   pat-6 Patricia Bell    home hemodialysis (NxStage, 90999) + training sessions x30 (partial, max 25)
///   pat-7 Thomas Reed      insured by a payer the gateway does not know -> "payer not configured"
/// </summary>
public static class SeedData
{
    // ------------------------------------------------------------------ diagnosis codes of the form
    private static readonly EhrCode Esrd = Icd("N18.6", "End stage renal disease");
    private static readonly EhrCode Ckd5 = Icd("N18.5", "Chronic kidney disease, stage 5");
    private static readonly EhrCode Aki = Icd("N17.9", "Acute kidney failure, unspecified");
    private static readonly EhrCode AnemiaCkd = Icd("D63.1", "Anemia in chronic kidney disease");
    private static readonly EhrCode IronDeficiency = Icd("D50.9", "Iron deficiency anemia, unspecified");
    private static readonly EhrCode Shpt = Icd("N25.81", "Secondary hyperparathyroidism of renal origin");

    /// <summary>Fills an empty EhrData with the test data.</summary>
    public static void Load(EhrData s)
    {
        AddProvidersAndFacilities(s);
        AddRobertHayes(s);
        AddLindaMorales(s);
        AddJamesCarter(s);
        AddSusanNguyen(s);
        AddMichaelTorres(s);
        AddPatriciaBell(s);
        AddThomasReed(s);
    }

    // ================================================================== physicians and servicing facilities

    private static void AddProvidersAndFacilities(EhrData s)
    {
        // attending nephrologists (= ordering practitioner of the orders)
        s.Practitioners.Add(Practitioner("prac-1", "1111111112", "Maria", "Santos", "207RN0300X", "Nephrology", "org-1", "214-555-0201", "214-555-0291"));
        s.Practitioners.Add(Practitioner("prac-3", "1666777887", "Anita", "Patel", "207RN0300X", "Nephrology", "org-2", "214-555-0203", "214-555-0293"));
        // surgeon and referring physicians
        s.Practitioners.Add(Practitioner("prac-2", "1333444556", "Daniel", "Brooks", "2086S0129X", "Vascular Surgery", null, "214-555-0202", "214-555-0292"));
        s.Practitioners.Add(Practitioner("prac-4", "1444555662", "Kevin", "Wright", "207R00000X", "Internal Medicine (primary care)", null, "214-555-0204", "214-555-0294"));
        s.Practitioners.Add(Practitioner("prac-5", "1777888995", "Emily", "Chen", "208M00000X", "Hospitalist", null, "214-555-0205", "214-555-0295"));

        // servicing facilities (name, address, tax id, NPI)
        EhrOrganizationDto main = new EhrOrganizationDto();
        main.OrganizationId = "org-1"; main.Npi = "1999999992"; main.TaxId = "12-3456789"; main.Name = "Demo Dialysis Center - Main";
        main.Address = Addr("300 Clinic Way", "Dallas", "TX", "75201"); main.Phone = "214-555-0300"; main.Active = true;
        s.Organizations.Add(main);

        EhrOrganizationDto lakeside = new EhrOrganizationDto();
        lakeside.OrganizationId = "org-2"; lakeside.Npi = "1888999004"; lakeside.TaxId = "12-3456790"; lakeside.Name = "Demo Dialysis Center - Lakeside";
        lakeside.Address = Addr("45 Lakeside Drive", "Dallas", "TX", "75218"); lakeside.Phone = "214-555-0400"; lakeside.Active = true;
        s.Organizations.Add(lakeside);

        s.Locations.Add(Location("loc-main", "Demo Dialysis Center - Main, in-center unit", "65", "org-1", main.Address));   // 65 = ESRD facility
        s.Locations.Add(Location("loc-lakeside", "Demo Dialysis Center - Lakeside, in-center unit", "65", "org-2", lakeside.Address));
        s.Locations.Add(Location("loc-office", "Demo Dialysis Center - Nephrology office", "11", "org-1", main.Address));
    }

    // ================================================================== pat-1 Robert Hayes: renewal of in-center HD

    private static void AddRobertHayes(EhrData s)
    {
        s.Patients.Add(Patient("pat-1", "MRN-100001", "Robert", "Hayes", "male", "1959-03-14", "100 Demo Street", "214-555-0101"));
        s.Insurances.Add(Insurance("cov-1", "pat-1", "MBR-1001", "GRP-500", "Mock Da Vinci Payer", "PAYER001", "GOLD-PPO", "Gold PPO"));
        s.Encounters.Add(Encounter("enc-1", "pat-1", "prac-1", "2026-09-28T09:30:00Z", Esrd));

        s.Conditions.Add(Condition("cond-1-esrd", "pat-1", "N18.6", "End stage renal disease", "2024-02-10"));
        s.Conditions.Add(Condition("cond-1-anemia", "pat-1", "D63.1", "Anemia in chronic kidney disease", "2024-06-15"));
        s.Conditions.Add(Condition("cond-1-shpt", "pat-1", "N25.81", "Secondary hyperparathyroidism of renal origin", "2025-03-01"));
        s.Conditions.Add(Condition("cond-1-dialysis", "pat-1", "Z99.2", "Dependence on renal dialysis", "2024-02-20"));
        s.Procedures.Add(Procedure("proc-1-avf", "pat-1", "36821", "Arteriovenous fistula creation", "2023-11-02T14:00:00Z", "prac-2"));

        AddMonthlyLabs(s, "pat-1", "1", "2026-09-22T07:00:00Z", 8m, 7.9m, 9.2m, 480m, 27m, 3.9m, 5.0m, 410m, 9.0m, 5.4m);

        // In-center HD: RENEWAL of the July - October authorization
        EhrOrderDto hd = Order("ord-1-hd", "pat-1", "enc-1", "prac-1", "org-1", "loc-main", "service", "90935", "CPT",
            "In-center hemodialysis, 3 times a week", 39m, Esrd);
        Renewal(hd, "PA20260706451203", "2026-07-06", "2026-10-04", 39);
        hd.ReferringPractitionerId = "prac-4";
        hd.Notes = "Continue HD 3x/week, 4 hours, left arm AV fistula.";
        s.Orders.Add(hd);

        // Retacrit: RENEWAL (continuation). 4,000 units x 39 treatments = 1,560 billing units of 100 units
        EhrOrderDto esa = Drug("ord-1-retacrit", "pat-1", "enc-1", "prac-1", "org-1", "Q5105",
            "Retacrit (epoetin alfa-epbx), 100 units", 1560m, "4,000 units IV with each dialysis treatment (3 times a week)", AnemiaCkd);
        Renewal(esa, "PA20260706451207", "2026-07-06", "2026-10-04", 39);
        s.Orders.Add(esa);

        // Parsabiv NEW: 5 mg x 39 = 1,950 billing units of 0.1 mg
        s.Orders.Add(Drug("ord-1-parsabiv", "pat-1", "enc-1", "prac-1", "org-1", "J0606",
            "Parsabiv (etelcalcetide), 0.1 mg", 1950m, "5 mg IV 3 times a week at the end of dialysis", Shpt));

        // Zemplar: 4 mcg x 39 = 156 billing units of 1 mcg (no PA)
        s.Orders.Add(Drug("ord-1-zemplar", "pat-1", "enc-1", "prac-1", "org-1", "J2501",
            "Zemplar (paricalcitol), 1 mcg", 156m, "4 mcg IV 3 times a week with dialysis", Shpt));

        // Sensipar: 30 mg daily x 90 days = 2,700 billing units of 1 mg (not a medical benefit)
        s.Orders.Add(Drug("ord-1-sensipar", "pat-1", "enc-1", "prac-1", "org-1", "J0604",
            "Sensipar (cinacalcet, oral), 1 mg", 2700m, "30 mg by mouth once daily", Shpt));

        AddTextDocument(s, "doc-1-labs", "pat-1", "11502-2", "Laboratory report", "Monthly dialysis labs - September 2026",
            "2026-09-22T12:00:00Z", "prac-1", null,
            "MONTHLY DIALYSIS LABS (2026-09-22)\n" +
            "eGFR 8 mL/min/1.73m2 | Creatinine 7.9 mg/dL\n" +
            "Hemoglobin 9.2 g/dL | Ferritin 480 ng/mL | TSAT 27%\n" +
            "Albumin 3.9 g/dL | Potassium 5.0 mmol/L | Calcium 9.0 mg/dL | Phosphorus 5.4 mg/dL | iPTH 410 pg/mL\n" +
            "Adequacy: single-pool Kt/V 1.45 | URR 74%");

        AddTextDocument(s, "doc-1-note", "pat-1", "11506-3", "Progress note", "Nephrology progress note",
            "2026-09-28T10:15:00Z", "prac-1", "enc-1",
            "67-year-old man with ESRD, in-center hemodialysis since 02/2024 via left arm AV fistula. Current authorization " +
            "PA20260706451203 (07/06-10/04/2026, 39 treatments) ends 10/04. Kt/V 1.45. On Retacrit 4,000 units since 07/2026: " +
            "Hb 8.4 -> 9.2 g/dL. iPTH 410 on Zemplar; start Parsabiv. CMS-2728 on file, Medicare ESRD since 05/2024, transplant waitlisted.");
    }

    // ================================================================== pat-2 Linda Morales: home peritoneal dialysis

    private static void AddLindaMorales(EhrData s)
    {
        s.Patients.Add(Patient("pat-2", "MRN-100002", "Linda", "Morales", "female", "1968-07-02", "200 Sample Avenue", "214-555-0102"));
        s.Insurances.Add(Insurance("cov-2", "pat-2", "MBR-1002", "GRP-610", "Mock Da Vinci Payer", "PAYER001", "SILVER-HMO", "Silver HMO"));
        s.Encounters.Add(Encounter("enc-2", "pat-2", "prac-3", "2026-09-26T11:00:00Z", Ckd5));

        s.Conditions.Add(Condition("cond-2-ckd5", "pat-2", "N18.5", "Chronic kidney disease, stage 5", "2025-11-04"));
        s.Conditions.Add(Condition("cond-2-dm", "pat-2", "E11.22", "Type 2 diabetes mellitus with diabetic chronic kidney disease", "2012-03-01"));
        s.Procedures.Add(Procedure("proc-2-pdcath", "pat-2", "49324", "Laparoscopic insertion of tunneled peritoneal dialysis catheter", "2026-09-08T08:00:00Z", "prac-2"));

        AddMonthlyLabs(s, "pat-2", "2", "2026-09-20T07:00:00Z", 10m, 5.6m, 10.4m, 310m, 25m, 3.6m, 4.6m, 280m, 9.1m, 4.8m);

        EhrOrderDto training = Order("ord-2-training", "pat-2", "enc-2", "prac-3", "org-1", "loc-main", "service", "90989", "CPT",
            "Home dialysis training, completed course (PD)", 1m, Ckd5);
        training.ReferringPractitionerId = "prac-4";
        training.RequestedEndDate = "2026-10-31";
        s.Orders.Add(training);

        EhrOrderDto home = Order("ord-2-home-pd", "pat-2", "enc-2", "prac-3", "org-1", null, "service", "90945", "CPT",
            "Home peritoneal dialysis (APD cycler)", 1m, Ckd5);
        home.PlaceOfServiceCode = "12";   // patient home
        home.RequestedEndDate = "2027-04-04";
        home.ReferringPractitionerId = "prac-4";
        home.Notes = "Automated PD (cycler) nightly. PD catheter placed 2026-09-08. Training completed 2026-09-25.";
        s.Orders.Add(home);

        AddTextDocument(s, "doc-2-training", "pat-2", "11506-3", "Progress note", "Home dialysis training summary",
            "2026-09-25T16:00:00Z", "prac-3", null,
            "Patient completed 8 home PD (APD cycler) training sessions with her husband as care partner. Home assessment " +
            "2026-09-18: adequate space, electricity and supply storage. No hernia. PD catheter placed 2026-09-08, exit site healed.");
    }

    // ================================================================== pat-3 James Carter: new dialysis start

    private static void AddJamesCarter(EhrData s)
    {
        s.Patients.Add(Patient("pat-3", "MRN-100003", "James", "Carter", "male", "1953-11-20", "310 Example Road", "214-555-0103"));
        s.Insurances.Add(Insurance("cov-3", "pat-3", "MBR-1003", "GRP-500", "Mock Da Vinci Payer", "PAYER001", "GOLD-PPO", "Gold PPO"));
        s.Encounters.Add(Encounter("enc-3", "pat-3", "prac-1", "2026-09-27T13:00:00Z", Esrd));

        s.Conditions.Add(Condition("cond-3-esrd", "pat-3", "N18.6", "End stage renal disease", "2026-07-14"));
        s.Conditions.Add(Condition("cond-3-anemia", "pat-3", "D63.1", "Anemia in chronic kidney disease", "2026-07-14"));
        s.Conditions.Add(Condition("cond-3-iron", "pat-3", "D50.9", "Iron deficiency anemia, unspecified", "2026-09-21"));
        s.Procedures.Add(Procedure("proc-3-cvc", "pat-3", "36558", "Insertion of tunneled central venous catheter", "2026-07-15T10:00:00Z", "prac-2"));

        AddMonthlyLabs(s, "pat-3", "3", "2026-09-21T07:00:00Z", 6m, 8.8m, 10.9m, 150m, 18m, 3.4m, 5.3m, 520m, 8.6m, 5.9m);

        // NEW: 4 a week x 13 weeks = 52 (plan limit 39 -> partial approval)
        EhrOrderDto hd = Order("ord-3-hd", "pat-3", "enc-3", "prac-1", "org-1", "loc-main", "service", "90935", "CPT",
            "In-center hemodialysis, 4 times a week", 52m, Esrd);
        hd.ReferringPractitionerId = "prac-5";
        hd.Notes = "New start 07/2026 in the hospital. Tunneled right IJ catheter; AV fistula planned.";
        s.Orders.Add(hd);

        // Venofer: 100 mg x 10 doses = 1,000 billing units of 1 mg
        s.Orders.Add(Drug("ord-3-venofer", "pat-3", "enc-3", "prac-1", "org-1", "J1756",
            "Venofer (iron sucrose), 1 mg", 1000m, "100 mg IV with 10 consecutive dialysis treatments", IronDeficiency));

        // Aranesp: 40 mcg weekly x 13 = 520 billing units of 1 mcg (labs miss the ESA criteria -> pended)
        s.Orders.Add(Drug("ord-3-aranesp", "pat-3", "enc-3", "prac-1", "org-1", "J0882",
            "Aranesp (darbepoetin alfa), 1 mcg", 520m, "40 mcg IV once a week with dialysis", AnemiaCkd));

        AddTextDocument(s, "doc-3-labs", "pat-3", "11502-2", "Laboratory report", "Monthly dialysis labs - September 2026",
            "2026-09-21T12:00:00Z", "prac-1", null,
            "MONTHLY DIALYSIS LABS (2026-09-21)\n" +
            "eGFR 6 mL/min/1.73m2 | Creatinine 8.8 mg/dL\n" +
            "Hemoglobin 10.9 g/dL | Ferritin 150 ng/mL | TSAT 18%\n" +
            "Albumin 3.4 g/dL | Potassium 5.3 mmol/L | Calcium 8.6 mg/dL | Phosphorus 5.9 mg/dL | iPTH 520 pg/mL\n" +
            "Adequacy: single-pool Kt/V 1.25 | URR 67%");

        AddTextDocument(s, "doc-3-note", "pat-3", "11506-3", "Progress note", "Nephrology progress note",
            "2026-09-27T14:00:00Z", "prac-1", "enc-3",
            "72-year-old man, new dialysis start 07/2026 in the hospital (referred by the hospitalist). Tunneled catheter, AV fistula planned. " +
            "Hb 10.9 g/dL, ferritin 150, TSAT 18% - iron deficient: Venofer 100 mg x 10, then Aranesp. CMS-2728 signed 07/2026. Medicare application pending.");
    }

    // ================================================================== pat-4 Susan Nguyen: transient (visiting) patient

    private static void AddSusanNguyen(EhrData s)
    {
        s.Patients.Add(Patient("pat-4", "MRN-100004", "Susan", "Nguyen", "female", "1975-01-09", "420 Test Lane", "602-555-0104"));
        s.Insurances.Add(Insurance("cov-4", "pat-4", "MBR-1004", "GRP-720", "Mock Da Vinci Payer", "PAYER001", "BRONZE-EPO", "Bronze EPO"));
        s.Encounters.Add(Encounter("enc-4", "pat-4", "prac-3", "2026-09-28T15:00:00Z", Esrd));
        s.Conditions.Add(Condition("cond-4-esrd", "pat-4", "N18.6", "End stage renal disease", "2022-08-01"));

        AddMonthlyLabs(s, "pat-4", "4", "2026-09-15T07:00:00Z", 7m, 8.1m, 10.2m, 390m, 29m, 3.8m, 4.9m, 350m, 9.2m, 5.1m);

        // TRANSIENT: 3 weeks at the Lakeside unit while visiting family, 9 treatments
        EhrOrderDto hd = Order("ord-4-hd", "pat-4", "enc-4", "prac-3", "org-2", "loc-lakeside", "service", "90937", "CPT",
            "In-center hemodialysis (visiting patient), 3 times a week", 9m, Esrd);
        hd.PatientType = "transient";
        hd.RequestedStartDate = "2026-10-12";
        hd.RequestedEndDate = "2026-10-30";
        hd.Notes = "Visiting from Phoenix, AZ. Home unit: Sunrise Kidney Center (fictional), returns 2026-10-31.";
        s.Orders.Add(hd);

        AddTextDocument(s, "doc-4-transfer", "pat-4", "11506-3", "Progress note", "Transient patient transfer packet",
            "2026-09-28T16:00:00Z", "prac-3", "enc-4",
            "Transient dialysis request from the home unit. HD 3x/week, 4 hours, right arm AV fistula, EDW 68 kg. " +
            "Hepatitis B surface antigen negative (2026-09-01). Last Kt/V 1.38. No medication changes.");
    }

    // ================================================================== pat-5 Michael Torres: urgent AKI dialysis

    private static void AddMichaelTorres(EhrData s)
    {
        s.Patients.Add(Patient("pat-5", "MRN-100005", "Michael", "Torres", "male", "1961-05-23", "515 Market Street", "214-555-0105"));
        s.Insurances.Add(Insurance("cov-5", "pat-5", "MBR-1005", "GRP-500", "Mock Da Vinci Payer", "PAYER001", "GOLD-PPO", "Gold PPO"));
        s.Encounters.Add(Encounter("enc-5", "pat-5", "prac-3", "2026-09-27T10:00:00Z", Aki));
        s.Conditions.Add(Condition("cond-5-aki", "pat-5", "N17.9", "Acute kidney failure, unspecified", "2026-09-10"));

        AddMonthlyLabs(s, "pat-5", "5", "2026-09-26T07:00:00Z", 15m, 4.1m, 10.8m, 260m, 24m, 3.2m, 5.6m, 95m, 8.4m, 5.0m);

        // URGENT: outpatient dialysis for AKI after discharge, 12 treatments, reassessed monthly
        EhrOrderDto aki = Order("ord-5-aki", "pat-5", "enc-5", "prac-3", "org-1", "loc-main", "service", "G0491", "HCPCS",
            "Dialysis for acute kidney injury (without ESRD)", 12m, Aki);
        aki.Priority = "urgent";
        aki.ReferringPractitionerId = "prac-5";
        aki.RequestedStartDate = "2026-10-01";
        aki.RequestedEndDate = "2026-10-28";
        aki.Notes = "Septic ATN, discharged 2026-09-26 on HD. Check kidney recovery at each treatment.";
        s.Orders.Add(aki);

        AddTextDocument(s, "doc-5-discharge", "pat-5", "18842-5", "Discharge summary", "Hospital discharge summary",
            "2026-09-26T18:00:00Z", "prac-5", null,
            "Admitted 2026-09-09 with urosepsis; AKI (acute tubular necrosis) from 2026-09-10, oliguric, baseline creatinine 1.1 mg/dL. " +
            "Hemodialysis started 2026-09-12 via tunneled catheter. Discharged 2026-09-26, creatinine 4.1, urine output 600 mL/day. " +
            "No prior CKD. Outpatient AKI dialysis requested with weekly renal recovery assessment.");
    }

    // ================================================================== pat-6 Patricia Bell: home hemodialysis (NxStage)

    private static void AddPatriciaBell(EhrData s)
    {
        s.Patients.Add(Patient("pat-6", "MRN-100006", "Patricia", "Bell", "female", "1970-12-03", "606 Garden Court", "214-555-0106"));
        s.Insurances.Add(Insurance("cov-6", "pat-6", "MBR-1006", "GRP-610", "Mock Da Vinci Payer", "PAYER001", "SILVER-HMO", "Silver HMO"));
        s.Encounters.Add(Encounter("enc-6", "pat-6", "prac-3", "2026-09-25T09:00:00Z", Esrd));
        s.Conditions.Add(Condition("cond-6-esrd", "pat-6", "N18.6", "End stage renal disease", "2023-04-18"));
        s.Procedures.Add(Procedure("proc-6-avf", "pat-6", "36821", "Arteriovenous fistula creation", "2022-12-01T09:00:00Z", "prac-2"));

        AddMonthlyLabs(s, "pat-6", "6", "2026-09-19T07:00:00Z", 9m, 7.2m, 10.6m, 420m, 31m, 4.0m, 4.4m, 290m, 9.3m, 4.9m);

        // Training per session: 30 asked, plan allows 25 -> partial approval
        EhrOrderDto training = Order("ord-6-training", "pat-6", "enc-6", "prac-3", "org-2", "loc-lakeside", "service", "90993", "CPT",
            "Home hemodialysis training, per session", 30m, Esrd);
        training.ReferringPractitionerId = "prac-4";
        training.RequestedEndDate = "2026-12-18";
        s.Orders.Add(training);

        // NxStage home hemodialysis (90999) - training not finished yet -> pended for review
        EhrOrderDto home = Order("ord-6-nxstage", "pat-6", "enc-6", "prac-3", "org-2", null, "service", "90999", "CPT",
            "Home hemodialysis (NxStage), 5 times a week", 1m, Esrd);
        home.PlaceOfServiceCode = "12";
        home.RequestedStartDate = "2026-12-21";
        home.RequestedEndDate = "2027-06-20";
        home.ReferringPractitionerId = "prac-4";
        home.Notes = "Short daily home HD with NxStage, husband as care partner. Training started 2026-09-21.";
        s.Orders.Add(home);

        AddTextDocument(s, "doc-6-training", "pat-6", "11506-3", "Progress note", "Home hemodialysis training plan",
            "2026-09-25T15:00:00Z", "prac-3", "enc-6",
            "Home HD (NxStage) training started 2026-09-21, 4 sessions done. Estimated 26-30 sessions. Care partner: husband. " +
            "Left forearm AV fistula, self-cannulation in progress. Home assessment scheduled 2026-11-10.");
    }

    // ================================================================== pat-7 Thomas Reed: payer not connected

    private static void AddThomasReed(EhrData s)
    {
        s.Patients.Add(Patient("pat-7", "MRN-100007", "Thomas", "Reed", "male", "1966-02-17", "707 River Road", "214-555-0107"));
        s.Insurances.Add(Insurance("cov-7", "pat-7", "OTH-7007", "OTH-100", "Other Health Plan (not connected)", "PAYER999", "BASIC", "Basic"));
        s.Encounters.Add(Encounter("enc-7", "pat-7", "prac-1", "2026-09-28T15:00:00Z", Esrd));
        s.Conditions.Add(Condition("cond-7-esrd", "pat-7", "N18.6", "End stage renal disease", "2021-08-01"));

        s.Orders.Add(Order("ord-7-hd", "pat-7", "enc-7", "prac-1", "org-1", "loc-main", "service", "90935", "CPT",
            "In-center hemodialysis, 3 times a week", 39m, Esrd));
    }

    // ================================================================== documents

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
        row["payerIdentifier"] = "PAYER001";
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
        row["memberIdSystem"] = "http://example.org/member-ids";
        row["payerIdSystem"] = "http://example.org/payer-ids";
        row["crdTimeoutSeconds"] = 300;
        row["timeoutSeconds"] = 300;
        row["fhirVersion"] = "R4";
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

    /// <summary>A plain-text clinical document (lab report, progress note, discharge summary).</summary>
    private static void AddTextDocument(EhrData s, string id, string patientId, string loinc, string typeDisplay, string title,
        string created, string authorId, string? encounterId, string text)
    {
        EhrDocumentDto document = new EhrDocumentDto();
        document.DocumentId = id; document.PatientId = patientId; document.TypeCode = loinc; document.TypeDisplay = typeDisplay;
        document.Category = "clinical-note"; document.Title = title; document.CreatedDateTime = created;
        document.AuthorPractitionerId = authorId; document.OrganizationId = "org-1"; document.EncounterId = encounterId;
        document.ContentType = "text/plain";
        AddDocument(s, document, Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
    }

    // ================================================================== labs

    /// <summary>The monthly dialysis lab panel (LOINC coded) of one patient.</summary>
    private static void AddMonthlyLabs(EhrData s, string patientId, string key, string when, decimal egfr, decimal creatinine,
        decimal hemoglobin, decimal ferritin, decimal tsat, decimal albumin, decimal potassium, decimal pth, decimal calcium, decimal phosphorus)
    {
        s.Observations.Add(Lab("obs-" + key + "-egfr", patientId, "33914-3", "eGFR (MDRD)", egfr, "mL/min/1.73m2", when));
        s.Observations.Add(Lab("obs-" + key + "-creat", patientId, "2160-0", "Creatinine [Mass/volume] in Serum or Plasma", creatinine, "mg/dL", when));
        s.Observations.Add(Lab("obs-" + key + "-hgb", patientId, "718-7", "Hemoglobin [Mass/volume] in Blood", hemoglobin, "g/dL", when));
        s.Observations.Add(Lab("obs-" + key + "-ferritin", patientId, "2276-4", "Ferritin [Mass/volume] in Serum or Plasma", ferritin, "ng/mL", when));
        s.Observations.Add(Lab("obs-" + key + "-tsat", patientId, "2502-3", "Iron saturation [Mass Fraction] in Serum or Plasma", tsat, "%", when));
        s.Observations.Add(Lab("obs-" + key + "-alb", patientId, "1751-7", "Albumin [Mass/volume] in Serum or Plasma", albumin, "g/dL", when));
        s.Observations.Add(Lab("obs-" + key + "-k", patientId, "2823-3", "Potassium [Moles/volume] in Serum or Plasma", potassium, "mmol/L", when));
        s.Observations.Add(Lab("obs-" + key + "-pth", patientId, "2731-8", "Parathyrin.intact [Mass/volume] in Serum or Plasma", pth, "pg/mL", when));
        s.Observations.Add(Lab("obs-" + key + "-ca", patientId, "17861-6", "Calcium [Mass/volume] in Serum or Plasma", calcium, "mg/dL", when));
        s.Observations.Add(Lab("obs-" + key + "-phos", patientId, "2777-1", "Phosphate [Mass/volume] in Serum or Plasma", phosphorus, "mg/dL", when));
    }

    // ================================================================== small builders

    private static EhrAddress Addr(string line, string city, string state, string postalCode)
    {
        EhrAddress address = new EhrAddress();
        address.Line1 = line; address.City = city; address.State = state; address.PostalCode = postalCode; address.Country = "US";
        return address;
    }

    private static EhrCode Code(string code, string system, string display)
    {
        EhrCode value = new EhrCode();
        value.Code = code; value.CodeSystem = system; value.Display = display;
        return value;
    }

    private static EhrCode Icd(string code, string display)
    {
        return Code(code, "ICD10CM", display);
    }

    private static EhrPractitionerDto Practitioner(string id, string npi, string first, string last, string taxonomy, string specialty,
        string? organizationId, string phone, string fax)
    {
        EhrPractitionerDto practitioner = new EhrPractitionerDto();
        practitioner.PractitionerId = id; practitioner.Npi = npi; practitioner.FirstName = first; practitioner.LastName = last; practitioner.Prefix = "Dr.";
        practitioner.Specialty = Code(taxonomy, "NUCC", specialty); practitioner.OrganizationId = organizationId;
        practitioner.Phone = phone; practitioner.Fax = fax; practitioner.Active = true;
        return practitioner;
    }

    private static EhrLocationDto Location(string id, string name, string placeOfService, string organizationId, EhrAddress? address)
    {
        EhrLocationDto location = new EhrLocationDto();
        location.LocationId = id; location.Name = name; location.PlaceOfServiceCode = placeOfService;
        location.OrganizationId = organizationId; location.Address = address;
        return location;
    }

    private static EhrPatientDto Patient(string id, string mrn, string first, string last, string gender, string birthDate, string street, string phone)
    {
        EhrPatientDto patient = new EhrPatientDto();
        patient.PatientId = id; patient.Mrn = mrn; patient.FirstName = first; patient.LastName = last; patient.Gender = gender;
        patient.DateOfBirth = birthDate; patient.Address = Addr(street, "Dallas", "TX", "75201"); patient.Phone = phone; patient.Active = true;
        return patient;
    }

    /// <summary>Active primary insurance 2026-01-01 to 2027-12-31 (form: Insurance Plan, Policy # = member id, Group #).</summary>
    private static EhrInsuranceDto Insurance(string id, string patientId, string memberId, string groupNumber, string payerName, string payerId,
        string planId, string planName)
    {
        EhrInsuranceDto insurance = new EhrInsuranceDto();
        insurance.CoverageId = id; insurance.PatientId = patientId; insurance.Status = "active"; insurance.MemberId = memberId;
        insurance.PayerId = payerId; insurance.PayerName = payerName; insurance.PlanId = planId; insurance.PlanName = planName;
        insurance.GroupNumber = groupNumber; insurance.RelationshipToSubscriber = "self"; insurance.EffectiveDate = "2026-01-01";
        insurance.TerminationDate = "2027-12-31"; insurance.CoverageOrder = 1; insurance.CoverageType = "HIP";
        return insurance;
    }

    /// <summary>The nephrology visit during which the orders are signed.</summary>
    private static EhrEncounterDto Encounter(string id, string patientId, string practitionerId, string start, EhrCode reason)
    {
        EhrEncounterDto encounter = new EhrEncounterDto();
        encounter.EncounterId = id; encounter.PatientId = patientId; encounter.Status = "in-progress"; encounter.EncounterClass = "AMB";
        encounter.TypeCode = "99214"; encounter.TypeCodeSystem = "CPT"; encounter.TypeDisplay = "Nephrology office visit"; encounter.StartDateTime = start;
        encounter.PractitionerId = practitionerId; encounter.OrganizationId = "org-1"; encounter.LocationId = "loc-office";
        encounter.ReasonDiagnoses.Add(reason);
        return encounter;
    }

    /// <summary>
    /// A draft order (one line of the request form): attending nephrologist = ordering practitioner, servicing facility =
    /// performer organization, requested DOS 2026-10-05 to 2027-01-03 (13 weeks), quantity = # of treatments or billing units.
    /// Request type "new" and patient type "permanent" unless changed.
    /// </summary>
    private static EhrOrderDto Order(string id, string patientId, string encounterId, string attendingId, string facilityId, string? locationId,
        string type, string code, string system, string display, decimal quantity, EhrCode diagnosis)
    {
        EhrOrderDto order = new EhrOrderDto();
        order.OrderId = id; order.OrderType = type; order.PatientId = patientId; order.EncounterId = encounterId; order.Status = "draft";
        order.Code = code; order.CodeSystem = system; order.CodeDisplay = display; order.Quantity = quantity;
        order.OrderedDateTime = "2026-09-28T10:00:00Z"; order.RequestedStartDate = "2026-10-05"; order.RequestedEndDate = "2027-01-03";
        order.OrderingPractitionerId = attendingId; order.PerformerOrganizationId = facilityId;
        order.LocationId = locationId; order.PlaceOfServiceCode = "65"; order.Priority = "routine";
        order.RequestType = "new"; order.PatientType = "permanent";
        order.CoverageId = "cov-" + patientId.Substring(patientId.IndexOf('-') + 1);
        order.Diagnoses.Add(diagnosis);
        if (diagnosis.Code != "N18.6" && diagnosis.Code != "N17.9" && diagnosis.Code != "N18.5")
        {
            order.Diagnoses.Add(Esrd);   // drug orders also carry the kidney diagnosis
        }
        return order;
    }

    /// <summary>A drug given at the dialysis unit (form: Drug Codes with a dose); quantity is in HCPCS billing units.</summary>
    private static EhrOrderDto Drug(string id, string patientId, string encounterId, string attendingId, string facilityId, string hcpcs,
        string display, decimal billingUnits, string dose, EhrCode diagnosis)
    {
        string locationId = facilityId == "org-2" ? "loc-lakeside" : "loc-main";
        EhrOrderDto order = Order(id, patientId, encounterId, attendingId, facilityId, locationId, "medication", hcpcs, "HCPCS",
            display, billingUnits, diagnosis);
        EhrMedicationDetailsDto medication = new EhrMedicationDetailsDto();
        medication.DoseText = dose;
        medication.DaysSupply = 90;
        order.Medication = medication;
        order.Notes = dose;
        return order;
    }

    /// <summary>Marks an order as a renewal of an earlier authorization (form: Renewal, Previous Authorization #, Previous DOS).</summary>
    private static void Renewal(EhrOrderDto order, string previousAuthorization, string previousStart, string previousEnd, int previousTreatments)
    {
        order.RequestType = "renewal";
        order.PreviousAuthorizationNumber = previousAuthorization;
        order.PreviousStartDate = previousStart;
        order.PreviousEndDate = previousEnd;
        order.PreviousTreatments = previousTreatments;
    }

    private static EhrObservationDto Lab(string id, string patientId, string code, string display, decimal value, string unit, string when)
    {
        EhrObservationDto observation = new EhrObservationDto();
        observation.ObservationId = id; observation.PatientId = patientId; observation.Category = "laboratory";
        observation.Code = code; observation.Display = display; observation.ValueNumber = value; observation.Unit = unit;
        observation.EffectiveDateTime = when; observation.IssuedDateTime = when;
        return observation;
    }

    private static EhrConditionDto Condition(string id, string patientId, string code, string display, string onset)
    {
        EhrConditionDto condition = new EhrConditionDto();
        condition.ConditionId = id; condition.PatientId = patientId; condition.Code = code; condition.Display = display;
        condition.ClinicalStatus = "active"; condition.Category = "problem-list-item"; condition.OnsetDate = onset;
        return condition;
    }

    private static EhrProcedureDto Procedure(string id, string patientId, string code, string display, string when, string practitionerId)
    {
        EhrProcedureDto procedure = new EhrProcedureDto();
        procedure.ProcedureId = id; procedure.PatientId = patientId; procedure.Code = code; procedure.Display = display;
        procedure.PerformedDateTime = when; procedure.PerformerPractitionerId = practitionerId;
        return procedure;
    }
}
