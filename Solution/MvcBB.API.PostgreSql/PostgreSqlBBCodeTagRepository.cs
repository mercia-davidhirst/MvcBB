using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlBBCodeTagRepository : IBBCodeTagRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlBBCodeTagRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<BBCodeTagModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<BBCodeTagModel>("SELECT * FROM public.fn_bb_code_tag_get_all()").ToList();
        }

        public BBCodeTagModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<BBCodeTagModel>("SELECT * FROM public.fn_bb_code_tag_get_by_id(@Id)", new { Id = id });
        }

        public BBCodeTagModel Add(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_bb_code_tag_insert(@Name, @Pattern, @Replacement, @Description, @Example, @IsActive, @SortOrder, NULL)",
                new { tag.Name, tag.Pattern, tag.Replacement, tag.Description, tag.Example, tag.IsActive, tag.SortOrder });

            return connection.QuerySingle<BBCodeTagModel>("SELECT * FROM public.fn_bb_code_tag_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_bb_code_tag_update(@Id, @Name, @Pattern, @Replacement, @Description, @Example, @IsActive, @SortOrder)",
                new { tag.Id, tag.Name, tag.Pattern, tag.Replacement, tag.Description, tag.Example, tag.IsActive, tag.SortOrder });
        }

        public void Remove(BBCodeTagModel tag)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_bb_code_tag_delete(@Id)", new { tag.Id });
        }
    }
}
