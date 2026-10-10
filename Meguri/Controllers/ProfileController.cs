using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;

namespace Meguri.Controllers {
    public class ProfileController : Controller {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context) {
            _context = context;
        }

        // GET: /Profile/Details/{userId}
        public async Task<IActionResult> Details(string? id, int? skip) {
            if (string.IsNullOrEmpty(id)) return NotFound();
            const int pageSize = 10;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsWithdrawn);
            if (user == null) return NotFound();

            var isOwner = User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id;
            if (!user.IsProfilePublic && User.Identity?.IsAuthenticated != true) return NotFound();

            var query = _context.Posts.Where(p => p.UserId == user.Id);
            if (!isOwner) {
                query = query.Where(p => p.IsPublic);
            }

            var totalCount = await query.CountAsync();
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            if (totalCount > 0 && resolvedSkip >= totalCount) {
                resolvedSkip = ((totalCount - 1) / pageSize) * pageSize;
            }

            var posts = await query
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Profile = user;
            ViewBag.IsOwner = isOwner;
            ViewBag.ExternalUrls = user.ExternalUrls
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(5)
                .ToList();
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Skip = resolvedSkip;
            ViewBag.HasPrevious = resolvedSkip > 0;
            ViewBag.HasNext = resolvedSkip + posts.Count < totalCount;
            ViewBag.PreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.NextSkip = resolvedSkip + pageSize;
            return View(posts);
        }
    }
}
