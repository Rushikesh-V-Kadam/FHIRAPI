namespace FHIRAPI.Helpers;

/// <summary>
/// Reads SMART scopes ("patient/Observation.rs", "user/*.read", "patient/QuestionnaireResponse.cu") and answers
/// whether they allow one interaction on one resource type.
///   SMART v2 letters: c = create, r = read, u = update, d = delete, s = search
///   SMART v1 words:   read = r + s, write = c + u + d, * = everything
/// A v2 scope with search limits ("patient/Observation.rs?category=laboratory") gives NO access here: the limits
/// cannot be enforced, so the scope is ignored rather than widened.
/// </summary>
public static class SmartScopeChecker
{
    /// <summary>Scopes this API does not support: they are left out of the granted scope.</summary>
    private static readonly string[] Unsupported = { "openid", "online_access", "offline_access" };

    /// <summary>True when one of the scopes (space separated) allows the permission letter on the resource type.</summary>
    public static bool Allows(string? scopes, string resourceType, char permission)
    {
        foreach (string scope in Split(scopes))
        {
            if (ScopeAllows(scope, resourceType, permission))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True when the scope list contains this exact scope (e.g. "launch").</summary>
    public static bool Contains(string? scopes, string scope)
    {
        foreach (string item in Split(scopes))
        {
            if (item == scope)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The scope to grant: the requested scopes, without the unsupported ones and, when the app has an allowed list,
    /// only those found in that list (exact strings; non-resource scopes such as "launch" and "fhirUser" must be listed too).
    /// </summary>
    public static string Grant(string? requested, string? allowed)
    {
        List<string> allowedList = Split(allowed);
        List<string> granted = new List<string>();
        foreach (string scope in Split(requested))
        {
            if (Array.IndexOf(Unsupported, scope) >= 0)
            {
                continue;
            }
            if (allowedList.Count > 0 && !allowedList.Contains(scope))
            {
                continue;
            }
            if (!granted.Contains(scope))
            {
                granted.Add(scope);
            }
        }
        return string.Join(" ", granted);
    }

    /// <summary>Space-separated text -> list (empty items removed).</summary>
    public static List<string> Split(string? scopes)
    {
        List<string> list = new List<string>();
        if (string.IsNullOrWhiteSpace(scopes))
        {
            return list;
        }
        foreach (string item in scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            list.Add(item);
        }
        return list;
    }

    /// <summary>One scope "context/Type.permissions" against one interaction.</summary>
    private static bool ScopeAllows(string scope, string resourceType, char permission)
    {
        int slash = scope.IndexOf('/');
        if (slash <= 0)
        {
            return false;   // launch, fhirUser, openid ... are not resource scopes
        }
        string context = scope.Substring(0, slash);
        if (context != "patient" && context != "user" && context != "system")
        {
            return false;   // e.g. launch/patient
        }
        if (scope.IndexOf('?') >= 0)
        {
            return false;   // v2 search limits cannot be enforced here: ignore the scope
        }

        string rest = scope.Substring(slash + 1);
        int dot = rest.LastIndexOf('.');
        if (dot <= 0)
        {
            return false;
        }
        string scopeType = rest.Substring(0, dot);
        string permissions = ToLetters(rest.Substring(dot + 1));
        if (scopeType != "*" && scopeType != resourceType)
        {
            return false;
        }
        return permissions.IndexOf(permission) >= 0;
    }

    /// <summary>v1 word or v2 letters -> v2 letters. Anything else -> "" (no access).</summary>
    private static string ToLetters(string permissions)
    {
        if (permissions == "read")
        {
            return "rs";
        }
        if (permissions == "write")
        {
            return "cud";
        }
        if (permissions == "*")
        {
            return "cruds";
        }
        foreach (char letter in permissions)
        {
            if ("cruds".IndexOf(letter) < 0)
            {
                return string.Empty;
            }
        }
        return permissions;
    }
}
