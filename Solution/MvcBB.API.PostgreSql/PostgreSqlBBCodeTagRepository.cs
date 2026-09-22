using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlBBCodeTagRepository : IBBCodeTagRepository
    {
        public IReadOnlyList<BBCodeTagModel> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public BBCodeTagModel? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public BBCodeTagModel Add(BBCodeTagModel tag) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(BBCodeTagModel tag) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(BBCodeTagModel tag) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
