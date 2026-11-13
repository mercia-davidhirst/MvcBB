using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.InMemory
{
    public class InMemoryPasswordPolicyRepository : IPasswordPolicyRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryPasswordPolicyRepository(InMemoryStore store)
        {
            _store = store;
        }

        public PasswordPolicy Get() => _store.PasswordPolicy;

        public void Update(PasswordPolicy policy) => _store.PasswordPolicy = policy;
    }
}
