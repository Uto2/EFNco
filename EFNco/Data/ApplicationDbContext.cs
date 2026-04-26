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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename Identity tables
            builder.Entity<ApplicationUser>().ToTable("Users");

            // Vehicle → one active permit at a time
            builder.Entity<Vehicle>()
                .HasOne(v => v.Permit)
                .WithOne(p => p.Vehicle)
                .HasForeignKey<ParkingPermit>(p => p.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vehicle → Owner (restrict delete so we don't lose history)
            builder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Permit → Applicant
            builder.Entity<ParkingPermit>()
                .HasOne(p => p.Applicant)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Permit → ReviewedBy
            builder.Entity<ParkingPermit>()
                .HasOne(p => p.ReviewedBy)
                .WithMany()
                .HasForeignKey(p => p.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
