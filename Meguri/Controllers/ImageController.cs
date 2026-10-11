using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Services;

namespace Meguri.Controllers {
    /// <summary>
    /// 画像は単独で投稿・閲覧できない。投稿/メッセージ/コメントへの添付としてのみファイル配信する。
    /// 公開設定・R-18・R-18G は親の投稿に従う。
    /// </summary>
    public class ImageController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly IR2StorageService _r2StorageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ImageController(
            ApplicationDbContext context,
            IR2StorageService r2StorageService,
            UserManager<ApplicationUser> userManager) {
            _context = context;
            _r2StorageService = r2StorageService;
            _userManager = userManager;
        }

        private async Task<bool> IsUserAdultAsync() {
            if (User.Identity?.IsAuthenticated != true) return false;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return user.DateOfBirth.AddYears(18) <= today;
        }

        private async Task<(bool CanView, bool Cacheable)> CheckAccessAsync(Image image) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAuthenticated = !string.IsNullOrEmpty(userId);
            if (isAuthenticated && (image.UserId == userId || User.IsInRole("Admin"))) {
                return (true, false);
            }

            var isAdult = await IsUserAdultAsync();

            bool PostVisible(Post p) =>
                (p.IsPublic || isAuthenticated) && ((!p.IsSexual && !p.IsViolence) || isAdult);

            var posts = await _context.PostImages
                .Where(pi => pi.ImageId == image.Id)
                .Select(pi => pi.Post)
                .ToListAsync();
            if (posts.Any(PostVisible)) {
                return (true, posts.Any(p => p.IsPublic && !p.IsSexual && !p.IsViolence));
            }

            var commentPosts = await _context.CommentImages
                .Where(ci => ci.ImageId == image.Id && ci.Comment.Post != null)
                .Select(ci => ci.Comment.Post!)
                .ToListAsync();
            if (commentPosts.Any(PostVisible)) {
                return (true, false);
            }

            if (isAuthenticated) {
                var inMessage = await _context.MessageImages
                    .AnyAsync(mi => mi.ImageId == image.Id &&
                        mi.Message.Conversation.Members.Any(m => m.UserId == userId));
                if (inMessage) return (true, false);
            }

            var profileVisible = await _context.Users.AnyAsync(u =>
                (u.ActiveAvatarImageId == image.Id || u.HeaderImageId == image.Id) &&
                (u.IsProfilePublic || isAuthenticated));
            if (profileVisible) return (true, false);

            return (false, false);
        }

        // GET: /Image/File/5
        public async Task<IActionResult> File(long id) {
            var image = await _context.Images.FindAsync(id);
            if (image == null || string.IsNullOrEmpty(image.StorageKey)) {
                return NotFound();
            }

            var (canView, cacheable) = await CheckAccessAsync(image);
            if (!canView) {
                if (User.Identity?.IsAuthenticated != true) {
                    return Unauthorized();
                }
                return Forbid();
            }

            var fileResult = await _r2StorageService.GetFileAsync(image.StorageKey);
            if (fileResult == null) {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = cacheable ? "public, max-age=86400" : "private, no-cache";

            return File(fileResult.Value.Stream, fileResult.Value.ContentType);
        }
    }
}
