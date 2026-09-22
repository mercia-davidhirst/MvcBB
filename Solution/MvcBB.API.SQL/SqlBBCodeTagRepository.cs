using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.SQL
{
    public class SqlBBCodeTagRepository : IBBCodeTagRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlBBCodeTagRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<BBCodeTagModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<BBCodeTagModel>("dbo.usp_BBCodeTag_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public BBCodeTagModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<BBCodeTagModel>("dbo.usp_BBCodeTag_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public BBCodeTagModel Add(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<BBCodeTagModel>("dbo.usp_BBCodeTag_Insert", new
            {
                tag.Name,
                tag.Pattern,
                tag.Replacement,
                tag.Description,
                tag.Example,
                tag.IsActive,
                tag.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_BBCodeTag_Update", new
            {
                tag.Id,
                tag.Name,
                tag.Pattern,
                tag.Replacement,
                tag.Description,
                tag.Example,
                tag.IsActive,
                tag.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_BBCodeTag_Delete", new { tag.Id }, commandType: CommandType.StoredProcedure);
        }
    }
}
