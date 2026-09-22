using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.SQL
{
    public class SqlSmilieRepository : ISmilieRepository
    {
        public IReadOnlyList<SmilieModel> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public SmilieModel? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public SmilieModel Add(SmilieModel smilie) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(SmilieModel smilie) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(SmilieModel smilie) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
