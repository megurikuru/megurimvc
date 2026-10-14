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
        // 3:イラスト 4:漫画(創作) 5:小説 7:アニメ 8:漫画(見る) 9:ゲーム 10:コスプレ 11:音楽 12:アイドル 15:トヨタ 17:ホンダ 24:BMW 27:IT 28:社会 30:運営
        // ルート界隈(Id=1)には投稿も画像も登録しない
        private const int RootFandomId = 1;

        private static readonly int[] FandomIds = new[] { 3, 4, 5, 7, 8, 9, 10, 11, 12, 15, 17, 24, 27, 28, 30 }
            .Where(id => id != RootFandomId).ToArray();

        // 画像付きで投稿されやすい界隈ID
        private static readonly HashSet<int> ImageFandomIds = new() { 3, 4, 7, 8, 9, 10, 11, 12, 15, 17, 24 };

        // ルート界隈には画像を付けない
        private static bool CanAttachImage(int fandomId) => fandomId != RootFandomId;

        private sealed record FandomContent(string[] Titles, string[] Bodies);

        // 界隈ごとの投稿内容
        private static readonly Dictionary<int, FandomContent> FandomContents = new() {
            [3] = new(
                new[] { "新作イラストを描きました", "キャラデザの練習中", "厚塗りに挑戦してみた", "ラフから完成までの工程" },
                new[] { "今回は光の表現を意識して描いてみました。ご意見・ご感想いただけると嬉しいです。", "ラフから線画、色塗りまでの工程をまとめました。塗りのコツがあれば教えてください。", "最近デジタルで厚塗りを練習しています。影の入れ方がまだ安定しないので、おすすめの講座や本があれば知りたいです。" }),
            [4] = new(
                new[] { "ネーム作業日記", "4コマ漫画を描きました", "読み切りの下書きが完成", "コマ割りで悩み中" },
                new[] { "ネームがなかなか進まず苦戦しています。見せ場のコマ割りをどうするか悩み中です。", "日常を題材にした4コマを描いてみました。オチの作り方についてアドバイスをいただけるとありがたいです。", "初めて読み切りの下書きを最後まで描き切りました。ペン入れ前に、読みにくい箇所がないか見ていただけませんか。" }),
            [5] = new(
                new[] { "短編小説を書きました", "プロットで悩み中", "長編の第1章を公開", "おすすめのミステリー小説は？" },
                new[] { "2000字ほどの短編を書き上げました。最後の一文に一番こだわったので、読んだ感想を聞かせてください。", "長編のプロットを練っていますが、中盤の展開が弱い気がしています。皆さんはどうやって山場を作っていますか。", "最近ミステリーにハマっています。どんでん返しが鮮やかな作品があれば、ぜひ教えてください。" }),
            [7] = new(
                new[] { "今期アニメの感想", "神作画回だった", "アニメ化決定で歓喜", "一気見した作品を語りたい" },                new[] { "今期は豊作で、毎週の楽しみが多くて困っています。特に最新話の戦闘シーンの作画は圧巻でした。", "ずっと原作を追っていた作品がアニメ化されると知って、ひとりで盛り上がっています。キャストも気になります。", "話題になっていたアニメを週末に一気見しました。伏線の回収が見事だったので、考察を語り合いたいです。" }),
            [8] = new(
                new[] { "新刊の告知", "単行本を全巻揃えた", "最新話の考察", "おすすめ漫画を教えて" },
                new[] { "推している漫画の新刊が今日発売でした。あの引きの続きがやっと読めるので、仕事帰りに買いに行きます。", "ついに完結まで全巻揃えました。一気に読み返して、改めて初期の伏線の細かさに驚きました。", "最近読むものがなくなってきたので、完結済みでおすすめの漫画があれば教えてください。ジャンルは問いません。" }),
            [9] = new(
                new[] { "積みゲー消化中", "クリアしました", "協力プレイ募集", "今週のプレイ記録" },
                new[] { "セールで買ったゲームがたまりにたまっていて、やっと一本目をクリアしました。ボス戦が熱かったです。", "週末に一緒に遊べる人を探しています。初心者歓迎なので、気軽に声をかけてください。", "最近は対戦ゲームにハマっていて、毎晩少しずつランクを上げています。立ち回りのコツがあれば教えてください。" }),
            [10] = new(
                new[] { "コスプレイベント参加のお知らせ", "撮影会の記録", "衣装制作の進捗", "ウィッグのセット方法" },
                new[] { "来月のコスプレイベントに参加します。当日は新しく作った衣装で出ますので、見かけたら気軽に声をかけてください。", "先日の撮影会で、ロケ地を借りて撮影してきました。屋外の光がとてもきれいで、満足のいく写真になりました。", "衣装の制作が半分ほど終わりました。パーツの造形が思ったより大変です。ウィッグのセットのコツも教えていただけると嬉しいです。" }),
            [11] = new(
                new[] { "作業用BGM募集", "弾いてみた動画を投稿", "ライブに行ってきた", "作曲に挑戦中" },
                new[] { "作業中に流せるおすすめのプレイリストを探しています。歌詞が少なめで集中できる曲だと嬉しいです。", "最近ギターを始めたので、好きな曲を弾いてみました。まだ拙いですが、アドバイスをお待ちしています。", "先日ライブに行ってきました。生演奏の迫力がすごくて、帰り道もずっと余韻に浸っていました。" }),
            [12] = new(
                new[] { "推しのグッズ開封", "ライブ遠征のレポ", "推し活の予算管理", "新曲が最高だった" },
                new[] { "届いたグッズを開封しました。ずっと欲しかったものなので、うれしくて何度も眺めています。", "初めてライブ遠征をしました。移動は大変でしたが、会場の盛り上がりは忘れられません。宿の選び方なども情報交換したいです。", "新曲が公開されて、ずっとリピートしています。サビの振付も気になるので、ライブが待ちきれません。" }),
            [15] = new(
                new[] { "週末ドライブ", "愛車の洗車をしました", "新型車の試乗レポ", "中古車選びで悩み中" },
                new[] { "週末に家族で郊外までドライブしてきました。燃費も良く、長距離でも疲れにくいのが助かります。", "久しぶりに洗車とワックスがけをしました。ピカピカになると気分が上がります。おすすめのコーティング剤はありますか。", "新型の試乗に行ってきました。加速が滑らかで、静粛性が前モデルより大きく向上していると感じました。" }),
            [17] = new(
                new[] { "愛車の自慢", "ツーリング報告", "車検の見積もりが出た", "カスタムの相談" },
                new[] { "納車からそろそろ一年になります。使い勝手が良くて、毎日の通勤でも相棒として活躍してくれています。", "紅葉を見に山道までドライブしました。コーナーでの安定感が気持ち良く、休憩を挟みながらゆっくり楽しめました。", "車検の見積もりが予想より高く出て悩んでいます。ディーラーと整備工場の違いについて、経験のある方の意見を聞きたいです。" }),
            [24] = new(
                new[] { "輸入車の維持費について", "サーキット走行してきた", "新型モデルの感想", "初めての輸入車" },
                new[] { "初めて輸入車を購入しました。維持費や消耗品の交換タイミングについて、先輩オーナーの方に教えていただきたいです。", "サーキットで走行会に参加してきました。ハンドリングの正確さに改めて感動しました。", "最新モデルを見てきました。内装の質感がかなり上がっていて、乗り換えを真剣に悩んでいます。" }),
            [27] = new(
                new[] { "個人開発の進捗", "C#の書き方で質問", "生成AIの活用方法", "自作PCを組みました" },
                new[] { "個人開発しているWebアプリの認証機能がひとまず動きました。次は検索機能を作る予定です。", "非同期処理の書き方で迷っています。例外処理とキャンセルの扱いについて、皆さんの設計方針を知りたいです。", "最近は生成AIをコードレビューの補助に使っています。便利な反面、確認の手間も増えるので、運用のコツがあれば教えてください。" }),
            [28] = new(
                new[] { "物価高について思うこと", "選挙制度の議論", "働き方改革の現状", "少子化対策はどうあるべきか" },
                new[] { "物価が上がり続けていて、家計への影響を強く感じています。皆さんはどのような工夫をしていますか。", "最近の選挙制度をめぐる議論を見て思うことがあります。投票率を上げるためには何が必要だと思いますか。", "在宅勤務やフレックスなど、働き方は多様になってきました。実際に取り入れて感じたメリットと課題を共有したいです。" }),
            [30] = new(
                new[] { "運営への要望です", "検索機能の改善案", "通報機能について", "使いやすくなりました" },
                new[] { "投稿の検索条件をもう少し細かく指定できると助かります。界隈やタグでの絞り込みを追加してほしいです。", "不適切な投稿を通報した後の対応状況が分からないため、結果が確認できる仕組みがあると安心できます。", "先日のアップデートで画像の表示が速くなり、とても使いやすくなりました。今後の改善も楽しみにしています。" })
        };

        // 全ユーザーが初期参加する界隈ID 1:ルート 29:なんでも 30:運営
        private static readonly int[] DefaultFandomIds = { 1, 29, 30 };

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
            "新しい機材を買ったので試し撮りしました。",
            "最近ずっと気になっていた作品をようやく一気に見終わりました。序盤はゆっくりした展開でしたが、中盤から一気に引き込まれて、気づいたら朝になっていました。特に終盤の演出がとても印象的で、見終わったあともしばらく余韻に浸っていました。同じ作品を見た方がいたら、好きなシーンや考察をぜひ聞かせてください。",
            "今週末は久しぶりに予定が空いたので、前から行きたかった場所へ出かけてきました。天気にも恵まれて、予定していたよりも多くの写真を撮ることができました。帰ってから見返してみると、思わぬところに良い瞬間が写っていて嬉しくなりました。次はもう少し遠出してみたいので、おすすめのスポットがあれば教えてください。",
            "ここ数週間、時間を見つけてコツコツ作業を進めてきた作品がひとまず形になりました。最初は思うように進まず何度もやり直しましたが、周りの方のアドバイスのおかげで方向性が定まりました。まだ改善できる点はたくさんあるので、率直な感想や気になった点があれば遠慮なくコメントしてもらえると助かります。",
            "初めてこのコミュニティに参加してみました。普段は一人で楽しんでいることが多かったのですが、同じものが好きな人と話してみたいと思い、思い切って投稿しています。好きになったきっかけや、最近のマイブームなど、いろいろな話ができたら嬉しいです。どうぞよろしくお願いします。"
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

        private sealed record ImageContext(IR2StorageService Storage, List<TestImageFile> Pool, string Directory) {
            public Dictionary<int, List<TestImageFile>> ByFandom { get; } = new();
        }

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

            var parentMap = await context.Set<Fandom>().ToDictionaryAsync(f => f.Id, f => f.ParentFandomId);
            var joined = new HashSet<(int FandomId, string UserId)>();
            foreach (var user in users) {
                foreach (var fandomId in DefaultFandomIds) {
                    JoinWithAncestors(context, parentMap, joined, fandomId, user.Id);
                }
                foreach (var fandomId in FandomIds.OrderBy(_ => random.Next()).Take(3)) {
                    JoinWithAncestors(context, parentMap, joined, fandomId, user.Id);
                }
            }
            await context.SaveChangesAsync();

            var imageDirectory = Path.Combine(contentRootPath, "Data", "TestImg");
            var pool = LoadImagePool(imageDirectory);
            var images = new ImageContext(storage, pool, imageDirectory);

            await SeedPostsAsync(context, users, random, now, images, parentMap, joined);
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
                    using var codec = SkiaSharp.SKCodec.Create(new MemoryStream(data));
                    if (codec != null) {
                        width = codec.Info.Width;
                        height = codec.Info.Height;
                    }
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
        private static async Task<Image?> CreateImageAsync(ApplicationUser user, int index, Random random, DateTime createdAt, ImageContext images, List<TestImageFile>? pool = null) {
            pool ??= images.Pool;
            if (pool.Count == 0) {
                return null;
            }
            var file = pool[random.Next(pool.Count)];
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
                Alt = file.FileName,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
        }

        /// <summary>
        /// 指定した界隈とその全祖先界隈にユーザーを参加させます（参加済みは無視）。
        /// </summary>
        private static void JoinWithAncestors(ApplicationDbContext context, Dictionary<int, int?> parentMap, HashSet<(int FandomId, string UserId)> joined, int fandomId, string userId) {
            int? current = fandomId;
            while (current.HasValue) {
                if (joined.Add((current.Value, userId))) {
                    context.Set<FandomUser>().Add(new FandomUser { FandomId = current.Value, UserId = userId });
                }
                current = parentMap.TryGetValue(current.Value, out var parent) ? parent : null;
            }
        }

        private static async Task SeedPostsAsync(ApplicationDbContext context, List<ApplicationUser> users, Random random, DateTime now, ImageContext images, Dictionary<int, int?> parentMap, HashSet<(int FandomId, string UserId)> joined) {
            var imageIndex = 1;
            for (var i = 0; i < 100; i++) {
                var user = users[random.Next(users.Count)];
                var createdAt = now.AddHours(-random.Next(1, 24 * 60));
                var postFandomId = FandomIds[random.Next(FandomIds.Length)];
                JoinWithAncestors(context, parentMap, joined, postFandomId, user.Id);
                var content = FandomContents[postFandomId];
                var post = new Post {
                    User = user,
                    FandomId = postFandomId,
                    Subject = $"{content.Titles[random.Next(content.Titles.Length)]} #{i + 1}",
                    Body = content.Bodies[random.Next(content.Bodies.Length)],
                    IsPublic = true,
                    ViewCount = random.Next(0, 500),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };
                context.Set<Post>().Add(post);

                // 画像系の界隈は約9割、それ以外は約2割の投稿に画像を1〜2枚添付
                // 画像は Data/TestImg/{界隈ID} フォルダがあればそこから、なければ Data/TestImg 直下から選ぶ
                var imageRate = ImageFandomIds.Contains(postFandomId) ? 9 : 2;
                if (CanAttachImage(postFandomId) && random.Next(10) < imageRate) {
                    if (!images.ByFandom.TryGetValue(postFandomId, out var fandomPool)) {
                        fandomPool = LoadImagePool(Path.Combine(images.Directory, postFandomId.ToString()));
                        images.ByFandom[postFandomId] = fandomPool;
                    }
                    var count = random.Next(1, 3);
                    for (var order = 0; order < count; order++) {
                        var image = await CreateImageAsync(user, imageIndex++, random, createdAt, images, fandomPool.Count > 0 ? fandomPool : null);
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
                var created = new List<Message>();
                for (var m = 0; m < messageCount; m++) {
                    var sender = members[random.Next(members.Count)];
                    var createdAt = now.AddMinutes(-(messageCount - m) * random.Next(30, 600));
                    var parent = created.Count > 0 && random.Next(10) < 4 ? created[random.Next(created.Count)] : null;
                    var subject = parent == null ? Titles[random.Next(Titles.Length)] : (parent.Subject.StartsWith("Re: ") ? parent.Subject : "Re: " + parent.Subject);
                    var message = new Message {
                        Conversation = conversation,
                        Sender = sender,
                        ParentMessage = parent,
                        Subject = subject,
                        Body = ChatTexts[random.Next(ChatTexts.Length)],
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt
                    };
                    context.Set<Message>().Add(message);
                    created.Add(message);

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
