using Microsoft.EntityFrameworkCore;
using mpesaIntegration.Models.Authentication;
using mpesaIntegration.Models.Payments;

namespace mpesaIntegration.Data
{
    /// <summary>
    /// Database context for the application using Entity Framework Core
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext
        /// </summary>
        /// <param name="options">Database context options</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Users table configuration
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Payments table configuration
        /// </summary>
        public DbSet<Payment> Payments { get; set; }

        /// <summary>
        /// Configures the database connection and other model settings
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=ep-silent-frost-a8yk1rxt-pooler.eastus2.azure.neon.tech;Database=mpesaintergration;Username=mpesaintergration_owner;Password=npg_U7Dj1KGNhMAf;SSL Mode=Require;Trust Server Certificate=true");
            }
        }

        /// <summary>
        /// Configures entity relationships and constraints
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>();
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Status).HasConversion<string>();
            });
        }
    }
}