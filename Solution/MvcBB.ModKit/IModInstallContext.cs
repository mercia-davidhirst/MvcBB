using System.Data;

namespace MvcBB.ModKit
{
    /// <summary>
    /// What a mod's InstallAsync gets to work with. Wraps whichever connection
    /// factory core already has registered for the active DataProvider, so a
    /// mod never needs to know which one is configured.
    /// </summary>
    public interface IModInstallContext
    {
        /// <summary>"InMemory", "Sql" or "PostgreSql" - matches MvcBB.API's DataProvider setting.</summary>
        string DataProvider { get; }

        /// <summary>
        /// A raw ADO.NET connection against the active database, or null when
        /// DataProvider is "InMemory" (there's no database to install into -
        /// an InMemory mod keeps its own data in a static in-process store).
        /// </summary>
        IDbConnection? GetConnection();
    }
}
