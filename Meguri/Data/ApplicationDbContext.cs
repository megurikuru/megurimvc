using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Meguri.Models;

namespace Meguri.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {
        }

        public override int SaveChanges() {
            ApplyAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            ApplyAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditFields() {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries()) {
                if (entry.Entity == null) {
                    continue;
                }

                var entityType = entry.Entity.GetType();
                var createdProperty = entityType.GetProperty("Created");
                var updatedProperty = entityType.GetProperty("Updated");

                if (entry.State == EntityState.Added) {
                    if (createdProperty != null && createdProperty.PropertyType == typeof(DateTime)) {
                        createdProperty.SetValue(entry.Entity, now);
                    }
                }

                if (updatedProperty != null && updatedProperty.PropertyType == typeof(DateTime)) {
                    updatedProperty.SetValue(entry.Entity, now);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.Entity<DocTag>()
                .HasIndex(dt => new { dt.DocId, dt.TagId })
                .IsUnique();

            builder.Entity<DocTag>()
                .HasOne(dt => dt.Doc)
                .WithMany(d => d.DocTags)
                .HasForeignKey(dt => dt.DocId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DocTag>()
                .HasOne(dt => dt.Tag)
                .WithMany(t => t.DocTags)
                .HasForeignKey(dt => dt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Tag>()
                .HasOne(t => t.ParentTag)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentTagId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Tag>()
                .HasIndex(t => new { t.Name, t.ParentTagId })
                .IsUnique();

            builder.Entity<DocImage>()
                .HasIndex(di => new { di.DocId, di.ImageId })
                .IsUnique();

            builder.Entity<DocImage>()
                .HasOne(di => di.Doc)
                .WithMany(d => d.DocImages)
                .HasForeignKey(di => di.DocId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DocImage>()
                .HasOne(di => di.Image)
                .WithMany(i => i.DocImages)
                .HasForeignKey(di => di.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
