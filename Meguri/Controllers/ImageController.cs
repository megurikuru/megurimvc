using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ImageController(
            ApplicationDbContext context,
            ITagService tagService,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService,
            UserManager<ApplicationUser> userManager) {
            _context = context;
            _tagService = tagService;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
            _userManager = userManager;
        }

        /// <summary>
        /// 画像閲覧権限の判定
        /// 1. 非公開画像: 会員（認証済み）のみ閲覧可能
        /// 2. R18画像またはグロテスク画像: 会員かつ18歳以上のみ閲覧可能
        /// 3. 公開かつ一般画像: 全ユーザー閲覧可能
        /// </summary>
        private async Task<bool> CanViewImageAsync(Image image) {
            // 非公開画像はログイン会員のみ
            if (!image.IsPublic) {
                if (User.Identity?.IsAuthenticated != true) {
                    return false;
                }
            }

            // R18 / グロテスク画像は会員かつ18歳以上のみ
            if (image.IsSexual || image.IsViolence) {
                if (User.Identity?.IsAuthenticated != true) {
                    return false;
                }

                var isAdult = await IsUserAdultAsync();
                if (!isAdult) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ログイン中のユーザーが18歳以上かどうか判定
        /// </summary>
        private async Task<bool> IsUserAdultAsync() {
            if (User.Identity?.IsAuthenticated != true) {
                return false;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return false;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return user.DateOfBirth.AddYears(18) <= today;
        }

        // GET: /Image
        public async Task<IActionResult> Index(int? fandomId, string? tag, int? skip) {
            const int pageSize = 40;

            var query = _context.Images
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(i => i.PostImages).ThenInclude(pi => pi.Post)
                .Include(i => i.Reactions)
                .Include(i => i.Comments)
                .AsQueryable();

            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isAdult = isAuthenticated && await IsUserAdultAsync();

            if (!isAuthenticated) {
                // 未ログインユーザー: 公開画像かつ一般（非NSFW）のみ
                query = query.Where(i => i.IsPublic && !i.IsSexual && !i.IsViolence);
            } else if (!isAdult) {
                // 18歳未満または生年月日未登録の会員: 一般画像のみ (公開 + 会員用)
                query = query.Where(i => !i.IsSexual && !i.IsViolence);
            }
            // 18歳以上の会員: 全て閲覧可能

            if (!string.IsNullOrEmpty(tag)) {
                var normalized = tag.Trim().ToLowerInvariant();
                query = query.Where(i => i.ImageTags.Any(it => it.TagConcept.Tags.Any(t => t.NormalizedText == normalized)));
                ViewBag.CurrentTag = tag;
            }

            var totalCount = await query.CountAsync();
            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var images = await query
                .OrderByDescending(i => i.Created)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Skip = resolvedSkip;
            ViewBag.HasPrevious = resolvedSkip > 0;
            ViewBag.HasNext = resolvedSkip + images.Count < totalCount;
            ViewBag.PreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.NextSkip = resolvedSkip + pageSize;
            return View(images);
        }

        // GET: /Image/Details/5
        public async Task<IActionResult> Details(long? id) {
            if (id == null) return NotFound();

            var image = await _context.Images
                .Include(i => i.User)
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.Tags)
                .Include(i => i.ImageTags).ThenInclude(it => it.TagConcept).ThenInclude(tc => tc.User)
                .Include(i => i.PostImages).ThenInclude(pi => pi.Post).ThenInclude(p => p.User)
                .Include(i => i.Reactions).ThenInclude(r => r.User)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .Include(i => i.Comments).ThenInclude(c => c.Replies)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            if (!await CanViewImageAsync(image)) {
                if (User.Identity?.IsAuthenticated != true) {
                    return Challenge();
                }
                return Forbid();
            }

            return View(image);
        }

        // GET: /Image/File/5
        public async Task<IActionResult> File(long id) {
            var image = await _context.Images.FindAsync(id);
            if (image == null || string.IsNullOrEmpty(image.StorageKey)) {
                return NotFound();
            }

            // 認証・認可チェック (R18/グロ/非公開)
            if (!await CanViewImageAsync(image)) {
                if (User.Identity?.IsAuthenticated != true) {
                    return Unauthorized();
                }
                return Forbid();
            }

            var fileResult = await _r2StorageService.GetFileAsync(image.StorageKey);
            if (fileResult == null) {
                return NotFound();
            }

            // キャッシュ制御ヘッダー
            if (image.IsPublic && !image.IsSexual && !image.IsViolence) {
                Response.Headers["Cache-Control"] = "public, max-age=86400";
            } else {
                Response.Headers["Cache-Control"] = "private, no-cache";
            }

            return File(fileResult.Value.Stream, fileResult.Value.ContentType);
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdult = await IsUserAdultAsync();

            if ((model.IsSexual || model.IsViolence) && !isAdult) {
                ModelState.AddModelError(string.Empty, "18歳未満または生年月日未登録のアカウントは、R-18/R-18G画像の投稿はできません。");
            }

            if (!ModelState.IsValid) {
                ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                return View(model);
            }

            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, userId);

            // 単体または複数枚のImageエンティティを作成
            var createdImages = new List<Image>();

            for (int i = 0; i < model.Files.Count; i++) {
                var file = model.Files[i];
                if (file.Length > 0) {
                    try {
                        // サーバー側画像検証・セキュリティチェック・WebP変換・250KB以下への圧縮
                        var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);

                        // R2保存用キー: {userId}/{uuid}.webp
                        var storageKey = $"{userId}/{Guid.NewGuid():N}.webp";

                        // R2へアップロード
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
                            Description = model.Description ?? string.Empty,
                            Caption = model.Caption ?? string.Empty,
                            IsPublic = model.IsPublic,
                            IsSexual = model.IsSexual,
                            IsViolence = model.IsViolence
                        };

                        image.UserImages.Add(new UserImage {
                            UserId = userId,
                            Image = image
                        });

                        foreach (var tc in tagConcepts) {
                            image.ImageTags.Add(new ImageTag { TagConceptId = tc.Id });
                        }

                        _context.Images.Add(image);
                        createdImages.Add(image);
                    } catch (Exception ex) {
                        ModelState.AddModelError("Files", $"{file.FileName}: {ex.Message}");
                        ViewBag.Fandoms = await _context.Fandoms.ToListAsync();
                        return View(model);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // 複数枚投稿、または界隈(Fandom)への投稿の場合はPostとしてまとめる
            if (model.FandomId.HasValue || createdImages.Count > 1) {
                var fandomId = model.FandomId ?? (await _context.Fandoms.Select(f => f.Id).FirstOrDefaultAsync());
                if (fandomId == 0) fandomId = 1;

                var post = new Post {
                    UserId = userId,
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (image.UserId != currentUserId && !User.IsInRole("Admin")) {
                return Forbid();
            }

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

            var image = await _context.Images
                .Include(i => i.ImageTags)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (image.UserId != currentUserId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            var isAdult = await IsUserAdultAsync();
            if ((model.IsSexual || model.IsViolence) && !isAdult) {
                ModelState.AddModelError(string.Empty, "18歳未満または生年月日未登録のアカウントは、R-18/R-18G画像の設定はできません。");
            }

            if (!ModelState.IsValid) return View(model);

            image.Caption = model.Caption ?? string.Empty;
            image.Description = model.Description ?? string.Empty;
            image.IsPublic = model.IsPublic;
            image.IsSexual = model.IsSexual;
            image.IsViolence = model.IsViolence;

            var tagConcepts = await _tagService.GetOrCreateTagConceptsAsync(model.Tags, currentUserId);

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
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (image.UserId != currentUserId && !User.IsInRole("Admin")) {
                    return Forbid();
                }

                // R2から画像ファイルを削除
                if (!string.IsNullOrEmpty(image.StorageKey)) {
                    await _r2StorageService.DeleteFileAsync(image.StorageKey);
                }

                _context.Images.Remove(image);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
