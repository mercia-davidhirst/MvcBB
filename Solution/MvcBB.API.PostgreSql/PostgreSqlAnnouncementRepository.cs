using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlAnnouncementRepository : IAnnouncementRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlAnnouncementRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<AnnouncementModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AnnouncementModel>("SELECT * FROM public.fn_announcement_get_all()").ToList();
        }

        public IReadOnlyList<AnnouncementModel> GetActive()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AnnouncementModel>("SELECT * FROM public.fn_announcement_get_active()").ToList();
        }

        public AnnouncementModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<AnnouncementModel>("SELECT * FROM public.fn_announcement_get_by_id(@Id)", new { Id = id });
        }

        public AnnouncementModel Add(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_announcement_insert(@Title, @Content, @IsActive, @SortOrder, @StartDate, @EndDate, @CreatedAt, NULL)",
                new
                {
                    announcement.Title,
                    announcement.Content,
                    announcement.IsActive,
                    announcement.SortOrder,
                    announcement.StartDate,
                    announcement.EndDate,
                    announcement.CreatedAt
                });

            return connection.QuerySingle<AnnouncementModel>("SELECT * FROM public.fn_announcement_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_announcement_update(@Id, @Title, @Content, @IsActive, @SortOrder, @StartDate, @EndDate)",
                new
                {
                    announcement.Id,
                    announcement.Title,
                    announcement.Content,
                    announcement.IsActive,
                    announcement.SortOrder,
                    announcement.StartDate,
                    announcement.EndDate
                });
        }

        public void Remove(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_announcement_delete(@Id)", new { announcement.Id });
        }
    }
}
