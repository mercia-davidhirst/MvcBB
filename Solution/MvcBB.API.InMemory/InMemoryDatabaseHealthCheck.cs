using MvcBB.Shared.Interfaces;

namespace MvcBB.API.InMemory
{
    public class InMemoryDatabaseHealthCheck : IDatabaseHealthCheck
    {
        public bool IsHealthy() => true;
    }
}
