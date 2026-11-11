using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.API.SQL
{
    public class SqlAnnouncementRepository : IAnnouncementRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlAnnouncementRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<AnnouncementModel> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AnnouncementModel>("dbo.usp_Announcement_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public IReadOnlyList<AnnouncementModel> GetActive()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AnnouncementModel>("dbo.usp_Announcement_GetActive", commandType: CommandType.StoredProcedure).ToList();
        }

        public AnnouncementModel? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<AnnouncementModel>("dbo.usp_Announcement_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public AnnouncementModel Add(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<AnnouncementModel>("dbo.usp_Announcement_Insert", new
            {
                announcement.Title,
                announcement.Content,
                announcement.IsActive,
                announcement.SortOrder,
                announcement.StartDate,
                announcement.EndDate,
                announcement.CreatedAt
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Announcement_Update", new
            {
                announcement.Id,
                announcement.Title,
                announcement.Content,
                announcement.IsActive,
                announcement.SortOrder,
                announcement.StartDate,
                announcement.EndDate
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(AnnouncementModel announcement)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Announcement_Delete", new { announcement.Id }, commandType: CommandType.StoredProcedure);
        }
    }
}
