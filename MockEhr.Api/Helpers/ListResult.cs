using MockEhr.Api.Models;

namespace MockEhr.Api.Helpers;

/// <summary>Builds the { items, total } list body of the EHR contract.</summary>
public static class ListResult
{
    /// <summary>Wraps a list.</summary>
    public static EhrList<T> Of<T>(List<T> items)
    {
        EhrList<T> list = new EhrList<T>();
        list.Items = items;
        list.Total = items.Count;
        return list;
    }
}
