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

        // ── Existing ──────────────────────────────────────────
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ParkingPermit> ParkingPermits { get; set; }
        public DbSet<EntryExitLog> EntryExitLogs { get; set; }
        public DbSet<Violation> Violations { get; set; }
        public DbSet<ViolationAppeal> ViolationAppeals { get; set; }
        public DbSet<AppNotification> AppNotifications { get; set; }

        // ✅ Sprint 7 — New
        public DbSet<AuthorizedPerson> AuthorizedPersons { get; set; }
        public DbSet<ParkingDurationSetting> ParkingDurationSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");

            // ── Vehicle ───────────────────────────────────────
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

            // ── ParkingPermit ─────────────────────────────────
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

            // ── EntryExitLog ──────────────────────────────────
            builder.Entity<EntryExitLog>()
                .HasOne(l => l.Permit)
                .WithMany()
                .HasForeignKey(l => l.PermitId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<EntryExitLog>()
                .HasOne(l => l.VerifiedBy)
                .WithMany()
                .HasForeignKey(l => l.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Violation ─────────────────────────────────────
            builder.Entity<Violation>()
                .HasOne(v => v.Permit)
                .WithMany()
                .HasForeignKey(v => v.PermitId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Violation>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Violation>()
                .HasOne(v => v.IssuedBy)
                .WithMany()
                .HasForeignKey(v => v.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Violation>()
                .HasOne(v => v.Appeal)
                .WithOne(a => a.Violation)
                .HasForeignKey<ViolationAppeal>(a => a.ViolationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── ViolationAppeal ───────────────────────────────
            builder.Entity<ViolationAppeal>()
                .HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── AppNotification ───────────────────────────────
            builder.Entity<AppNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── AuthorizedPerson ──────────────────────────────
            builder.Entity<AuthorizedPerson>()
                .HasOne(a => a.Permit)
                .WithMany(p => p.AuthorizedPersons)
                .HasForeignKey(a => a.PermitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AuthorizedPerson>()
                .HasOne(a => a.AddedBy)
                .WithMany()
                .HasForeignKey(a => a.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── ParkingDurationSetting ────────────────────────
            // One row per PermitType — enforce uniqueness
            builder.Entity<ParkingDurationSetting>()
                .HasIndex(s => s.PermitType)
                .IsUnique();

            builder.Entity<ParkingDurationSetting>()
                .HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
