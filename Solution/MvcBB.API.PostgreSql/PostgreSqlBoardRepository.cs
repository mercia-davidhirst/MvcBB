using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed board repository, calling the fn_board_*/sp_board_*
    /// functions/procedures in Database/PostGresSql.
    /// </summary>
    public class PostgreSqlBoardRepository : IBoardRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlBoardRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Board> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<BoardRow>("SELECT * FROM public.fn_board_get_all()");
            return rows.Select(MapToBoard).ToList();
        }

        public Board? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<BoardRow>("SELECT * FROM public.fn_board_get_by_id(@Id)", new { Id = id });
            return row == null ? null : MapToBoard(row);
        }

        public Board Add(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_board_insert(@Name, @Description, @SortOrder, @IsActive, @ThreadCount, @PostCount, @LastPostByUserId, @LastPostAt, NULL)",
                new
                {
                    board.Name,
                    board.Description,
                    board.SortOrder,
                    board.IsActive,
                    board.ThreadCount,
                    board.PostCount,
                    LastPostByUserId = ParseUserId(board.LastPostByUserId),
                    board.LastPostAt
                });

            var row = connection.QuerySingle<BoardRow>("SELECT * FROM public.fn_board_get_by_id(@Id)", new { Id = newId });
            return MapToBoard(row);
        }

        public void Update(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_board_update(@Id, @Name, @Description, @SortOrder, @IsActive, @ThreadCount, @PostCount, @LastPostByUserId, @LastPostAt)",
                new
                {
                    board.Id,
                    board.Name,
                    board.Description,
                    board.SortOrder,
                    board.IsActive,
                    board.ThreadCount,
                    board.PostCount,
                    LastPostByUserId = ParseUserId(board.LastPostByUserId),
                    board.LastPostAt
                });
        }

        public void Remove(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_board_delete(@Id)", new { board.Id });
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static Board MapToBoard(BoardRow row) => new()
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            SortOrder = row.SortOrder,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            ThreadCount = row.ThreadCount,
            PostCount = row.PostCount,
            LastPostByUserId = row.LastPostByUserId?.ToString(),
            LastPostAt = row.LastPostAt
        };

        /// <summary>
        /// Matches public.boards' actual column types (last_post_by_user_id is
        /// an INTEGER foreign key to users.id there, not a string as on the
        /// Board model). Relies on Dapper.DefaultTypeMap.MatchNamesWithUnderscores
        /// (set in ServiceCollectionExtensions) to map the snake_case columns.
        /// </summary>
        private class BoardRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public int ThreadCount { get; set; }
            public int PostCount { get; set; }
            public int? LastPostByUserId { get; set; }
            public DateTime? LastPostAt { get; set; }
        }
    }
}
