using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.SQL
{
    public class SqlSmilieRepository : ISmilieRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlSmilieRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<SmilieModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<SmilieModel>("dbo.usp_Smilie_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public SmilieModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<SmilieModel>("dbo.usp_Smilie_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public SmilieModel Add(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<SmilieModel>("dbo.usp_Smilie_Insert", new
            {
                smilie.Code,
                smilie.Description,
                smilie.ImagePath,
                smilie.IsActive,
                smilie.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Smilie_Update", new
            {
                smilie.Id,
                smilie.Code,
                smilie.Description,
                smilie.ImagePath,
                smilie.IsActive,
                smilie.SortOrder
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(SmilieModel smilie)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Smilie_Delete", new { smilie.Id }, commandType: CommandType.StoredProcedure);
        }
    }
}
