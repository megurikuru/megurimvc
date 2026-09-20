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
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;

        public PostController(
            ApplicationDbContext context,
            ITagService tagService,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService) {
            _context = context;
            _tagService = tagService;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
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
                IsSexual = model.IsSexual,
                IsViolence = model.IsViolence,
                IsPublic = model.IsPublic,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            // 画像の保存と添付
            if (model.ImageFiles != null && model.ImageFiles.Count > 0) {
                int order = 0;
                foreach (var file in model.ImageFiles) {
                    if (file.Length > 0) {
                        try {
                            var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);
                            var storageKey = $"{userId}/{Guid.NewGuid():N}.webp";

                            using var uploadStream = new MemoryStream(processed.Data);
                            await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

                            var image = new Image {
                                UserId = userId,
                                Name = Path.GetFileNameWithoutExtension(file.FileName) + ".webp",
                                StorageKey = storageKey,
                                ContentType = processed.ContentType,
                                FileSize = processed.FileSize,
                                Width = processed.Width,
                                Height = processed.Height,
                                Description = model.Name,
                                Caption = string.Empty,
                                IsPublic = model.IsPublic,
                                IsSexual = model.IsSexual,
                                IsViolence = model.IsViolence
                            };
                            image.UserImages.Add(new UserImage {
                                UserId = userId,
                                Image = image
                            });
                            _context.Images.Add(image);
                            await _context.SaveChangesAsync();

                            post.PostImages.Add(new PostImage {
                                ImageId = image.Id,
                                DisplayOrder = order++
                            });
                        } catch (Exception ex) {
                            ModelState.AddModelError("ImageFiles", $"{file.FileName}: {ex.Message}");
                            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                            return View(model);
                        }
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
                IsSexual = post.IsSexual,
                IsViolence = post.IsViolence,
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
            post.IsSexual = model.IsSexual;
            post.IsViolence = model.IsViolence;
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
                        try {
                            var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);
                            var storageKey = $"{userId}/{Guid.NewGuid():N}.webp";

                            using var uploadStream = new MemoryStream(processed.Data);
                            await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

                            var image = new Image {
                                UserId = userId,
                                Name = Path.GetFileNameWithoutExtension(file.FileName) + ".webp",
                                StorageKey = storageKey,
                                ContentType = processed.ContentType,
                                FileSize = processed.FileSize,
                                Width = processed.Width,
                                Height = processed.Height,
                                Description = model.Name,
                                Caption = string.Empty,
                                IsPublic = model.IsPublic,
                                IsSexual = model.IsSexual,
                                IsViolence = model.IsViolence
                            };
                            image.UserImages.Add(new UserImage {
                                UserId = userId,
                                Image = image
                            });
                            _context.Images.Add(image);
                            await _context.SaveChangesAsync();

                            post.PostImages.Add(new PostImage {
                                ImageId = image.Id,
                                DisplayOrder = nextOrder++
                            });
                        } catch (Exception ex) {
                            ModelState.AddModelError("NewImageFiles", $"{file.FileName}: {ex.Message}");
                            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                            return View(model);
                        }
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
            var post = await _context.Posts
                .Include(p => p.PostImages)
                .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post != null) {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (post.UserId == userId || User.IsInRole("Admin")) {
                    foreach (var pi in post.PostImages) {
                        if (pi.Image != null && !string.IsNullOrEmpty(pi.Image.StorageKey)) {
                            await _r2StorageService.DeleteFileAsync(pi.Image.StorageKey);
                        }
                    }
                    _context.Posts.Remove(post);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
