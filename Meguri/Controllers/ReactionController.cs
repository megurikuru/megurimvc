using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;

namespace Meguri.Controllers {
    public class ReactionController : Controller {
        private readonly ApplicationDbContext _context;

        public ReactionController(ApplicationDbContext context) {
            _context = context;
        }

        // POST: /Reaction/Toggle
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string targetType, long targetId, int reactionType) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) {
                return Unauthorized();
            }

            var type = (ReactionType)reactionType;

            var query = _context.Reactions.Where(r => r.Type == type);
            Reaction? existingReaction = null;

            if (targetType == "Post") {
                query = query.Where(r => r.PostId == targetId);
                existingReaction = await query.FirstOrDefaultAsync(r => r.UserId == userId);
            } else if (targetType == "Image") {
                query = query.Where(r => r.ImageId == targetId);
                existingReaction = await query.FirstOrDefaultAsync(r => r.UserId == userId);
            } else if (targetType == "Comment") {
                query = query.Where(r => r.CommentId == targetId);
                existingReaction = await query.FirstOrDefaultAsync(r => r.UserId == userId);
            } else if (targetType == "Message") {
                query = query.Where(r => r.MessageId == targetId);
                existingReaction = await query.FirstOrDefaultAsync(r => r.UserId == userId);
            } else {
                return BadRequest("Invalid target type");
            }

            bool hasReacted;
            if (existingReaction != null) {
                _context.Reactions.Remove(existingReaction);
                hasReacted = false;
            } else {
                var reaction = new Reaction {
                    UserId = userId,
                    Type = type,
                    Created = DateTime.UtcNow,
                    PostId = targetType == "Post" ? targetId : null,
                    ImageId = targetType == "Image" ? targetId : null,
                    CommentId = targetType == "Comment" ? targetId : null,
                    MessageId = targetType == "Message" ? targetId : null,
                };
                _context.Reactions.Add(reaction);
                hasReacted = true;
            }

            await _context.SaveChangesAsync();

            // 新しいリアクション件数を取得
            int count;
            if (targetType == "Post") {
                count = await _context.Reactions.CountAsync(r => r.PostId == targetId && r.Type == type);
            } else if (targetType == "Image") {
                count = await _context.Reactions.CountAsync(r => r.ImageId == targetId && r.Type == type);
            } else if (targetType == "Comment") {
                count = await _context.Reactions.CountAsync(r => r.CommentId == targetId && r.Type == type);
            } else {
                count = await _context.Reactions.CountAsync(r => r.MessageId == targetId && r.Type == type);
            }

            return Json(new {
                success = true,
                hasReacted = hasReacted,
                count = count
            });
        }
    }
}
