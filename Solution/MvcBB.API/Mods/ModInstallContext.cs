using System.Data;
using MvcBB.ModKit;

namespace MvcBB.API.Mods
{
    public class ModInstallContext : IModInstallContext
    {
        private readonly IDbConnection? _connection;

        public ModInstallContext(string dataProvider, IDbConnection? connection)
        {
            DataProvider = dataProvider;
            _connection = connection;
        }

        public string DataProvider { get; }

        public IDbConnection? GetConnection() => _connection;
    }
}
