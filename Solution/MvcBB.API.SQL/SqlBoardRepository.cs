using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed board repository, calling the usp_Board_* stored
    /// procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlBoardRepository : IBoardRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlBoardRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Board> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<BoardRow>("dbo.usp_Board_GetAll", commandType: CommandType.StoredProcedure);
            return rows.Select(MapToBoard).ToList();
        }

        public Board? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<BoardRow>("dbo.usp_Board_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToBoard(row);
        }

        public Board Add(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<BoardRow>("dbo.usp_Board_Insert", new
            {
                board.Name,
                board.Description,
                board.SortOrder,
                board.IsActive,
                board.CreatedAt,
                board.ThreadCount,
                board.PostCount,
                LastPostByUserId = ParseUserId(board.LastPostByUserId),
                board.LastPostAt
            }, commandType: CommandType.StoredProcedure);
            return MapToBoard(row);
        }

        public void Update(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Board_Update", new
            {
                board.Id,
                board.Name,
                board.Description,
                board.SortOrder,
                board.IsActive,
                board.UpdatedAt,
                board.ThreadCount,
                board.PostCount,
                LastPostByUserId = ParseUserId(board.LastPostByUserId),
                board.LastPostAt
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Board_Delete", new { board.Id }, commandType: CommandType.StoredProcedure);
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
        /// Matches dbo.Boards' actual column types (LastPostByUserId is an INT
        /// foreign key to Users.Id there, not a string as on the Board model).
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
