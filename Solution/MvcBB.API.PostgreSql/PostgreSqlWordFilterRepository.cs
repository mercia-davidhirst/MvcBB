using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlWordFilterRepository : IWordFilterRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlWordFilterRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<WordFilterModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<WordFilterModel>("SELECT * FROM public.fn_word_filter_get_all()").ToList();
        }

        public WordFilterModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<WordFilterModel>("SELECT * FROM public.fn_word_filter_get_by_id(@Id)", new { Id = id });
        }

        public WordFilterModel Add(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_word_filter_insert(@Word, @Replacement, @IsActive, @SortOrder, NULL)",
                new { filter.Word, filter.Replacement, filter.IsActive, filter.SortOrder });

            return connection.QuerySingle<WordFilterModel>("SELECT * FROM public.fn_word_filter_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_word_filter_update(@Id, @Word, @Replacement, @IsActive, @SortOrder)",
                new { filter.Id, filter.Word, filter.Replacement, filter.IsActive, filter.SortOrder });
        }

        public void Remove(WordFilterModel filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_word_filter_delete(@Id)", new { filter.Id });
        }
    }
}
