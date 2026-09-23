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
    public class HomeController : Controller {
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IStringLocalizer<SharedResource> sharedLocalizer, ApplicationDbContext context, UserManager<ApplicationUser> userManager) {
            _sharedLocalizer = sharedLocalizer;
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// ログイン中のユーザーが18歳以上かどうか判定（ImageControllerと同じロジック）
        /// </summary>
        private async Task<bool> IsUserAdultAsync() {
            if (User.Identity?.IsAuthenticated != true) {
                return false;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return false;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return user.DateOfBirth.AddYears(18) <= today;
        }

        public async Task<IActionResult> Index(int? fandomId, string? search, int? skip, int? imageSkip) {
            const int pageSize = 40;
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            var resolvedImageSkip = imageSkip.HasValue && imageSkip.Value > 0 ? imageSkip.Value : 0;

            var query = _context.Posts.AsQueryable();

            if (fandomId.HasValue) {
                query = query.Where(p => p.FandomId == fandomId.Value);
                ViewBag.CurrentFandom = await _context.Fandoms.FindAsync(fandomId.Value);
            }

            var imageQuery = _context.Images.AsQueryable();

            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isAdult = isAuthenticated && await IsUserAdultAsync();

            if (!isAuthenticated) {
                // 未ログインユーザー: 公開画像かつ一般（非NSFW）のみ
                imageQuery = imageQuery.Where(i => i.IsPublic && !i.IsSexual && !i.IsViolence);
            } else if (!isAdult) {
                // 18歳未満または生年月日未登録の会員: 一般画像のみ（公開＋非公開問わず）
                imageQuery = imageQuery.Where(i => !i.IsSexual && !i.IsViolence);
            }
            // 18歳以上の会員: 全て閲覧可能

            if (!string.IsNullOrEmpty(search)) {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Text.Contains(search) ||
                    p.PostTags.Any(pt => pt.TagConcept.Tags.Any(t => t.TagText.Contains(search))));
                imageQuery = imageQuery.Where(i =>
                    i.Name.Contains(search) ||
                    i.Caption.Contains(search) ||
                    i.Description.Contains(search) ||
                    i.ImageTags.Any(it => it.TagConcept.Tags.Any(t => t.TagText.Contains(search))));
                ViewBag.CurrentSearch = search;
            }

            var totalCount = await query.CountAsync();
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var recentPosts = await query
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions)
                .OrderByDescending(p => p.Created)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            var imageTotalCount = await imageQuery.CountAsync();
            if (resolvedImageSkip >= imageTotalCount) {
                resolvedImageSkip = Math.Max(0, imageTotalCount - pageSize);
            }

            var recentImages = !string.IsNullOrEmpty(search)
                ? await imageQuery
                    .Include(i => i.User)
                    .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                    .OrderByDescending(i => i.Created)
                    .Skip(resolvedImageSkip)
                    .Take(pageSize)
                    .ToListAsync()
                : new List<Image>();

            var fandoms = await _context.Fandoms
                .Include(f => f.ChildFandoms)
                .Where(f => f.ParentFandomId == null)
                .ToListAsync();

            ViewBag.RecentPosts = recentPosts;
            ViewBag.TopFandoms = fandoms;
            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            ViewBag.PostsPageSize = pageSize;
            ViewBag.PostsTotalCount = totalCount;
            ViewBag.PostsSkip = resolvedSkip;
            ViewBag.PostsHasPrevious = resolvedSkip > 0;
            ViewBag.PostsHasNext = resolvedSkip + recentPosts.Count < totalCount;
            ViewBag.PostsPreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.PostsNextSkip = resolvedSkip + pageSize;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl) {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult About() {
            ViewData["Message"] = _sharedLocalizer["Controller_About_Message"];
            return View();
        }

        public IActionResult Contact() {
            ViewData["Message"] = _sharedLocalizer["Controller_Contact_Message"];
            return View();
        }

        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
