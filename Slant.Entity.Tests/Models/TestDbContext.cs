using Microsoft.EntityFrameworkCore;

namespace Slant.Entity.Tests.Models
{
    internal class TestDbContext : DbContext
    {
        private readonly string _connectionString;

        public TestDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseUser>()
                .HasKey(cu => new { cu.CourseId, cu.UserId });
        }

        public DbSet<Course> Courses { get; set; } = default!;
        public DbSet<CourseUser> CoursesUsers { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
    }
}
