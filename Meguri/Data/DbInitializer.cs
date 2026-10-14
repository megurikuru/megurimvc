using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Meguri.Models;

namespace Meguri.Data {

    /// <summary>
    /// データベースの全データ削除と初期データ登録（シード）処理を提供するクラス
    /// </summary>
    public static class DbInitializer {

        /// <summary>
        /// ユーザー・認証情報を含む全管理テーブルのデータを削除し、連番をリセットします。
        /// テーブル構造とマイグレーション履歴は保持します。
        /// </summary>
        public static async Task DeleteAllDataAsync(ApplicationDbContext context) {
            if (!context.Database.IsNpgsql()) {
                throw new NotSupportedException("全データ削除はPostgreSQLのみ対応しています。");
            }

            var sqlHelper = context.GetService<ISqlGenerationHelper>();
            var tableNames = context.Model.GetRelationalModel().Tables
                .Select(table => sqlHelper.DelimitIdentifier(table.Name, table.Schema))
                .ToArray();

            if (tableNames.Length == 0) {
                return;
            }

            var sql = "TRUNCATE TABLE " + string.Join(", ", tableNames) + " RESTART IDENTITY;";
            await context.Database.ExecuteSqlRawAsync(sql);
            context.ChangeTracker.Clear();
        }

        /// <summary>
        /// 設定で有効な場合のみ、既存データを削除せずに初期データを登録します。
        /// </summary>
        public static async Task InitializeAsync(ApplicationDbContext context, IConfiguration configuration, Meguri.Services.IR2StorageService? storage = null, string? contentRootPath = null) {
            if (!configuration.GetValue<bool>("DatabaseInitialization:Enabled")) {
                return;
            }

            await InitializeAsync(context);

            if (configuration.GetValue<bool>("DatabaseInitialization:SeedTestData")) {
                if (storage == null || contentRootPath == null) {
                    throw new InvalidOperationException("テストデータの登録には画像ストレージとコンテンツルートのパスが必要です。");
                }
                await TestDataSeeder.SeedAsync(context, storage, contentRootPath);
            }
        }

        /// <summary>
        /// Fandomモデルなどのマスター初期データを登録します。
        /// </summary>
        public static async Task InitializeAsync(ApplicationDbContext context) {
            await InitializeFandomsAsync(context);
            await InitializeTagsAsync(context);
        }

        /// <summary>
        /// 界隈の初期データを登録・更新します。
        /// </summary>
        private static async Task InitializeFandomsAsync(ApplicationDbContext context) {
            const int rootFandomId = 1;
            // 初期登録するFandom一覧
            var initialFandoms = new[] {
                new Fandom { Id = rootFandomId, Name = "ルート", ParentFandomId = null },
                new Fandom { Id = 2, Name = "創作（イラスト・漫画等）", ParentFandomId = rootFandomId },
                new Fandom { Id = 3, Name = "イラスト", ParentFandomId = 2 },
                new Fandom { Id = 4, Name = "漫画", ParentFandomId = 2 },
                new Fandom { Id = 5, Name = "小説", ParentFandomId = 2 },
                new Fandom { Id = 6, Name = "見る・読む（アニメ・漫画等）", ParentFandomId = rootFandomId },
                new Fandom { Id = 7, Name = "アニメ", ParentFandomId = 6 },
                new Fandom { Id = 8, Name = "漫画", ParentFandomId = 6 },
                new Fandom { Id = 9, Name = "ゲーム", ParentFandomId = rootFandomId },
                new Fandom { Id = 10, Name = "コスプレ", ParentFandomId = rootFandomId },
                new Fandom { Id = 11, Name = "音楽", ParentFandomId = rootFandomId },
                new Fandom { Id = 12, Name = "アイドル", ParentFandomId = rootFandomId },
                new Fandom { Id = 13, Name = "自動車", ParentFandomId = rootFandomId },
                new Fandom { Id = 14, Name = "国産車", ParentFandomId = 13 },
                new Fandom { Id = 15, Name = "トヨタ", ParentFandomId = 14 },
                new Fandom { Id = 16, Name = "日産", ParentFandomId = 14 },
                new Fandom { Id = 17, Name = "ホンダ", ParentFandomId = 14 },
                new Fandom { Id = 18, Name = "マツダ", ParentFandomId = 14 },
                new Fandom { Id = 19, Name = "スバル", ParentFandomId = 14 },
                new Fandom { Id = 20, Name = "スズキ", ParentFandomId = 14 },
                new Fandom { Id = 21, Name = "ダイハツ", ParentFandomId = 14 },
                new Fandom { Id = 22, Name = "輸入車", ParentFandomId = 14 },
                new Fandom { Id = 23, Name = "メルセデス・ベンツ", ParentFandomId = 22 },
                new Fandom { Id = 24, Name = "BMW", ParentFandomId = 22 },
                new Fandom { Id = 25, Name = "フォルクスワーゲン", ParentFandomId = 22 },
                new Fandom { Id = 26, Name = "ポルシェ", ParentFandomId = 22 },
                new Fandom { Id = 27, Name = "IT", ParentFandomId = rootFandomId },
                new Fandom { Id = 28, Name = "社会", ParentFandomId = rootFandomId },
                new Fandom { Id = 29, Name = "なんでも", ParentFandomId = rootFandomId },
                new Fandom { Id = 30, Name = "運営", ParentFandomId = rootFandomId }
            };

            foreach (var fandom in initialFandoms) {
                // 親側を先に確定させてから子を処理する必要があるため、1件ごとに保存する
                if (fandom.ParentFandomId.HasValue) {
                    var parentExists = await context.Set<Fandom>().AnyAsync(f => f.Id == fandom.ParentFandomId.Value);
                    if (!parentExists) {
                        // 親がまだ存在しない場合はデータ不整合なのでスキップする
                        continue;
                    }
                }

                var existing = await context.Set<Fandom>().FirstOrDefaultAsync(f => f.Id == fandom.Id || (f.Name == fandom.Name && f.ParentFandomId == fandom.ParentFandomId));
                if (existing == null) {
                    await context.Set<Fandom>().AddAsync(fandom);
                } else {
                    existing.Name = fandom.Name;
                    existing.ParentFandomId = fandom.ParentFandomId;
                }

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 未登録のタグと対応するタグ概念を登録します。
        /// </summary>
        private static async Task InitializeTagsAsync(ApplicationDbContext context) {
            // 初期登録するTag一覧
            var initialTagTexts = new[] {
// イラスト・漫画・小説（創作）
"オリジナル", "創作", "オリキャラ", "二次創作", "ファンアート", "落書き", "水彩", "デジタル絵", "ネーム", "4コマ漫画",
"web漫画", "同人誌", "連載", "短編小説", "長編小説", "ファンタジー小説", "ミステリー", "ライトノベル", "プロット", "なろう系",
// アニメ・漫画（見る・読む）
"鬼滅の刃", "呪術廻戦", "推しの子", "フリーレン", "チェンソーマン", "ぼっち・ざ・ろっく", "SPY_FAMILY", "名探偵コナン", "ワンピース", "葬送のフリーレン",
"ちいかわ", "リコリコ", "ガンダム", "進撃の巨人", "ブルーロック", "今期アニメ", "神作画", "原作漫画", "単行本", "アニメ化",
// ゲーム
"スーパーマリオブラザーズ", "マリオカート", "ドラゴンクエスト", "ファイナルファンタジー", "ポケットモンスター", "ゼルダの伝説", "Minecraft", "Apex Legends", "原神", "モンスターハンター",
"大乱闘スマッシュブラザーズ", "スプラトゥーン", "どうぶつの森", "ウマ娘 プリティーダービー", "エルデンリング", "積みゲー", "ゲーム実況", "RPG", "FPS", "インディーゲーム",
// コスプレ
"コスプレ", "コスプレイヤー", "撮影会", "コミケ", "レイヤー", "衣装制作", "ウィッグ", "ロケ撮影", "コスプレイベント", "合わせ",
// 音楽
"ボカロ", "歌ってみた", "弾いてみた", "作業用BGM", "ライブ", "バンド", "作曲", "DTM", "アニソン", "J-POP",
// アイドル
"Snow Man", "SixTONES", "なにわ男子", "King & Prince", "乃木坂46", "櫻坂46", "日向坂46", "AKB48", "FRUITS ZIPPER", "＝LOVE",
"NiziU", "JO1", "BTS", "SEVENTEEN", "Stray Kids", "TWICE", "推し活", "ライブ遠征", "ファンクラブ", "グッズ",
// 自動車
"ドライブ", "愛車", "洗車", "カスタム", "ツーリング", "車検", "中古車", "新型車", "試乗", "サーキット",
"トヨタ", "プリウス", "カローラ", "クラウン", "アルファード", "ヤリス", "GR86", "スープラ",
"日産", "スカイライン", "GT-R", "ノート", "セレナ", "フェアレディZ",
"ホンダ", "N-BOX", "フィット", "シビック", "ヴェゼル", "NSX",
"マツダ", "CX-5", "ロードスター", "MAZDA3",
"スバル", "フォレスター", "レヴォーグ", "WRX",
"スズキ", "ジムニー", "スイフト", "スペーシア", "アルト",
"ダイハツ", "タント", "ムーヴ", "コペン",
"メルセデス・ベンツ", "Gクラス", "Cクラス", "BMW", "M3", "3シリーズ",
"フォルクスワーゲン", "ゴルフ", "ポロ", "ポルシェ", "911", "カイエン",
// IT
"プログラミング", "C#", "Python", "JavaScript", "生成AI", "ChatGPT", "個人開発", "Web開発", "Linux", "自作PC",
// 社会・なんでも・運営
"ニュース", "経済", "働き方", "雑談", "日常", "ひとりごと", "初投稿", "お知らせ", "不具合報告", "要望"
            };

            var distinctTagTexts = initialTagTexts
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var tagText in distinctTagTexts) {
                var normalized = tagText.ToLowerInvariant();
                var exists = await context.Set<Tag>().AnyAsync(t => t.NormalizedText == normalized);
                if (!exists) {
                    var concept = new TagConcept {
                        Category = "general",
                        CreatedAt = DateTimeOffset.UtcNow,
                        Tags = new List<Tag> {
                            new Tag {
                                TagText = tagText,
                                NormalizedText = normalized,
                                LanguageCode = "ja",
                                IsCanonical = true
                            }
                        }
                    };
                    await context.Set<TagConcept>().AddAsync(concept);
                }
            }

            await context.SaveChangesAsync();
        }
    }

}
