using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace Slant.Entity.Tests.Helpers
{
    internal class SqliteMemoryDatabaseLifetimeManager : IDisposable
    {
        public readonly string ConnectionString = $"DataSource={Guid.NewGuid()};mode=memory;cache=shared";

        private DbConnection? _keepAliveConnection;

        public SqliteMemoryDatabaseLifetimeManager()
        {
            _keepAliveConnection = new SqliteConnection(ConnectionString);
            _keepAliveConnection.Open();
        }

        public void Dispose() // see https://rules.sonarsource.com/csharp/RSPEC-3881
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_keepAliveConnection != null)
            {
                _keepAliveConnection.Dispose();
                _keepAliveConnection = null;
            }
        }
    }
}
