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
                new Fandom { Id = 1, Name = "インターネット公開", ParentFandomId = null },
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
                var existing = await context.Set<Fandom>().FirstOrDefaultAsync(f => f.Id == fandom.Id || f.Name == fandom.Name);
                if (existing == null) {
                    await context.Set<Fandom>().AddAsync(fandom);
                } else {
                    existing.Name = fandom.Name;
                    existing.ParentFandomId = fandom.ParentFandomId;
                }
            }

            await context.SaveChangesAsync();
        }
    }

}
