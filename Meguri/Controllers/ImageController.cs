using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Models.ImageViewModels;
using Meguri.Services;

namespace Meguri.Controllers {
    public class ImageController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly ITagService _tagService;

        public ImageController(ApplicationDbContext context, ITagService tagService) {
            _context = context;
            _tagService = tagService;
        }

        // GET: /Image
        public async Task<IActionResult> Index(int? fandomId, string? tag) {
            var query = _context.Images
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(i => i.PostImages).ThenInclude(pi => pi.Post)
                .Include(i => i.Reactions)
                .Include(i => i.Comments)
                .Where(i => i.IsPublic);

            if (!string.IsNullOrEmpty(tag)) {
                var normalized = tag.Trim().ToLowerInvariant();
                query = query.Where(i => i.ImageTags.Any(it => it.TagConcept.Tags.Any(t => t.NormalizedText == normalized)));
                ViewBag.CurrentTag = tag;
            }

            var images = await query
                .OrderByDescending(i => i.Created)
                .Take(50)
                .ToListAsync();

            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            return View(images);
        }

        // GET: /Image/Details/5
        public async Task<IActionResult> Details(long? id) {
            if (id == null) return NotFound();

            var image = await _context.Images
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.User)
                .Include(i => i.PostImages).ThenInclude(pi => pi.Post).ThenInclude(p => p.User)
                .Include(i => i.Reactions).ThenInclude(r => r.User)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .Include(i => i.Comments).ThenInclude(c => c.Replies)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            return View(image);
        }

        // GET: /Image/File/5
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> File(long id) {
            var image = await _context.Images.FindAsync(id);
            if (image == null || image.Content == null) {
                return NotFound();
            }
            return File(image.Content, "image/jpeg");
        }

        // GET: /Image/Upload
        [Authorize]
        public async Task<IActionResult> Upload(int? fandomId) {
            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            ViewBag.DefaultFandomId = fandomId;
            return View(new ImageUploadViewModel { FandomId = fandomId });
        }

        // POST: /Image/Upload
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImageUploadViewModel model) {
            if (model.Files == null || model.Files.Count == 0) {
                ModelState.AddModelError("Files", "画像ファイルを1枚以上選択してください。");
            }

            if (!ModelState.IsValid) {
                ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, userId);

            // 単体または複数枚のImageエンティティを作成
            var createdImages = new List<Image>();

            for (int i = 0; i < model.Files.Count; i++) {
                var file = model.Files[i];
                if (file.Length > 0) {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);

                    var image = new Image {
                        Name = Path.GetFileName(file.FileName),
                        Description = model.Description ?? string.Empty,
                        Caption = model.Caption ?? string.Empty,
                        IsPublic = model.IsPublic,
                        IsSexual = model.IsSexual,
                        IsViolence = model.IsViolence,
                        Content = memoryStream.ToArray()
                    };

                    foreach (var tc in tagConcepts) {
                        image.ImageTags.Add(new ImageTag { TagConceptId = tc.Id });
                    }

                    _context.Images.Add(image);
                    createdImages.Add(image);
                }
            }

            await _context.SaveChangesAsync();

            // 複数枚投稿、または界隈(Fandom)への投稿の場合はPostとしてまとめる
            if (model.FandomId.HasValue || createdImages.Count > 1) {
                var fandomId = model.FandomId ?? (await _context.Fandoms.Select(f => f.Id).FirstOrDefaultAsync());
                if (fandomId == 0) fandomId = 1;

                var post = new Post {
                    UserId = userId ?? string.Empty,
                    Name = !string.IsNullOrWhiteSpace(model.Caption) ? model.Caption : (createdImages.Count > 1 ? $"画像ギャラリー ({createdImages.Count}枚)" : createdImages.First().Name),
                    Text = model.Description ?? string.Empty,
                    FandomId = fandomId,
                    IsPublic = model.IsPublic,
                };

                for (int i = 0; i < createdImages.Count; i++) {
                    post.PostImages.Add(new PostImage {
                        ImageId = createdImages[i].Id,
                        DisplayOrder = i
                    });
                }

                foreach (var tc in tagConcepts) {
                    post.PostTags.Add(new PostTag { TagConceptId = tc.Id });
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Post", new { id = post.Id });
            }

            if (createdImages.Any()) {
                return RedirectToAction(nameof(Details), new { id = createdImages.First().Id });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Image/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(long? id) {
            if (id == null) return NotFound();

            var image = await _context.Images
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            var vm = new ImageEditViewModel {
                Id = image.Id,
                Caption = image.Caption,
                Description = image.Description,
                IsPublic = image.IsPublic,
                IsSexual = image.IsSexual,
                IsViolence = image.IsViolence,
                Tags = image.ImageTags
                    .Select(it => it.TagConcept.Tags.FirstOrDefault()?.TagText)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => t!)
                    .ToList()
            };

            return View(vm);
        }

        // POST: /Image/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, ImageEditViewModel model) {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid) return View(model);

            var image = await _context.Images
                .Include(i => i.ImageTags)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            image.Caption = model.Caption ?? string.Empty;
            image.Description = model.Description ?? string.Empty;
            image.IsPublic = model.IsPublic;
            image.IsSexual = model.IsSexual;
            image.IsViolence = model.IsViolence;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, userId);

            image.ImageTags.Clear();
            foreach (var tc in tagConcepts) {
                image.ImageTags.Add(new ImageTag { ImageId = image.Id, TagConceptId = tc.Id });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = image.Id });
        }

        // POST: /Image/Delete/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id) {
            var image = await _context.Images.FindAsync(id);
            if (image != null) {
                _context.Images.Remove(image);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
