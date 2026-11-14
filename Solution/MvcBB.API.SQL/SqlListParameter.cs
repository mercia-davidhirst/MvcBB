using System.Text.Json;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// Every batch method in this project needs to pass a variable-length list
    /// of ids/usernames into a single ad-hoc query. Dapper's own answer -
    /// "WHERE Id IN @Ids" - expands the list into one SQL parameter PER
    /// element, which hits SQL Server's hard 2100-parameter-per-query ceiling
    /// once a list gets large enough (a busy board's posts, for example) and
    /// fails with "The incoming request has too many parameters." This
    /// serializes the list as a single JSON array string instead - always
    /// exactly one parameter, however many elements it holds - paired with
    /// "WHERE Id IN (SELECT value FROM OPENJSON(@Ids))" (or CAST(value AS INT)
    /// for numeric ids) on the SQL side. PostgreSQL doesn't need this: its
    /// "= ANY(@array)" array parameters are already a single parameter
    /// regardless of length.
    /// </summary>
    internal static class SqlListParameter
    {
        public static string ToJson<T>(IEnumerable<T> values) => JsonSerializer.Serialize(values);
    }
}
