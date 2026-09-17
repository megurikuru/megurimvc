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

namespace Meguri.Controllers {
    public class CommentController : Controller {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context) {
            _context = context;
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
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                IsDeleted = false
            };

            // 画像の添付
            if (commentImage != null && commentImage.Length > 0) {
                using var ms = new MemoryStream();
                await commentImage.CopyToAsync(ms);

                var img = new Image {
                    Name = Path.GetFileName(commentImage.FileName),
                    Description = $"Comment >>{nextNumber} attachment",
                    Caption = string.Empty,
                    IsPublic = true,
                    Content = ms.ToArray()
                };
                _context.Images.Add(img);
                await _context.SaveChangesAsync();

                comment.CommentImages.Add(new CommentImage {
                    ImageId = img.Id,
                    DisplayOrder = 0
                });
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
