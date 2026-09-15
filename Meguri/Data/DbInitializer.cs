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
                new Fandom { Id = 1, Name = "イラスト" },
                new Fandom { Id = 2, Name = "漫画" },
                new Fandom { Id = 3, Name = "小説" },
                new Fandom { Id = 4, Name = "車" },
                new Fandom { Id = 5, Name = "プログラミング" },
                new Fandom { Id = 6, Name = "ゲーム" },
                new Fandom { Id = 7, Name = "コスプレ" }
            };

            foreach (var fandom in initialFandoms) {
                var exists = await context.Set<Fandom>().AnyAsync(f => f.Name == fandom.Name || f.Id == fandom.Id);
                if (!exists) {
                    await context.Set<Fandom>().AddAsync(fandom);
                }
            }

            await context.SaveChangesAsync();
        }
    }

}
