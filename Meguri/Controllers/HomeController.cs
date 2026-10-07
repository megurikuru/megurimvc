using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Data.SqlClient;
using Meguri.Data;
using Meguri.Models;

namespace Meguri.Controllers {
    // ホーム画面および共通的な機能（言語設定、アバウト、問い合わせ等）を提供するコントローラー
    public class HomeController : Controller {
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // HomeController のコンストラクター（依存性の注入）
        public HomeController(IStringLocalizer<SharedResource> sharedLocalizer, ApplicationDbContext context, UserManager<ApplicationUser> userManager) {
            _sharedLocalizer = sharedLocalizer;
            _context = context;
            _userManager = userManager;
        }

        // ログイン中のユーザーが18歳以上かどうか判定（ImageControllerと同じロジック）
        private async Task<bool> IsUserAdultAsync() {
            // 未ログインの場合は成人ではないと判定
            if (User.Identity?.IsAuthenticated != true) {
                return false;
            }

            // ログイン中ユーザー情報を取得
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return false;
            }

            // 生年月日に18年を加算した日付が今日以前か確認して年齢を判定
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return user.DateOfBirth.AddYears(18) <= today;
        }

        // トップページ（インデックス画面）の表示処理
        public async Task<IActionResult> 
            Index(string? search, int? skip) {
            const int pageSize = 40;

            // パラメータの正規化
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            ViewBag.Search = search;

            // ユーザーの認証状態と年齢制限のチェック
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isAdult = isAuthenticated && await IsUserAdultAsync();

            // 1. ベースとなる SQL（テーブル名・カラム名をダブルクォーテーションで囲む）
            var sql = @"
                SELECT DISTINCT p.*
                FROM ""Posts"" p
                LEFT JOIN ""PostTags"" pt ON p.""Id"" = pt.""PostId""
                LEFT JOIN ""TagConcepts"" tc ON pt.""TagConceptId"" = tc.""Id""
                LEFT JOIN ""Tags"" t ON tc.""Id"" = t.""TagConceptId""
                LEFT JOIN ""PostImages"" pi ON p.""Id"" = pi.""PostId""
                LEFT JOIN ""Images"" i ON pi.""ImageId"" = i.""Id""
                LEFT JOIN ""ImageTags"" it ON i.""Id"" = it.""ImageId""
                LEFT JOIN ""TagConcepts"" itc ON it.""TagConceptId"" = itc.""Id""
                LEFT JOIN ""Tags"" itg ON itc.""Id"" = itg.""TagConceptId""";

            var parameters = new List<object>();

            // 検索キーワードがある場合のみ WHERE 句を追加
            if (!string.IsNullOrWhiteSpace(search)) {
                sql += @"
                WHERE (
                    p.""Name"" LIKE @search OR
                    p.""Text"" LIKE @search OR
                    t.""TagText"" LIKE @search OR
                    i.""Name"" LIKE @search OR
                    i.""Caption"" LIKE @search OR
                    i.""Description"" LIKE @search OR
                    itg.""TagText"" LIKE @search
                )";

                parameters.Add(new Npgsql.NpgsqlParameter("@search", $"%{search}%"));
            }
            // SQL から Posts のクエリを作成
            var postQuery = _context.Posts.FromSqlRaw(sql, parameters.ToArray());

            // 2. 総件数取得とページネーションの調整
            var totalCount = await postQuery.CountAsync();
            if (totalCount > 0 && resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, ((totalCount - 1) / pageSize) * pageSize);
            }

            // 3. 関連データのインクルードとデータ取得（添付画像へのアクセス権限・年齢制限フィルタ適用）
            var posts = await postQuery
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.TagConcept)
                        .ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .Select(p => new Post {
                    Id = p.Id,
                    Name = p.Name,
                    Text = p.Text,
                    CreatedAt = p.CreatedAt,
                    User = p.User,
                    Fandom = p.Fandom,
                    Reactions = p.Reactions,
                    PostTags = p.PostTags,
                    PostImages = p.PostImages.Where(pi =>
                        (!isAuthenticated ? (pi.Image.IsPublic && !pi.Image.IsSexual && !pi.Image.IsViolence) :
                         !isAdult ? (!pi.Image.IsSexual && !pi.Image.IsViolence) : true)
                    ).ToList()
                })
                .ToListAsync();

            // View へ渡すデータの設定
            ViewBag.Posts = posts;
            ViewBag.TopFandoms = await _context.Fandoms
                .Include(f => f.ChildFandoms)
                .Where(f => f.ParentFandomId == null)
                .ToListAsync();
            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();

            // ページネーション情報
            ViewBag.PostsPageSize = pageSize;
            ViewBag.PostsTotalCount = totalCount;
            ViewBag.PostsSkip = resolvedSkip;
            ViewBag.PostsHasPrevious = resolvedSkip > 0;
            ViewBag.PostsHasNext = resolvedSkip + posts.Count < totalCount;
            ViewBag.PostsPreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.PostsNextSkip = resolvedSkip + pageSize;

            return View();
        }

        // 言語・カルチャ切り替え処理（クッキーに選択された言語情報を保存）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl) {
            // クッキーに言語設定を保存（有効期限: 1年）
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
            );

            // ローカルURLであれば元のページへリダイレクト、それ以外はトップページへ
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // アバウト（サイト概要）画面の表示
        public IActionResult About() {
            ViewData["Message"] = _sharedLocalizer["Controller_About_Message"];
            return View();
        }

        // お問い合わせ画面の表示
        public IActionResult Contact() {
            ViewData["Message"] = _sharedLocalizer["Controller_Contact_Message"];
            return View();
        }

        // エラー画面の表示
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}