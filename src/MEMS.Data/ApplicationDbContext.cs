using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantContext _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public bool IsUnrestricted => _tenant.IsUnrestricted;
    public int? CurrentTenantId => _tenant.TenantId;

    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Endorsement> Endorsements => Set<Endorsement>();
    public DbSet<StoredImage> Images => Set<StoredImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Municipality>(entity =>
        {
            entity.Property(m => m.Name).HasMaxLength(100).IsRequired();
            entity.Property(m => m.Slug).HasMaxLength(50).IsRequired();
            entity.Property(m => m.Province).HasMaxLength(50).IsRequired();
            entity.Property(m => m.Description).HasMaxLength(2000);
            entity.Property(m => m.Tagline).HasMaxLength(150);
            entity.Property(m => m.ThemeColor).HasMaxLength(7).IsRequired();
            entity.Property(m => m.ElectionDate).HasColumnType("date");

            entity.HasIndex(m => m.Name).IsUnique();
            entity.HasIndex(m => m.Slug).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Municipalities_Slug_Format",
                    "LEN([Slug]) >= 2 AND [Slug] NOT LIKE '%[^a-z0-9-]%' AND [Slug] NOT LIKE '-%' AND [Slug] NOT LIKE '%-'");
                table.HasCheckConstraint("CK_Municipalities_ThemeColor_Hex",
                    "[ThemeColor] LIKE '#[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]'");
                table.HasCheckConstraint("CK_Municipalities_ElectionDate_Range",
                    "[ElectionDate] >= '2000-01-01' AND [ElectionDate] < '2101-01-01'");
            });

            entity.HasOne(m => m.Logo)
                .WithMany()
                .HasForeignKey(m => m.LogoImageId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(m => IsUnrestricted || m.MunicipalityId == CurrentTenantId);
        });

        builder.Entity<Position>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();

            entity.HasIndex(p => new { p.MunicipalityId, p.Name }).IsUnique();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Positions_SeatsAvailable_Range", "[SeatsAvailable] BETWEEN 1 AND 100"));

            entity.HasOne(p => p.Municipality)
                .WithMany(m => m.Positions)
                .HasForeignKey(p => p.MunicipalityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(p => IsUnrestricted || p.MunicipalityId == CurrentTenantId);
        });

        builder.Entity<Candidate>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Profile).HasMaxLength(2000);
            entity.Property(c => c.Website).HasMaxLength(300);
            entity.Property(c => c.Email).HasMaxLength(254);
            entity.Property(c => c.VideoUrl).HasMaxLength(300);
            entity.Property(c => c.SocialX).HasMaxLength(300);
            entity.Property(c => c.SocialFacebook).HasMaxLength(300);
            entity.Property(c => c.SocialInstagram).HasMaxLength(300);
            entity.Property(c => c.SocialLinkedIn).HasMaxLength(300);

            entity.HasIndex(c => new { c.MunicipalityId, c.Name });

            entity.HasOne(c => c.Position)
                .WithMany(p => p.Candidates)
                .HasForeignKey(c => new { c.PositionId, c.MunicipalityId })
                .HasPrincipalKey(p => new { p.PositionId, p.MunicipalityId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Municipality)
                .WithMany(m => m.Candidates)
                .HasForeignKey(c => c.MunicipalityId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Photo)
                .WithMany()
                .HasForeignKey(c => c.PhotoImageId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(c => IsUnrestricted || c.MunicipalityId == CurrentTenantId);
        });

        builder.Entity<Endorsement>(entity =>
        {
            entity.Property(e => e.EndorserName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Organization).HasMaxLength(150);
            entity.Property(e => e.Text).HasMaxLength(1000);

            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.Endorsements)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => IsUnrestricted || e.Candidate!.MunicipalityId == CurrentTenantId);
        });

        builder.Entity<StoredImage>(entity =>
        {
            entity.Property(i => i.ContentType).HasMaxLength(50).IsRequired();
            entity.Property(i => i.Data).IsRequired();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Images_Length_Range", "[Length] > 0 AND [Length] <= 5242880"));

            entity.HasOne(i => i.Municipality)
                .WithMany()
                .HasForeignKey(i => i.MunicipalityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(i => IsUnrestricted || i.MunicipalityId == CurrentTenantId);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Municipality)
                .WithMany(m => m.Administrators)
                .HasForeignKey(u => u.MunicipalityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
