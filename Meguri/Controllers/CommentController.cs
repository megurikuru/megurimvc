using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Services;

namespace Meguri.Controllers {
    public class CommentController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;

        public CommentController(
            ApplicationDbContext context,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService) {
            _context = context;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
        }

        // POST: /Comment/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(long? docId, long? imageId, long? parentId, string text, IFormFile? commentImage, string? returnUrl) {
            if (string.IsNullOrWhiteSpace(text) && commentImage == null) {
                return Redirect(returnUrl ?? "/");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // スレッド内の通し番号を計算
            int nextNumber = 1;
            if (docId.HasValue) {
                var maxNum = await _context.Comments
                    .Where(c => c.DocId == docId.Value)
                    .Select(c => (int?)c.Number)
                    .MaxAsync();
                nextNumber = (maxNum ?? 0) + 1;
            } else if (imageId.HasValue) {
                var maxNum = await _context.Comments
                    .Where(c => c.ImageId == imageId.Value)
                    .Select(c => (int?)c.Number)
                    .MaxAsync();
                nextNumber = (maxNum ?? 0) + 1;
            }

            var comment = new Comment {
                UserId = userId,
                DocId = docId,
                ImageId = imageId,
                ParentId = parentId,
                Number = nextNumber,
                Text = text ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // 画像の添付
            if (commentImage != null && commentImage.Length > 0) {
                try {
                    var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(commentImage);
                    var storageKey = $"{userId}/{Guid.NewGuid():N}.webp";

                    using var uploadStream = new MemoryStream(processed.Data);
                    await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

                    var img = new Image {
                        UserId = userId,
                        Name = Path.GetFileNameWithoutExtension(commentImage.FileName) + ".webp",
                        StorageKey = storageKey,
                        ContentType = processed.ContentType,
                        FileSize = processed.FileSize,
                        Width = processed.Width,
                        Height = processed.Height,
                        Description = $"Comment >>{nextNumber} attachment",
                        Caption = string.Empty,
                        IsPublic = true
                    };
                    img.UserImages.Add(new UserImage {
                        UserId = userId,
                        Image = img
                    });
                    _context.Images.Add(img);
                    await _context.SaveChangesAsync();

                    comment.CommentImages.Add(new CommentImage {
                        ImageId = img.Id,
                        DisplayOrder = 0
                    });
                } catch {
                    // 画像処理失敗時はコメントのみ保存、またはエラー
                }
            }

            _context.Comments.Add(comment);

            // PostのCommentCountとLastCommentedAtを更新
            if (docId.HasValue) {
                var post = await _context.Posts.FindAsync(docId.Value);
                if (post != null) {
                    post.CommentCount++;
                    post.LastCommentedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }

            if (docId.HasValue) return RedirectToAction("Details", "Post", new { id = docId.Value });
            if (imageId.HasValue) return RedirectToAction("Details", "Image", new { id = imageId.Value });
            return RedirectToAction("Index", "Home");
        }

        // POST: /Comment/Delete/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, string? returnUrl) {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null) {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (comment.UserId == userId || User.IsInRole("Admin")) {
                    comment.IsDeleted = true;
                    comment.Text = string.Empty;
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
