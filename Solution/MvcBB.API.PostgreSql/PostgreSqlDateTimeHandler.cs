using System.Data;
using Dapper;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// Npgsql maps PostgreSQL's "timestamp with time zone" (used throughout
    /// Database/PostGresSql) to DateTimeOffset, not DateTime - but every
    /// Shared model uses plain DateTime (matching DateTime.UtcNow usage
    /// throughout the app). Without this handler, Dapper throws trying to
    /// read a timestamptz column into a DateTime/DateTime? property. Registered
    /// for both DateTime and DateTime? in ServiceCollectionExtensions.
    /// </summary>
    public class PostgreSqlDateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) => value switch
        {
            DateTimeOffset dto => dto.UtcDateTime,
            DateTime dt => dt,
            _ => Convert.ToDateTime(value)
        };

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
