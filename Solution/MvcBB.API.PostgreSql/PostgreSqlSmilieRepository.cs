using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlSmilieRepository : ISmilieRepository
    {
        public IReadOnlyList<SmilieModel> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public SmilieModel? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public SmilieModel Add(SmilieModel smilie) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(SmilieModel smilie) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(SmilieModel smilie) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
