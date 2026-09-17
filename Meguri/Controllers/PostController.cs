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
using Meguri.Models.PostViewModels;
using Meguri.Services;

namespace Meguri.Controllers {
    public class PostController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly ITagService _tagService;

        public PostController(ApplicationDbContext context, ITagService tagService) {
            _context = context;
            _tagService = tagService;
        }

        // GET: /Post
        public async Task<IActionResult> Index(int? fandomId, string? tag, string? search) {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.Reactions)
                .AsQueryable();

            if (fandomId.HasValue) {
                query = query.Where(p => p.FandomId == fandomId.Value);
                ViewBag.CurrentFandom = await _context.Fandoms.FindAsync(fandomId.Value);
            }

            if (!string.IsNullOrEmpty(tag)) {
                var normalized = tag.Trim().ToLowerInvariant();
                query = query.Where(p => p.PostTags.Any(pt => pt.TagConcept.Tags.Any(t => t.NormalizedText == normalized)));
                ViewBag.CurrentTag = tag;
            }

            if (!string.IsNullOrEmpty(search)) {
                query = query.Where(p => p.Name.Contains(search) || p.Text.Contains(search));
                ViewBag.CurrentSearch = search;
            }

            var posts = await query
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.LastCommentedAt ?? p.Created)
                .Take(50)
                .ToListAsync();

            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            return View(posts);
        }

        // GET: /Post/Details/5
        public async Task<IActionResult> Details(long? id) {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Fandom)
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.User)
                .Include(p => p.Reactions).ThenInclude(r => r.User)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Include(p => p.Comments).ThenInclude(c => c.Replies).ThenInclude(r => r.User)
                .Include(p => p.Comments).ThenInclude(c => c.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            // 閲覧カウントアップ
            post.ViewCount++;
            await _context.SaveChangesAsync();

            return View(post);
        }

        // GET: /Post/Create
        [Authorize]
        public async Task<IActionResult> Create(int? fandomId) {
            var fandoms = await _context.Fandoms.ToListAsync();
            ViewBag.Fandoms = fandoms;
            return View(new PostCreateViewModel { FandomId = fandomId ?? fandoms.FirstOrDefault()?.Id ?? 1 });
        }

        // POST: /Post/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model) {
            if (!ModelState.IsValid) {
                ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var post = new Post {
                UserId = userId,
                Name = model.Name,
                Text = model.Text,
                FandomId = model.FandomId,
                Sexual = model.Sexual,
                Violence = model.Violence,
                IsPublic = model.IsPublic,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            // 画像の保存と添付
            if (model.ImageFiles != null && model.ImageFiles.Count > 0) {
                int order = 0;
                foreach (var file in model.ImageFiles) {
                    if (file.Length > 0) {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);

                        var image = new Image {
                            Name = Path.GetFileName(file.FileName),
                            Description = model.Name,
                            Caption = string.Empty,
                            IsPublic = model.IsPublic,
                            Content = ms.ToArray()
                        };
                        _context.Images.Add(image);
                        await _context.SaveChangesAsync();

                        post.PostImages.Add(new PostImage {
                            ImageId = image.Id,
                            DisplayOrder = order++
                        });
                    }
                }
            }

            // タグの関連付け（最大10個）
            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, userId);
            foreach (var tc in tagConcepts) {
                post.PostTags.Add(new PostTag { TagConceptId = tc.Id });
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        // GET: /Post/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(long? id) {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.PostImages).ThenInclude(pi => pi.Image)
                .Include(p => p.PostTags).ThenInclude(pt => pt.TagConcept).ThenInclude(tc => tc.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (post.UserId != userId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            var vm = new PostEditViewModel {
                Id = post.Id,
                Name = post.Name,
                Text = post.Text,
                FandomId = post.FandomId,
                Sexual = post.Sexual,
                Violence = post.Violence,
                IsPublic = post.IsPublic,
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                Tags = post.PostTags
                    .Select(pt => pt.TagConcept.Tags.FirstOrDefault()?.TagText)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => t!)
                    .ToList(),
                ExistingImages = post.PostImages
                    .OrderBy(pi => pi.DisplayOrder)
                    .Select(pi => new PostImageItemViewModel {
                        ImageId = pi.ImageId,
                        ImageName = pi.Image.Name,
                        DisplayOrder = pi.DisplayOrder,
                        Remove = false
                    })
                    .ToList()
            };

            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            return View(vm);
        }

        // POST: /Post/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, PostEditViewModel model) {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid) {
                ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                return View(model);
            }

            var post = await _context.Posts
                .Include(p => p.PostImages)
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (post.UserId != userId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            post.Name = model.Name;
            post.Text = model.Text;
            post.FandomId = model.FandomId;
            post.Sexual = model.Sexual;
            post.Violence = model.Violence;
            post.IsPublic = model.IsPublic;
            post.IsPinned = model.IsPinned;
            post.IsLocked = model.IsLocked;

            // 既存画像の並び替えと削除処理
            if (model.ExistingImages != null) {
                foreach (var imgItem in model.ExistingImages) {
                    var postImage = post.PostImages.FirstOrDefault(pi => pi.ImageId == imgItem.ImageId);
                    if (postImage != null) {
                        if (imgItem.Remove) {
                            post.PostImages.Remove(postImage);
                        } else {
                            postImage.DisplayOrder = imgItem.DisplayOrder;
                        }
                    }
                }
            }

            // 新規画像の追加
            if (model.NewImageFiles != null && model.NewImageFiles.Count > 0) {
                int nextOrder = post.PostImages.Any() ? post.PostImages.Max(pi => pi.DisplayOrder) + 1 : 0;
                foreach (var file in model.NewImageFiles) {
                    if (file.Length > 0) {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);

                        var image = new Image {
                            Name = Path.GetFileName(file.FileName),
                            Description = model.Name,
                            Caption = string.Empty,
                            IsPublic = model.IsPublic,
                            Content = ms.ToArray()
                        };
                        _context.Images.Add(image);
                        await _context.SaveChangesAsync();

                        post.PostImages.Add(new PostImage {
                            ImageId = image.Id,
                            DisplayOrder = nextOrder++
                        });
                    }
                }
            }

            // タグの更新（最大10個）
            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, userId);
            post.PostTags.Clear();
            foreach (var tc in tagConcepts) {
                post.PostTags.Add(new PostTag { PostId = post.Id, TagConceptId = tc.Id });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        // POST: /Post/Delete/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id) {
            var post = await _context.Posts.FindAsync(id);
            if (post != null) {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (post.UserId == userId || User.IsInRole("Admin")) {
                    _context.Posts.Remove(post);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
