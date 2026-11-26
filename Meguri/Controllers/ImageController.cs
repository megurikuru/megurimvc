using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IImageProcessingService _imageProcessingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ImageController(
            ApplicationDbContext context,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer) {
            _context = context;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
            _userManager = userManager;
            _localizer = localizer;
        }

        private async Task<(bool CanView, bool Cacheable)> CheckAccessAsync(Image image) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAuthenticated = !string.IsNullOrEmpty(userId);
            if (isAuthenticated && (image.UserId == userId || User.IsInRole("Admin"))) {
                return (true, false);
            }

            var currentUser = isAuthenticated ? await _userManager.GetUserAsync(User) : null;
            var isAdult = currentUser?.IsAdult() == true;
            var showSexual = isAdult && currentUser!.ShowSexual;
            var showViolence = isAdult && currentUser!.ShowViolence;

            bool PostVisible(Post p) =>
                (p.IsPublic || isAuthenticated) &&
                (!p.IsSexual || showSexual) &&
                (!p.IsViolence || showViolence);

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

            var isProfileImage = await _context.Users.AnyAsync(u =>
                u.ActiveAvatarImageId == image.Id || u.HeaderImageId == image.Id);
            if (isProfileImage) return (true, true);

            // 画像所有者が自身の投稿・コメント・メッセージ・プロフィール自己紹介文（Markdown）に
            // /Image/File/{id} として挿入している場合、それぞれの公開・閲覧権限に従って表示を許可する
            if (!string.IsNullOrEmpty(image.UserId)) {
                var imagePath = $"/Image/File/{image.Id}";

                var candidatePosts = await _context.Posts
                    .AsNoTracking()
                    .Where(p => p.UseMarkdown && p.UserId == image.UserId && p.Body.Contains(imagePath))
                    .Select(p => new { p.Body, p.IsPublic, p.IsSexual, p.IsViolence })
                    .ToListAsync();

                var referencedPosts = candidatePosts
                    .Where(p => ContainsExactImageReference(p.Body, image.Id))
                    .ToList();

                if (referencedPosts.Any(p =>
                    (p.IsPublic || isAuthenticated) &&
                    (!p.IsSexual || showSexual) &&
                    (!p.IsViolence || showViolence))) {
                    var isPublicCacheable = referencedPosts.Any(p => p.IsPublic && !p.IsSexual && !p.IsViolence);
                    return (true, isPublicCacheable);
                }

                // 自身のMarkdownコメント（親投稿が閲覧可能な場合）
                var candidateComments = await _context.Comments
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && c.UseMarkdown && c.UserId == image.UserId && c.Post != null && c.Text.Contains(imagePath))
                    .Select(c => new {
                        c.Text,
                        c.Post!.IsPublic,
                        c.Post.IsSexual,
                        c.Post.IsViolence
                    })
                    .ToListAsync();

                if (candidateComments.Any(c =>
                    ContainsExactImageReference(c.Text, image.Id) &&
                    (c.IsPublic || isAuthenticated) &&
                    (!c.IsSexual || showSexual) &&
                    (!c.IsViolence || showViolence))) {
                    return (true, false);
                }

                // 自身のMarkdownメッセージ（閲覧者が会話メンバーの場合）
                if (isAuthenticated) {
                    var candidateMessages = await _context.Messages
                        .AsNoTracking()
                        .Where(m => !m.IsDeleted && m.UseMarkdown && m.SenderId == image.UserId &&
                            m.Body.Contains(imagePath) &&
                            m.Conversation.Members.Any(cm => cm.UserId == userId))
                        .Select(m => m.Body)
                        .ToListAsync();

                    if (candidateMessages.Any(body => ContainsExactImageReference(body, image.Id))) {
                        return (true, false);
                    }
                }

                // 自身のMarkdownプロフィール自己紹介文（公開プロフィールまたはログイン済みの場合）
                var ownerBio = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == image.UserId && !u.IsWithdrawn && u.UseMarkdownBio && u.Bio.Contains(imagePath))
                    .Select(u => new { u.Bio, u.IsProfilePublic })
                    .FirstOrDefaultAsync();

                if (ownerBio != null &&
                    ContainsExactImageReference(ownerBio.Bio, image.Id) &&
                    (ownerBio.IsProfilePublic || isAuthenticated)) {
                    return (true, ownerBio.IsProfilePublic);
                }
            }

            return (false, false);
        }

        private static bool ContainsExactImageReference(string? body, long imageId) {
            if (string.IsNullOrEmpty(body)) return false;
            return Regex.IsMatch(body, $@"/Image/File/{imageId}(?![0-9])", RegexOptions.IgnoreCase);
        }

        // GET: /Image/MyImages?skip=0&take=24&search=
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyImages(int skip = 0, int take = 24, string? search = null) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) {
                return Unauthorized();
            }

            var resolvedSkip = Math.Max(0, skip);
            var resolvedTake = Math.Clamp(take, 1, 60);

            var query = _context.Images
                .AsNoTracking()
                .Where(i => i.UserId == userId && !string.IsNullOrEmpty(i.StorageKey));

            if (!string.IsNullOrWhiteSpace(search)) {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.Alt.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id)
                .Skip(resolvedSkip)
                .Take(resolvedTake)
                .Select(i => new {
                    id = i.Id,
                    name = i.Name,
                    alt = string.IsNullOrWhiteSpace(i.Alt) ? i.Name : i.Alt,
                    url = $"/Image/File/{i.Id}",
                    width = i.Width,
                    height = i.Height,
                    createdAt = i.CreatedAt
                })
                .ToListAsync();

            return Json(new {
                items,
                totalCount,
                hasMore = resolvedSkip + items.Count < totalCount
            });
        }

        // POST: /Image/Upload
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) {
                return Unauthorized();
            }

            if (file == null || file.Length == 0) {
                return BadRequest(new { error = _localizer["Markdown_ImagePicker_UploadNoFile"].Value });
            }

            if (file.Length > ImageProcessingService.MaxInputSizeBytes) {
                var sizeError = string.Format(
                    _localizer["Image_Error_MaxSizeExceeded"].Value,
                    file.Length / 1024.0 / 1024.0);
                return BadRequest(new { error = sizeError });
            }

            ProcessedImageResult processed;
            try {
                processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);
            } catch (Exception ex) {
                return BadRequest(new { error = ex.Message });
            }

            var storageKey = $"{userId}/{Guid.NewGuid():N}.webp";
            try {
                using var uploadStream = new MemoryStream(processed.Data);
                await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(baseName)) {
                    baseName = "image";
                }

                var image = new Image {
                    UserId = userId,
                    Name = baseName + ".webp",
                    StorageKey = storageKey,
                    ContentType = processed.ContentType,
                    FileSize = processed.FileSize,
                    Width = processed.Width,
                    Height = processed.Height,
                    Alt = baseName
                };
                image.UserImages.Add(new UserImage {
                    UserId = userId,
                    Image = image
                });
                _context.Images.Add(image);
                await _context.SaveChangesAsync();

                return Json(new {
                    id = image.Id,
                    name = image.Name,
                    alt = string.IsNullOrWhiteSpace(image.Alt) ? image.Name : image.Alt,
                    url = $"/Image/File/{image.Id}",
                    width = image.Width,
                    height = image.Height,
                    createdAt = image.CreatedAt
                });
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
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
