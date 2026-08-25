using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Servit.Domain.Entities;

namespace Servit.Infrastructure.Persistence;

public class ServitDbContext(DbContextOptions<ServitDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProviderCategory> ProviderCategories => Set<ProviderCategory>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ProviderResponse> ProviderResponses => Set<ProviderResponse>();
    public DbSet<ServiceRequestAttachment> ServiceRequestAttachments => Set<ServiceRequestAttachment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Provider>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithOne(u => u.Provider)
                .HasForeignKey<Provider>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.UserId).IsUnique();

            entity.Property(p => p.Location)
                .HasColumnType("geography (point)");
        });

        builder.Entity<ProviderCategory>(entity =>
        {
            entity.HasKey(pc => new { pc.ProviderId, pc.CategoryId });

            entity.HasOne(pc => pc.Provider)
                .WithMany(p => p.ProviderCategories)
                .HasForeignKey(pc => pc.ProviderId);

            entity.HasOne(pc => pc.Category)
                .WithMany(c => c.ProviderCategories)
                .HasForeignKey(pc => pc.CategoryId);
        });

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Gasfitería" },
            new Category { Id = 2, Name = "Carpintería" },
            new Category { Id = 3, Name = "Melaminería" },
            new Category { Id = 4, Name = "Electricidad" },
            new Category { Id = 5, Name = "Pintura" }
        );

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasOne(sr => sr.Customer)
                .WithMany()
                .HasForeignKey(sr => sr.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sr => sr.Category)
                .WithMany()
                .HasForeignKey(sr => sr.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(sr => sr.Location)
                .HasColumnType("geography (point)");

            entity.Property(sr => sr.Status)
                .HasConversion<string>();
        });

        builder.Entity<ProviderResponse>(entity =>
        {
            entity.HasOne(pr => pr.ServiceRequest)
                .WithMany(sr => sr.Responses)
                .HasForeignKey(pr => pr.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pr => pr.Provider)
                .WithMany()
                .HasForeignKey(pr => pr.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(pr => pr.Status)
                .HasConversion<string>();

            entity.HasIndex(pr => new { pr.ServiceRequestId, pr.ProviderId })
                .IsUnique();
        });

        builder.Entity<ServiceRequestAttachment>(entity =>
        {
            entity.HasOne(a => a.ServiceRequest)
                .WithMany(sr => sr.Attachments)
                .HasForeignKey(a => a.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.Type)
                .HasConversion<string>();
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.ServiceRequest)
                .WithMany()
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.ServiceRequestId).IsUnique();

            entity.HasOne(r => r.Provider)
                .WithMany()
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
