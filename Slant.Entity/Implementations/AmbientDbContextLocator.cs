using Microsoft.EntityFrameworkCore;

namespace Slant.Entity;

public class AmbientDbContextLocator : IAmbientDbContextLocator
{
    public TDbContext? Get<TDbContext>() where TDbContext : DbContext
    {
        var ambientDbContextScope = DbContextScope.GetAmbientScope();
        return ambientDbContextScope?.DbContexts.Get<TDbContext>();
    }
}