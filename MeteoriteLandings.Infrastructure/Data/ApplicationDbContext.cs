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
                // Unique index for external ID from NASA API
                entity.HasIndex(e => e.ExternalId).IsUnique();
                
                // Performance indexes for filtering queries
                entity.HasIndex(e => e.Year)
                    .HasDatabaseName("IX_MeteoriteLandings_Year");
                    
                entity.HasIndex(e => e.RecClass)
                    .HasDatabaseName("IX_MeteoriteLandings_RecClass");
                    
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_MeteoriteLandings_Name");
                    
                // Composite index for common filter combinations
                entity.HasIndex(e => new { e.Year, e.RecClass })
                    .HasDatabaseName("IX_MeteoriteLandings_Year_RecClass");
                    
                // Index for data sync operations
                entity.HasIndex(e => e.UpdatedAt)
                    .HasDatabaseName("IX_MeteoriteLandings_UpdatedAt");
            });
        }
    }
}
