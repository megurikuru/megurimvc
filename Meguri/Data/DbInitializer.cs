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
        public static async Task InitializeAsync(ApplicationDbContext context, IConfiguration configuration) {
            if (!configuration.GetValue<bool>("DatabaseInitialization:Enabled")) {
                return;
            }

            await InitializeAsync(context);
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
                new Fandom { Id = 2, Name = "公開界隈", ParentFandomId = rootFandomId },
                new Fandom { Id = 3, Name = "つぶやき", ParentFandomId = rootFandomId },
                new Fandom { Id = 4, Name = "創作", ParentFandomId = rootFandomId },
                new Fandom { Id = 5, Name = "イラスト", ParentFandomId = 4 },
                new Fandom { Id = 6, Name = "漫画", ParentFandomId = 4 },
                new Fandom { Id = 7, Name = "小説", ParentFandomId = 4 },
                new Fandom { Id = 8, Name = "コスプレ", ParentFandomId = rootFandomId },
                new Fandom { Id = 9, Name = "車", ParentFandomId = rootFandomId },
                new Fandom { Id = 10, Name = "国産車", ParentFandomId = 9 },
                new Fandom { Id = 11, Name = "トヨタ", ParentFandomId = 10 },
                new Fandom { Id = 12, Name = "ホンダ", ParentFandomId = 10 },
                new Fandom { Id = 13, Name = "スバル", ParentFandomId = 10 },
                new Fandom { Id = 14, Name = "日産", ParentFandomId = 10 },
                new Fandom { Id = 15, Name = "スズキ", ParentFandomId = 10 },
                new Fandom { Id = 16, Name = "ダイハツ", ParentFandomId = 10 },
                new Fandom { Id = 17, Name = "輸入車", ParentFandomId = 9 },
                new Fandom { Id = 18, Name = "漫画・アニメ", ParentFandomId = rootFandomId },
                new Fandom { Id = 19, Name = "漫画", ParentFandomId = 18 },
                new Fandom { Id = 20, Name = "アニメ", ParentFandomId = 18 },
                new Fandom { Id = 21, Name = "ゲーム", ParentFandomId = rootFandomId },
                new Fandom { Id = 22, Name = "プログラミング", ParentFandomId = rootFandomId },
                new Fandom { Id = 23, Name = "運営", ParentFandomId = rootFandomId }
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
                "鬼滅の刃", "呪術廻戦", "水星の魔女", "推しの子", "フリーレン", "チェンソーマン", "ぼっち・ざ・ろっく", "SPY_FAMILY", "名探偵コナン", "shingeki",
                "heroaca_a", "hq_anime", "ブルーロック", "gundam", "sao_anime", "アニメウマ娘", "ちいかわ", "リコリコ", "アニポケ", "エヴァ",
                "トヨタ", "ホンダ", "日産", "スズキ", "マツダ", "スバル", "三菱", "ダイハツ", "フォルクスワーゲン", "テスラ",
                "プリウス", "ヤリス", "カローラ", "クラウン", "アルファード", "N-BOX", "フィット", "ヴェゼル", "シビック", "ノート",
                "セレナ", "スカイライン", "タント", "スペーシア", "ジムニー", "CX-5", "ロードスター", "フォレスター", "レヴォーグ", "デリカD:5",
                "Snow Man", "SixTONES", "なにわ男子", "King & Prince", "乃木坂46", "櫻坂46", "日向坂46", "AKB48", "モーニング娘。'26", "FRUITS ZIPPER",
                "超ときめき♡宣伝部", "＝LOVE", "NiziU", "ME:I", "JO1", "INI", "BTS", "SEVENTEEN", "Stray Kids", "TWICE",
                "モンキー・D・ルフィ", "孫悟空", "江戸川コナン", "ピカチュウ", "ドラえもん", "野原しんのすけ", "竈門炭治郎", "五条悟", "リヴァイ・アッカーマン", "坂田銀時",
                "ロイド・フォージャー", "アーニャ・フォージャー", "フリーレン", "後藤ひとり", "エレン・イェーガー", "うずまきナルト", "黒崎一護", "空条承太郎", "アムロ・レイ", "シャア・アズナブル",
                "碇シンジ", "綾波レイ", "ルパン三世", "冴羽獠", "ケンシロウ", "矢吹丈", "古代進", "孫悟飯", "トランクス", "ベジータ",
                "キリト", "アスナ", "リムル＝テンペスト", "佐藤和真", "上条当麻", "木之本桜", "鹿目まどか", "平沢唯", "涼宮ハルヒ", "月野うさぎ",
                "スーパーマリオブラザーズ", "マリオカート", "ドラゴンクエスト", "ファイナルファンタジー", "ポケットモンスター", "ゼルダの伝説", "Minecraft", "グランド・セフト・オート", "Apex Legends", "フォートナイト",
                "原神", "モンスターハンター", "大乱闘スマッシュブラザーズ", "スプラトゥーン", "どうぶつの森", "パズドラ", "モンスターストライク", "Fate/Grand Order", "ウマ娘 プリティーダービー", "バイオハザード",
                "メタルギアソリッド", "ストリートファイター", "鉄拳", "キングダム ハーツ", "ペルソナ", "テイルズ オブ シリーズ", "ダークソウル", "エルデンリング", "Call of Duty", "オーバーウォッチ",
                "League of Legends", "VALORANT", "サイバーパンク2077", "ウィッチャー3", "The Last of Us", "クロノ・トリガー", "マインクラフト", "テトリス", "パックマン", "スペースインベーダー"
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
