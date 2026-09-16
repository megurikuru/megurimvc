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

            builder.Entity<Doc>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(u => u.Docs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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
                .WithMany()
                .HasForeignKey(dt => dt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.TagId);
                entity.HasIndex(t => t.NormalizedName)
                    .IsUnique();
            });

            builder.Entity<BoundTag>(entity =>
            {
                entity.HasKey(bt => bt.BoundId);

                entity.HasOne(bt => bt.MainTag)
                    .WithMany(t => t.MainBoundTags)
                    .HasForeignKey(bt => bt.MainTagId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bt => bt.ContextTag)
                    .WithMany(t => t.ContextBoundTags)
                    .HasForeignKey(bt => bt.ContextTagId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(bt => new { bt.MainTagId, bt.ContextTagId })
                    .IsUnique();
            });

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

            builder.Entity<ImageTag>()
                .HasIndex(it => new { it.ImageId, it.TagId })
                .IsUnique();

            builder.Entity<ImageTag>()
                .HasOne(it => it.Image)
                .WithMany(i => i.ImageTags)
                .HasForeignKey(it => it.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ImageTag>()
                .HasOne(it => it.Tag)
                .WithMany()
                .HasForeignKey(it => it.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Fandom>(entity =>
            {
                entity.HasOne(f => f.ParentFandom)
                    .WithMany(f => f.ChildFandoms)
                    .HasForeignKey(f => f.ParentFandomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(f => f.Users)
                    .WithMany(u => u.Fandoms)
                    .UsingEntity<FandomUser>(
                        j => j
                            .HasOne(fu => fu.User)
                            .WithMany(u => u.FandomUsers)
                            .HasForeignKey(fu => fu.UserId)
                            .OnDelete(DeleteBehavior.Cascade),
                        j => j
                            .HasOne(fu => fu.Fandom)
                            .WithMany(f => f.FandomUsers)
                            .HasForeignKey(fu => fu.FandomId)
                            .OnDelete(DeleteBehavior.Cascade),
                        j =>
                        {
                            j.HasKey(fu => new { fu.FandomId, fu.UserId });
                            j.ToTable("FandomUsers");
                        });
            });

            builder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Doc)
                    .WithMany(d => d.Comments)
                    .HasForeignKey(c => c.DocId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Image)
                    .WithMany(i => i.Comments)
                    .HasForeignKey(c => c.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Parent)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CommentImage>(entity =>
            {
                entity.HasOne(ci => ci.Comment)
                    .WithMany(c => c.CommentImages)
                    .HasForeignKey(ci => ci.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Image)
                    .WithMany(i => i.CommentImages)
                    .HasForeignKey(ci => ci.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ConversationMember>(entity =>
            {
                entity.HasOne(cm => cm.Conversation)
                    .WithMany(c => c.Members)
                    .HasForeignKey(cm => cm.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cm => cm.User)
                    .WithMany(u => u.ConversationMembers)
                    .HasForeignKey(cm => cm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<MessageImage>(entity =>
            {
                entity.HasOne(mi => mi.Message)
                    .WithMany(m => m.MessageImages)
                    .HasForeignKey(mi => mi.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mi => mi.Image)
                    .WithMany(i => i.MessageImages)
                    .HasForeignKey(mi => mi.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Reaction>(entity =>
            {
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reactions)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Doc)
                    .WithMany(d => d.Reactions)
                    .HasForeignKey(r => r.DocId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Comment)
                    .WithMany(c => c.Reactions)
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Image)
                    .WithMany(i => i.Reactions)
                    .HasForeignKey(r => r.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(r => r.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserImage>(entity =>
            {
                entity.HasOne(ui => ui.User)
                    .WithMany(u => u.UserImages)
                    .HasForeignKey(ui => ui.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ui => ui.Image)
                    .WithMany(i => i.UserImages)
                    .HasForeignKey(ui => ui.ImageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.ActiveAvatarImage)
                    .WithMany()
                    .HasForeignKey(u => u.ActiveAvatarImageId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Fandomマスターの初期シードデータ
            builder.Entity<Fandom>().HasData(
                new Fandom { Id = 1, Name = "イラスト" },
                new Fandom { Id = 2, Name = "漫画" },
                new Fandom { Id = 3, Name = "小説" },
                new Fandom { Id = 4, Name = "車" },
                new Fandom { Id = 5, Name = "プログラミング" },
                new Fandom { Id = 6, Name = "ゲーム" },
                new Fandom { Id = 7, Name = "コスプレ" }
            );

            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
