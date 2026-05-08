using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EFNco.Models;

namespace EFNco.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ParkingPermit> ParkingPermits { get; set; }
        public DbSet<EntryExitLog> EntryExitLogs { get; set; }  // Sprint 4

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");

            // Vehicle → one active permit at a time
            builder.Entity<Vehicle>()
                .HasOne(v => v.Permit)
                .WithOne(p => p.Vehicle)
                .HasForeignKey<ParkingPermit>(p => p.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ParkingPermit>()
                .HasOne(p => p.Applicant)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ParkingPermit>()
                .HasOne(p => p.ReviewedBy)
                .WithMany()
                .HasForeignKey(p => p.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // EntryExitLog → Permit (nullable — log survives permit deletion)
            builder.Entity<EntryExitLog>()
                .HasOne(l => l.Permit)
                .WithMany()
                .HasForeignKey(l => l.PermitId)
                .OnDelete(DeleteBehavior.SetNull);

            // EntryExitLog → VerifiedBy guard
            builder.Entity<EntryExitLog>()
                .HasOne(l => l.VerifiedBy)
                .WithMany()
                .HasForeignKey(l => l.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
