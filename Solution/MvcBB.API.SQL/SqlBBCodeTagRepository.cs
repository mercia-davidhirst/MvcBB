using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.SQL
{
    public class SqlBBCodeTagRepository : IBBCodeTagRepository
    {
        public IReadOnlyList<BBCodeTagModel> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public BBCodeTagModel? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public BBCodeTagModel Add(BBCodeTagModel tag) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(BBCodeTagModel tag) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(BBCodeTagModel tag) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
