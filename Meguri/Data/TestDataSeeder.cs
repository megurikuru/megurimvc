using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meguri.Models;
using Meguri.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Data {

    /// <summary>
    /// 動作確認用のテストデータ（ユーザー・投稿・画像・メッセージ）を登録するクラス
    /// </summary>
    public static class TestDataSeeder {

        private const string TestUserPrefix = "testuser";
        private const string TestPassword = "Test1234!";

        // 投稿先の界隈ID（DbInitializerの初期データに対応）
        // 3:つぶやき 5:イラスト 6:漫画 7:小説 8:コスプレ 11:トヨタ 12:ホンダ 20:アニメ 21:ゲーム 22:プログラミング
        private static readonly int[] FandomIds = { 3, 5, 6, 7, 8, 11, 12, 20, 21, 22 };

        private static readonly string[] UserNames = {
            "メグリ", "ゆきのん", "アニ吉", "コス姫", "ドライブ太郎",
            "ゲー廃", "小説家の卵", "漫画描き", "コード猫", "推し活民"
        };

        private static readonly string[] Titles = {
            "今期アニメの感想", "描いてみたイラスト", "新刊の告知", "コスプレ撮影会の記録", "週末ドライブ",
            "積みゲー消化中", "プロットで悩み中", "ネーム作業日記", "推しのグッズ開封", "作業用BGM募集",
            "初投稿です", "神回だった", "イベント参加レポ", "今日のつぶやき", "おすすめ作品教えて"
        };

        private static readonly string[] Bodies = {
            "テスト用の投稿です。動作確認のため自動で登録されています。",
            "最近ハマっている作品について語りたいです。みなさんのおすすめも教えてください。",
            "ようやく完成しました。感想をいただけると嬉しいです。",
            "今日は推しの話で一日盛り上がりました。尊い。",
            "締切が近いのに手が進まない…。応援してください。",
            "久しぶりにイベントに行ってきました。楽しかった！",
            "同じ趣味の人と繋がりたいです。気軽に話しかけてください。",
            "新しい機材を買ったので試し撮りしました。"
        };

        private static readonly string[] ChatTexts = {
            "お疲れさま！今日の更新見たよ", "ありがとう！そう言ってもらえると嬉しい", "次のイベントはいつ参加する？",
            "週末にみんなで集まらない？", "新作の進捗どう？", "ちょっと詰まってるけど頑張る！",
            "この前おすすめしてくれた作品、最高だった", "でしょ？語りたいことがいっぱいある", "了解、また連絡するね",
            "画像送るね、見てみて", "めっちゃいい！保存した", "来月の合同誌、参加する？"
        };

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase) {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        private sealed record TestImageFile(string FileName, byte[] Data, string ContentType, int Width, int Height);

        private sealed record ImageContext(IR2StorageService Storage, List<TestImageFile> Pool);

        /// <summary>
        /// テストデータが未登録の場合のみ登録します。
        /// 画像は Data/TestImg 配下のファイルを各ユーザーの画像としてR2へアップロードして登録します。
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context, IR2StorageService storage, string contentRootPath) {
            if (await context.Users.AnyAsync(u => u.UserName!.StartsWith(TestUserPrefix))) {
                return;
            }

            var random = new Random(20240101);
            var now = DateTime.UtcNow;

            var users = CreateUsers(now);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            foreach (var user in users) {
                foreach (var fandomId in FandomIds.OrderBy(_ => random.Next()).Take(3)) {
                    context.Set<FandomUser>().Add(new FandomUser { FandomId = fandomId, UserId = user.Id });
                }
            }
            await context.SaveChangesAsync();

            var pool = LoadImagePool(Path.Combine(contentRootPath, "Data", "TestImg"));
            var images = new ImageContext(storage, pool);

            await SeedPostsAsync(context, users, random, now, images);
            await SeedCommentsAsync(context, users, random, now);
            await SeedMessagesAsync(context, users, random, now, images);
        }

        private static readonly string[] CommentTexts = {
            "いいですね！", "楽しく読ませてもらいました。", "素敵です、参考になります。", "わかります、自分もハマってます！",
            "続きが気になります。", "応援してます！", "これは尊い…", "情報ありがとうございます。"
        };

        /// <summary>
        /// 登録済みのテスト投稿・画像に対してテストユーザーのコメントを登録します。
        /// </summary>
        private static async Task SeedCommentsAsync(ApplicationDbContext context, List<ApplicationUser> users, Random random, DateTime now) {
            var posts = await context.Set<Post>().Where(p => users.Select(u => u.Id).Contains(p.UserId)).ToListAsync();
            foreach (var post in posts) {
                var count = random.Next(0, 5);
                for (var n = 1; n <= count; n++) {
                    var createdAt = post.CreatedAt.AddMinutes(random.Next(10, 60 * 24 * 5));
                    if (createdAt > now) {
                        createdAt = now;
                    }
                    context.Set<Comment>().Add(new Comment {
                        User = users[random.Next(users.Count)],
                        DocId = post.Id,
                        Number = n,
                        Text = CommentTexts[random.Next(CommentTexts.Length)],
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    });
                }
            }
            await context.SaveChangesAsync();

            var postImages = await context.Set<PostImage>().Select(pi => pi.Image).ToListAsync();
            foreach (var image in postImages) {
                var count = random.Next(0, 4);
                for (var n = 1; n <= count; n++) {
                    var createdAt = image.CreatedAt.AddMinutes(random.Next(10, 60 * 24 * 5));
                    if (createdAt > now) {
                        createdAt = now;
                    }
                    context.Set<Comment>().Add(new Comment {
                        User = users[random.Next(users.Count)],
                        ImageId = image.Id,
                        Number = n,
                        Text = CommentTexts[random.Next(CommentTexts.Length)],
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Data/TestImg 配下の画像ファイルを読み込みます。フォルダがない場合は空になります。
        /// </summary>
        private static List<TestImageFile> LoadImagePool(string directory) {
            var pool = new List<TestImageFile>();
            if (!Directory.Exists(directory)) {
                return pool;
            }
            foreach (var path in Directory.EnumerateFiles(directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) {
                if (!ContentTypes.TryGetValue(Path.GetExtension(path), out var contentType)) {
                    continue;
                }
                var data = File.ReadAllBytes(path);
                int width = 0, height = 0;
                try {
                    var info = SixLabors.ImageSharp.Image.Identify(data);
                    width = info.Width;
                    height = info.Height;
                } catch (Exception) {
                    // 寸法が取得できない画像はサイズ0のまま登録する
                }
                pool.Add(new TestImageFile(Path.GetFileName(path), data, contentType, width, height));
            }
            return pool;
        }

        private static List<ApplicationUser> CreateUsers(DateTime now) {
            var hasher = new PasswordHasher<ApplicationUser>();
            var users = new List<ApplicationUser>();
            for (var i = 0; i < UserNames.Length; i++) {
                var userName = $"{TestUserPrefix}{i + 1:00}";
                var email = $"{userName}@example.com";
                var user = new ApplicationUser {
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    DateOfBirth = new DateOnly(1990 + i, (i % 12) + 1, 10),
                    Bio = $"{UserNames[i]}です。テスト用アカウントです。",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                user.PasswordHash = hasher.HashPassword(user, TestPassword);
                users.Add(user);
            }
            return users;
        }

        /// <summary>
        /// TestImg の画像をユーザーの画像としてR2へアップロードし、Imageを作成します。画像がない場合はnullを返します。
        /// </summary>
        private static async Task<Image?> CreateImageAsync(ApplicationUser user, int index, Random random, DateTime createdAt, ImageContext images) {
            if (images.Pool.Count == 0) {
                return null;
            }
            var file = images.Pool[random.Next(images.Pool.Count)];
            var key = $"{user.Id}/testdata-{index:0000}{Path.GetExtension(file.FileName)}";
            using (var stream = new MemoryStream(file.Data)) {
                await images.Storage.UploadFileAsync(stream, key, file.ContentType);
            }
            return new Image {
                User = user,
                Name = file.FileName,
                StorageKey = key,
                ContentType = file.ContentType,
                FileSize = file.Data.Length,
                Width = file.Width,
                Height = file.Height,
                Description = $"テスト画像 {index}",
                Caption = $"テスト画像 {index}",
                IsPublic = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
        }

        private static async Task SeedPostsAsync(ApplicationDbContext context, List<ApplicationUser> users, Random random, DateTime now, ImageContext images) {
            var imageIndex = 1;
            for (var i = 0; i < 100; i++) {
                var user = users[random.Next(users.Count)];
                var createdAt = now.AddHours(-random.Next(1, 24 * 60));
                var post = new Post {
                    User = user,
                    FandomId = FandomIds[random.Next(FandomIds.Length)],
                    Name = $"{Titles[random.Next(Titles.Length)]} #{i + 1}",
                    Text = Bodies[random.Next(Bodies.Length)],
                    IsPublic = true,
                    ViewCount = random.Next(0, 500),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };
                context.Set<Post>().Add(post);

                // 約6割の投稿に画像を1〜2枚添付
                if (random.Next(10) < 6) {
                    var count = random.Next(1, 3);
                    for (var order = 0; order < count; order++) {
                        var image = await CreateImageAsync(user, imageIndex++, random, createdAt, images);
                        if (image != null) {
                            context.Set<PostImage>().Add(new PostImage { Post = post, Image = image, DisplayOrder = order });
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedMessagesAsync(ApplicationDbContext context, List<ApplicationUser> users, Random random, DateTime now, ImageContext images) {
            var conversations = new List<(Conversation Conversation, List<ApplicationUser> Members)>();

            // 1対1のDM（5件）
            for (var i = 0; i < 5; i++) {
                var members = new List<ApplicationUser> { users[i * 2], users[i * 2 + 1] };
                conversations.Add((new Conversation { Title = string.Empty, IsGroup = false, CreatedAt = now.AddDays(-20), UpdatedAt = now }, members));
            }

            // グループメッセージ（3件）
            var groups = new[] {
                ("イラスト部屋", new[] { 0, 1, 2, 3 }),
                ("ドライブ&ゲーム好き", new[] { 4, 5, 6, 7, 8 }),
                ("全員集合", new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })
            };
            foreach (var (title, indexes) in groups) {
                var members = indexes.Select(idx => users[idx]).ToList();
                conversations.Add((new Conversation { Title = title, IsGroup = true, CreatedAt = now.AddDays(-25), UpdatedAt = now }, members));
            }

            var imageIndex = 1000;
            foreach (var (conversation, members) in conversations) {
                context.Set<Conversation>().Add(conversation);
                foreach (var member in members) {
                    context.Set<ConversationMember>().Add(new ConversationMember {
                        Conversation = conversation,
                        User = member,
                        JoinedAt = conversation.CreatedAt,
                        LastReadAt = now.AddHours(-random.Next(0, 48))
                    });
                }

                var messageCount = conversation.IsGroup ? 25 : 15;
                for (var m = 0; m < messageCount; m++) {
                    var sender = members[random.Next(members.Count)];
                    var createdAt = now.AddMinutes(-(messageCount - m) * random.Next(30, 600));
                    var message = new Message {
                        Conversation = conversation,
                        Sender = sender,
                        Text = ChatTexts[random.Next(ChatTexts.Length)],
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    };
                    context.Set<Message>().Add(message);

                    // 約1割のメッセージに画像を添付
                    if (random.Next(10) == 0) {
                        var image = await CreateImageAsync(sender, imageIndex++, random, createdAt, images);
                        if (image != null) {
                            context.Set<MessageImage>().Add(new MessageImage { Message = message, Image = image, DisplayOrder = 0 });
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
