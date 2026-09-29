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
        // 投稿や画像の取得・フィルタリング・ページネーション設定を行います。
        public async Task<IActionResult> Index(int? fandomId, string? search, int? skip, int? imageSkip) {
            const int pageSize = 40; // 1ページあたりの表示件数

            // 負の値などを防止してスキップ数を正規化
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            var resolvedImageSkip = imageSkip.HasValue && imageSkip.Value > 0 ? imageSkip.Value : 0;

            // 投稿データのベースクエリ作成
            var query = _context.Posts.AsQueryable();

            // ファンダムIDが指定されている場合、投稿を絞り込む
            if (fandomId.HasValue) {
                query = query.Where(p => p.FandomId == fandomId.Value);
                ViewBag.CurrentFandom = await _context.Fandoms.FindAsync(fandomId.Value);
            }

            // 画像データのベースクエリ作成
            var imageQuery = _context.Images.AsQueryable();

            // ユーザーの認証状態と年齢制限のチェック
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isAdult = isAuthenticated && await IsUserAdultAsync();

            // 年齢制限・ログイン状態に応じた画像のフィルタリング
            if (!isAuthenticated) {
                // 未ログインユーザー: 公開画像かつ一般（非NSFW）のみ
                imageQuery = imageQuery.Where(i => i.IsPublic && !i.IsSexual && !i.IsViolence);
            } else if (!isAdult) {
                // 18歳未満または生年月日未登録の会員: 一般画像のみ（公開＋非公開問わず）
                imageQuery = imageQuery.Where(i => !i.IsSexual && !i.IsViolence);
            }
            // 18歳以上の会員: 全て閲覧可能

            // 検索キーワードが指定されている場合、投稿および画像を検索
            if (!string.IsNullOrEmpty(search)) {
                // 投稿の件名・本文・タグテキストから曖昧検索
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Text.Contains(search) ||
                    p.PostTags.Any(pt => pt.TagConcept.Tags.Any(t => t.TagText.Contains(search))));

                // 画像の名称・キャプション・説明・タグテキストから曖昧検索
                imageQuery = imageQuery.Where(i =>
                    i.Name.Contains(search) ||
                    i.Caption.Contains(search) ||
                    i.Description.Contains(search) ||
                    i.ImageTags.Any(it => it.TagConcept.Tags.Any(t => t.TagText.Contains(search))));
                
                ViewBag.CurrentSearch = search;
            }

            // --- 投稿（Posts）の取得とページネーション計算 ---
            var totalCount = await query.CountAsync();
            // スキップ数が全件数を超えている場合は最後のページに調整
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            // 関連データ（ユーザー、ファンダム、画像、タグ、リアクション）をインクルードして最新順に取得
            var recentPosts = await query
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            // --- 画像（Images）の取得とページネーション計算 ---
            var imageTotalCount = await imageQuery.CountAsync();
            if (resolvedImageSkip >= imageTotalCount) {
                resolvedImageSkip = Math.Max(0, imageTotalCount - pageSize);
            }

            // 検索キーワードがある場合のみ画像を検索して取得（無しの場合は空リスト）
            var recentImages = !string.IsNullOrEmpty(search)
                ? await imageQuery
                    .Include(i => i.User)
                    .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                    .OrderByDescending(i => i.CreatedAt)
                    .Skip(resolvedImageSkip)
                    .Take(pageSize)
                    .ToListAsync()
                : new List<Image>();

            // 親ファンダム一覧（カテゴリー階層のトップレベル）を取得
            var fandoms = await _context.Fandoms
                .Include(f => f.ChildFandoms)
                .Where(f => f.ParentFandomId == null)
                .ToListAsync();

            // --- View 側へ渡すデータのセット ---
            ViewBag.RecentPosts = recentPosts;
            ViewBag.TopFandoms = fandoms;
            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            
            // 投稿用ページネーション情報
            ViewBag.PostsPageSize = pageSize;
            ViewBag.PostsTotalCount = totalCount;
            ViewBag.PostsSkip = resolvedSkip;
            ViewBag.PostsHasPrevious = resolvedSkip > 0;
            ViewBag.PostsHasNext = resolvedSkip + recentPosts.Count < totalCount;
            ViewBag.PostsPreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.PostsNextSkip = resolvedSkip + pageSize;

            // 画像用ページネーション情報
            ViewBag.RecentImages = recentImages;
            ViewBag.ImagesPageSize = pageSize;
            ViewBag.ImagesTotalCount = imageTotalCount;
            ViewBag.ImagesSkip = resolvedImageSkip;
            ViewBag.ImagesHasPrevious = resolvedImageSkip > 0;
            ViewBag.ImagesHasNext = resolvedImageSkip + recentImages.Count < imageTotalCount;
            ViewBag.ImagesPreviousSkip = Math.Max(0, resolvedImageSkip - pageSize);
            ViewBag.ImagesNextSkip = resolvedImageSkip + pageSize;

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