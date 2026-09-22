using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlSmilieRepository : ISmilieRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlSmilieRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<SmilieModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<SmilieModel>("SELECT * FROM public.fn_smilie_get_all()").ToList();
        }

        public SmilieModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<SmilieModel>("SELECT * FROM public.fn_smilie_get_by_id(@Id)", new { Id = id });
        }

        public SmilieModel Add(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_smilie_insert(@Code, @Description, @ImagePath, @IsActive, @SortOrder, NULL)",
                new { smilie.Code, smilie.Description, smilie.ImagePath, smilie.IsActive, smilie.SortOrder });

            return connection.QuerySingle<SmilieModel>("SELECT * FROM public.fn_smilie_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_smilie_update(@Id, @Code, @Description, @ImagePath, @IsActive, @SortOrder)",
                new { smilie.Id, smilie.Code, smilie.Description, smilie.ImagePath, smilie.IsActive, smilie.SortOrder });
        }

        public void Remove(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_smilie_delete(@Id)", new { smilie.Id });
        }
    }
}
