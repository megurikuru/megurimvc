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

        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Fandom> Fandoms { get; set; } = null!;
        public DbSet<FandomUser> FandomUsers { get; set; } = null!;
        public DbSet<Image> Images { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<BoundTag> BoundTags { get; set; } = null!;
        public DbSet<PostTag> PostTags { get; set; } = null!;
        public DbSet<PostImage> PostImages { get; set; } = null!;
        public DbSet<ImageTag> ImageTags { get; set; } = null!;
        public DbSet<UserImage> UserImages { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<CommentImage> CommentImages { get; set; } = null!;
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<ConversationMember> ConversationMembers { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<MessageImage> MessageImages { get; set; } = null!;
        public DbSet<Reaction> Reactions { get; set; } = null!;

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

            builder.Entity<Post>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(u => u.Docs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PostTag>()
                .HasIndex(dt => new { dt.PostId, dt.TagId })
                .IsUnique();

            builder.Entity<PostTag>()
                .HasOne(dt => dt.Post)
                .WithMany(d => d.PostTags)
                .HasForeignKey(dt => dt.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostTag>()
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

            builder.Entity<PostImage>()
                .HasIndex(pi => new { pi.PostId, pi.ImageId })
                .IsUnique();

            builder.Entity<PostImage>()
                .HasOne(pi => pi.Post)
                .WithMany(d => d.PostImages)
                .HasForeignKey(pi => pi.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostImage>()
                .HasOne(pi => pi.Image)
                .WithMany(i => i.PostImages)
                .HasForeignKey(pi => pi.ImageId)
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

                entity.HasOne(r => r.Post)
                    .WithMany(d => d.Reactions)
                    .HasForeignKey(r => r.PostId)
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
                new Fandom { Id = 1, Name = "つぶやき", ParentFandomId = null },
                new Fandom { Id = 2, Name = "創作", ParentFandomId = null },
                new Fandom { Id = 3, Name = "イラスト", ParentFandomId = 2 },
                new Fandom { Id = 4, Name = "漫画", ParentFandomId = 2 },
                new Fandom { Id = 5, Name = "小説", ParentFandomId = 2 },
                new Fandom { Id = 6, Name = "コスプレ", ParentFandomId = null },
                new Fandom { Id = 7, Name = "車", ParentFandomId = null },
                new Fandom { Id = 8, Name = "国産車", ParentFandomId = 7 },
                new Fandom { Id = 9, Name = "輸入車", ParentFandomId = 7 },
                new Fandom { Id = 10, Name = "漫画・アニメ", ParentFandomId = null },
                new Fandom { Id = 11, Name = "漫画", ParentFandomId = 10 },
                new Fandom { Id = 12, Name = "アニメ", ParentFandomId = 10 },
                new Fandom { Id = 13, Name = "ゲーム", ParentFandomId = null },
                new Fandom { Id = 14, Name = "プログラミング", ParentFandomId = null },
                new Fandom { Id = 15, Name = "運営", ParentFandomId = null }
            );

            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
