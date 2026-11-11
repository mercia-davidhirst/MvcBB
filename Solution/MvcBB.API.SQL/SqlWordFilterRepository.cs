using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.API.SQL
{
    public class SqlWordFilterRepository : IWordFilterRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlWordFilterRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<WordFilterModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<WordFilterModel>("dbo.usp_WordFilter_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public WordFilterModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<WordFilterModel>("dbo.usp_WordFilter_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public WordFilterModel Add(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<WordFilterModel>("dbo.usp_WordFilter_Insert", new
            {
                filter.Word,
                filter.Replacement,
                filter.IsActive,
                filter.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_WordFilter_Update", new
            {
                filter.Id,
                filter.Word,
                filter.Replacement,
                filter.IsActive,
                filter.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_WordFilter_Delete", new { filter.Id }, commandType: CommandType.StoredProcedure);
        }
    }
}
