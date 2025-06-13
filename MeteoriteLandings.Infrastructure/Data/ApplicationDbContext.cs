using Microsoft.EntityFrameworkCore;
using MeteoriteLandings.Domain.Entities;

namespace MeteoriteLandings.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MeteoriteLanding> MeteoriteLandings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MeteoriteLanding>(entity =>
            {
                entity.HasIndex(e => e.ExternalId).IsUnique();
            });
        }
    }
}
