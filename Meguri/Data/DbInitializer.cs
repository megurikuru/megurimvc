using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meguri.Models;

namespace Meguri.Data {

    /// <summary>
    /// データベースの初期データ登録（シード）処理を提供するクラス
    /// </summary>
    public static class DbInitializer {

        /// <summary>
        /// Fandomモデルなどのマスター初期データを登録します。
        /// </summary>
        public static async Task InitializeAsync(ApplicationDbContext context) {
            // 初期登録するFandom一覧
            var initialFandoms = new[] {
                new Fandom { Id = 1, Name = "公開界隈", ParentFandomId = null },
                new Fandom { Id = 2, Name = "つぶやき", ParentFandomId = null },
                new Fandom { Id = 3, Name = "創作", ParentFandomId = null },
                new Fandom { Id = 4, Name = "イラスト", ParentFandomId = 3 },
                new Fandom { Id = 5, Name = "漫画", ParentFandomId = 3 },
                new Fandom { Id = 6, Name = "小説", ParentFandomId = 3 },
                new Fandom { Id = 7, Name = "コスプレ", ParentFandomId = null },
                new Fandom { Id = 8, Name = "車", ParentFandomId = null },
                new Fandom { Id = 9, Name = "国産車", ParentFandomId = 8 },
                new Fandom { Id = 10, Name = "輸入車", ParentFandomId = 8 },
                new Fandom { Id = 11, Name = "漫画・アニメ", ParentFandomId = null },
                new Fandom { Id = 12, Name = "漫画", ParentFandomId = 11 },
                new Fandom { Id = 13, Name = "アニメ", ParentFandomId = 11 },
                new Fandom { Id = 14, Name = "ゲーム", ParentFandomId = null },
                new Fandom { Id = 15, Name = "プログラミング", ParentFandomId = null },
                new Fandom { Id = 16, Name = "運営", ParentFandomId = null }
            };

            foreach (var fandom in initialFandoms) {
                var existing = await context.Set<Fandom>().FirstOrDefaultAsync(f => f.Id == fandom.Id || (f.Name == fandom.Name && f.ParentFandomId == fandom.ParentFandomId));
                if (existing == null) {
                    await context.Set<Fandom>().AddAsync(fandom);
                } else {
                    existing.Name = fandom.Name;
                    existing.ParentFandomId = fandom.ParentFandomId;
                }
            }

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
