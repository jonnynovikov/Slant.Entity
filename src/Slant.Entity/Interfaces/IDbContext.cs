using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Slant.Entity
{
    /// <summary>
    /// Interface for every EF data context used with library
    /// </summary>
    public interface IDbContext : IDisposable
    {
        DbContextConfiguration Configuration { get; }

        Database Database { get; }

        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancelToken);
    }
}