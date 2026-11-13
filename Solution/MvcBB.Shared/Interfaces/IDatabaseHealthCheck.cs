namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// Reports whether the API can currently reach its configured data store.
    /// InMemory is always healthy; Sql/PostgreSql open a connection and run a
    /// no-op query. Backs the root status page only - nothing functional
    /// depends on this.
    /// </summary>
    public interface IDatabaseHealthCheck
    {
        bool IsHealthy();
    }
}
