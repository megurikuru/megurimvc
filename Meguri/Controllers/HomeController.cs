using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        public HomeController(IStringLocalizer<SharedResource> sharedLocalizer, ApplicationDbContext context) {
            _sharedLocalizer = sharedLocalizer;
            _context = context;
        }

        public async Task<IActionResult> Index(int? fandomId, string? search, int? skip) {
            const int pageSize = 40;
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;

            var query = _context.Posts.AsQueryable();

            if (fandomId.HasValue) {
                query = query.Where(p => p.FandomId == fandomId.Value);
                ViewBag.CurrentFandom = await _context.Fandoms.FindAsync(fandomId.Value);
            }

            if (!string.IsNullOrEmpty(search)) {
                query = query.Where(p => p.Name.Contains(search) || p.Text.Contains(search));
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
