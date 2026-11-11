using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlRankRepository : IRankRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlRankRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<RankModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<RankModel>("SELECT * FROM public.fn_rank_get_all()").ToList();
        }

        public RankModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<RankModel>("SELECT * FROM public.fn_rank_get_by_id(@Id)", new { Id = id });
        }

        public RankModel Add(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_rank_insert(@Title, @MinPosts, @ImagePath, @IsActive, @SortOrder, NULL)",
                new { rank.Title, rank.MinPosts, rank.ImagePath, rank.IsActive, rank.SortOrder });

            return connection.QuerySingle<RankModel>("SELECT * FROM public.fn_rank_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_rank_update(@Id, @Title, @MinPosts, @ImagePath, @IsActive, @SortOrder)",
                new { rank.Id, rank.Title, rank.MinPosts, rank.ImagePath, rank.IsActive, rank.SortOrder });
        }

        public void Remove(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_rank_delete(@Id)", new { rank.Id });
        }
    }
}
