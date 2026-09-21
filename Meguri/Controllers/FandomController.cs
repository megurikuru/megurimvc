using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;

namespace Meguri.Controllers {
    public class FandomController : Controller {
        private readonly ApplicationDbContext _context;

        public FandomController(ApplicationDbContext context) {
            _context = context;
        }

        // GET: /Fandom
        public async Task<IActionResult> Index(int? skip) {
            const int pageSize = 40;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalCount = await _context.Fandoms.CountAsync();
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var fandoms = await _context.Fandoms
                .Include(f => f.ParentFandom)
                .Include(f => f.ChildFandoms)
                .Include(f => f.FandomUsers)
                .Include(f => f.Posts)
                .OrderBy(f => f.Id)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            var joinedFandomIds = new HashSet<int>();
            if (userId != null) {
                joinedFandomIds = (await _context.FandomUsers
                    .Where(fu => fu.UserId == userId)
                    .Select(fu => fu.FandomId)
                    .ToListAsync()).ToHashSet();
            }

            ViewBag.JoinedFandomIds = joinedFandomIds;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Skip = resolvedSkip;
            ViewBag.HasPrevious = resolvedSkip > 0;
            ViewBag.HasNext = resolvedSkip + fandoms.Count < totalCount;
            ViewBag.PreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.NextSkip = resolvedSkip + pageSize;
            return View(fandoms);
        }

        // GET: /Fandom/Details/5
        public async Task<IActionResult> Details(int? id, int? skip) {
            if (id == null) return NotFound();

            const int pageSize = 40;

            var fandom = await _context.Fandoms
                .Include(f => f.ParentFandom)
                .Include(f => f.ChildFandoms).ThenInclude(c => c.Posts)
                .Include(f => f.FandomUsers).ThenInclude(fu => fu.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fandom == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isJoined = userId != null && await _context.FandomUsers.AnyAsync(fu => fu.FandomId == fandom.Id && fu.UserId == userId);

            var postsQuery = _context.Posts
                .Where(p => p.FandomId == fandom.Id)
                .Include(p => p.User)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions);

            var totalCount = await postsQuery.CountAsync();
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var posts = await postsQuery
                .OrderByDescending(p => p.Created)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.IsJoined = isJoined;
            ViewBag.FandomPosts = posts;
            ViewBag.FandomPostsPageSize = pageSize;
            ViewBag.FandomPostsTotalCount = totalCount;
            ViewBag.FandomPostsSkip = resolvedSkip;
            ViewBag.FandomPostsHasPrevious = resolvedSkip > 0;
            ViewBag.FandomPostsHasNext = resolvedSkip + posts.Count < totalCount;
            ViewBag.FandomPostsPreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.FandomPostsNextSkip = resolvedSkip + pageSize;
            return View(fandom);
        }

        // POST: /Fandom/Join/5
        // 親のFandomに参加したら、子のFandomにも自動的に参加する
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id, string? returnUrl) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var allFandoms = await _context.Fandoms.ToListAsync();
            var targetFandom = allFandoms.FirstOrDefault(f => f.Id == id);
            if (targetFandom == null) return NotFound();

            // 対象界隈とその全子孫界隈のIDを収集
            var fandomIdsToJoin = new HashSet<int> { id };
            CollectDescendantFandomIds(id, allFandoms, fandomIdsToJoin);

            var existingJoinedIds = (await _context.FandomUsers
                .Where(fu => fu.UserId == userId && fandomIdsToJoin.Contains(fu.FandomId))
                .Select(fu => fu.FandomId)
                .ToListAsync()).ToHashSet();

            foreach (var fId in fandomIdsToJoin) {
                if (!existingJoinedIds.Contains(fId)) {
                    _context.FandomUsers.Add(new FandomUser {
                        FandomId = fId,
                        UserId = userId
                    });
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Fandom/Leave/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id, string? returnUrl) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var membership = await _context.FandomUsers
                .FirstOrDefaultAsync(fu => fu.FandomId == id && fu.UserId == userId);

            if (membership != null) {
                _context.FandomUsers.Remove(membership);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Fandom/Create
        [Authorize]
        public async Task<IActionResult> Create(int? parentId) {
            ViewBag.ParentFandoms = await _context.Fandoms.ToListAsync();
            return View(new Fandom { ParentFandomId = parentId });
        }

        // POST: /Fandom/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ParentFandomId")] Fandom fandom) {
            if (string.IsNullOrWhiteSpace(fandom.Name)) {
                ModelState.AddModelError("Name", "界隈名を入力してください。");
            }

            if (ModelState.IsValid) {
                _context.Fandoms.Add(fandom);
                await _context.SaveChangesAsync();

                // 作成者を自動参加
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null) {
                    _context.FandomUsers.Add(new FandomUser { FandomId = fandom.Id, UserId = userId });
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Details), new { id = fandom.Id });
            }

            ViewBag.ParentFandoms = await _context.Fandoms.ToListAsync();
            return View(fandom);
        }

        // 再帰的に全子孫界隈IDを取得するヘルパー
        private void CollectDescendantFandomIds(int parentId, List<Fandom> allFandoms, HashSet<int> result) {
            var children = allFandoms.Where(f => f.ParentFandomId == parentId).ToList();
            foreach (var child in children) {
                if (result.Add(child.Id)) {
                    CollectDescendantFandomIds(child.Id, allFandoms, result);
                }
            }
        }
    }
}
