using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Slant.Entity
{
    public interface IDbContext : IDisposable
    {
        DbContextConfiguration Configuration { get; }
        Database Database { get; }

        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancelToken);
    }
}