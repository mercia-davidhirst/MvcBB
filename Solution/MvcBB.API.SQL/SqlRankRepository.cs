using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.API.SQL
{
    public class SqlRankRepository : IRankRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlRankRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<RankModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<RankModel>("dbo.usp_Rank_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public RankModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<RankModel>("dbo.usp_Rank_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public RankModel Add(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<RankModel>("dbo.usp_Rank_Insert", new
            {
                rank.Title,
                rank.MinPosts,
                rank.ImagePath,
                rank.IsActive,
                rank.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Rank_Update", new
            {
                rank.Id,
                rank.Title,
                rank.MinPosts,
                rank.ImagePath,
                rank.IsActive,
                rank.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(RankModel rank)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Rank_Delete", new { rank.Id }, commandType: CommandType.StoredProcedure);
        }
    }
}
