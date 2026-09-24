using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Models.MessageViewModels;
using Meguri.Services;

namespace Meguri.Controllers {
    [Authorize]
    public class MessageController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;

        public MessageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService) {
            _context = context;
            _userManager = userManager;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
        }

        // GET: /Message
        public async Task<IActionResult> Index(DateTime? date, int? skip) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            const int pageSize = 20;
            const int halfWindow = pageSize / 2;

            var baseQuery = _context.Conversations
                .Where(c => c.Members.Any(m => m.UserId == currentUserId));

            var totalCount = await baseQuery.CountAsync();

            int resolvedSkip;
            if (skip.HasValue) {
                resolvedSkip = skip.Value;
            } else if (date.HasValue) {
                var anchorStart = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                var anchorEnd = anchorStart.AddDays(1);
                // 指定日より新しい会話の件数（一覧では上側に表示される）
                var moreRecentCount = await baseQuery.CountAsync(c => c.UpdatedAt >= anchorEnd);
                resolvedSkip = Math.Max(0, moreRecentCount - halfWindow);
            } else {
                resolvedSkip = 0;
            }

            if (totalCount == 0) {
                resolvedSkip = 0;
            } else if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var conversations = await baseQuery
                .Include(c => c.Members).ThenInclude(m => m.User)
                .Include(c => c.Messages).ThenInclude(m => m.Sender)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            var vmList = new List<ConversationItemViewModel>();
            foreach (var conv in conversations) {
                var lastMsg = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                string title = conv.Title;
                if (!conv.IsGroup || string.IsNullOrEmpty(title)) {
                    var otherMember = conv.Members.FirstOrDefault(m => m.UserId != currentUserId);
                    title = otherMember?.User?.UserName ?? "ダイレクトメッセージ";
                }

                vmList.Add(new ConversationItemViewModel {
                    Id = conv.Id,
                    Title = title,
                    IsGroup = conv.IsGroup,
                    LastMessageText = lastMsg?.Text ?? "(メッセージなし)",
                    LastMessageTime = lastMsg?.CreatedAt ?? conv.CreatedAt,
                    MemberNames = conv.Members.Select(m => m.User?.UserName ?? "User").ToList()
                });
            }

            var availableUsers = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .Take(50)
                .ToListAsync();

            var hasPrevious = resolvedSkip > 0;
            var hasNext = resolvedSkip + conversations.Count < totalCount;

            var vm = new ConversationListViewModel {
                Conversations = vmList,
                AvailableUsers = availableUsers,
                PageSize = pageSize,
                TotalCount = totalCount,
                Skip = resolvedSkip,
                FilterDate = date,
                HasPrevious = hasPrevious,
                HasNext = hasNext,
                PreviousSkip = Math.Max(0, resolvedSkip - pageSize),
                NextSkip = resolvedSkip + pageSize
            };

            return View(vm);
        }

        // GET: /Message/Chat/5
        public async Task<IActionResult> Chat(long id, DateTime? date, int? skip) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == id && cm.UserId == currentUserId);

            if (!isMember) return Forbid();

            var conversation = await _context.Conversations
                .Include(c => c.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null) return NotFound();

            string displayTitle = conversation.Title;
            if (!conversation.IsGroup || string.IsNullOrEmpty(displayTitle)) {
                var otherMember = conversation.Members.FirstOrDefault(m => m.UserId != currentUserId);
                displayTitle = otherMember?.User?.UserName ?? "ダイレクトメッセージ";
            }

            const int pageSize = 80;
            const int halfWindow = pageSize / 2;

            var baseQuery = _context.Messages.Where(m => m.ConversationId == id);
            var totalCount = await baseQuery.CountAsync();

            int resolvedSkip;
            if (skip.HasValue) {
                resolvedSkip = skip.Value;
            } else if (date.HasValue) {
                var anchorUtc = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                var beforeCount = await baseQuery.CountAsync(m => m.CreatedAt < anchorUtc);
                resolvedSkip = Math.Max(0, beforeCount - halfWindow);
            } else {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            if (totalCount == 0) {
                resolvedSkip = 0;
            } else if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var messages = await baseQuery
                .Include(m => m.Sender)
                .Include(m => m.MessageImages).ThenInclude(mi => mi.Image)
                .Include(m => m.Reactions).ThenInclude(r => r.User)
                .OrderBy(m => m.CreatedAt)
                .Skip(resolvedSkip)
                .Take(pageSize)
                .ToListAsync();

            var hasPrevious = resolvedSkip > 0;
            var hasNext = resolvedSkip + messages.Count < totalCount;

            var vm = new ChatRoomViewModel {
                Conversation = conversation,
                CurrentUserId = currentUserId,
                DisplayTitle = displayTitle,
                Messages = messages,
                PageSize = pageSize,
                TotalCount = totalCount,
                Skip = resolvedSkip,
                FilterDate = date,
                HasPrevious = hasPrevious,
                HasNext = hasNext,
                PreviousSkip = Math.Max(0, resolvedSkip - pageSize),
                NextSkip = resolvedSkip + pageSize
            };

            return View(vm);
        }


        // POST: /Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateConversationViewModel model) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            if (model.SelectedUserIds == null || model.SelectedUserIds.Count == 0) {
                ModelState.AddModelError("SelectedUserIds", "宛先を1人以上選択してください。");
            }

            if (!ModelState.IsValid) {
                return RedirectToAction(nameof(Index));
            }

            // 1対1の場合、既に同一相手との会話があればそれを再利用
            if (!model.IsGroup && model.SelectedUserIds.Count == 1) {
                var targetUserId = model.SelectedUserIds[0];
                var existingConv = await _context.Conversations
                    .Where(c => !c.IsGroup)
                    .Where(c => c.Members.Any(m => m.UserId == currentUserId) && c.Members.Any(m => m.UserId == targetUserId))
                    .FirstOrDefaultAsync();

                if (existingConv != null) {
                    await SendMessageInternal(existingConv.Id, currentUserId, model.InitialMessage, model.ImageFiles);
                    return RedirectToAction(nameof(Chat), new { id = existingConv.Id });
                }
            }

            var conv = new Conversation {
                Title = model.IsGroup ? (model.Title ?? "グループチャット") : string.Empty,
                IsGroup = model.IsGroup,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            conv.Members.Add(new ConversationMember { UserId = currentUserId, JoinedAt = DateTime.UtcNow });
            foreach (var uid in model.SelectedUserIds.Distinct()) {
                if (uid != currentUserId) {
                    conv.Members.Add(new ConversationMember { UserId = uid, JoinedAt = DateTime.UtcNow });
                }
            }

            _context.Conversations.Add(conv);
            await _context.SaveChangesAsync();

            await SendMessageInternal(conv.Id, currentUserId, model.InitialMessage, model.ImageFiles);

            return RedirectToAction(nameof(Chat), new { id = conv.Id });
        }

        // POST: /Message/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(long conversationId, string text, List<IFormFile>? imageFiles) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId);

            if (!isMember) return Forbid();

            if (!string.IsNullOrWhiteSpace(text) || (imageFiles != null && imageFiles.Count > 0)) {
                await SendMessageInternal(conversationId, currentUserId, text, imageFiles);
            }

            return RedirectToAction(nameof(Chat), new { id = conversationId });
        }

        private async Task SendMessageInternal(long conversationId, string senderId, string text, List<IFormFile>? imageFiles) {
            var message = new Message {
                ConversationId = conversationId,
                SenderId = senderId,
                Text = text ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (imageFiles != null && imageFiles.Count > 0) {
                int order = 0;
                foreach (var file in imageFiles) {
                    if (file.Length > 0) {
                        try {
                            var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);
                            var storageKey = $"{senderId}/{Guid.NewGuid():N}.webp";

                            using var uploadStream = new MemoryStream(processed.Data);
                            await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

                            var img = new Image {
                                UserId = senderId,
                                Name = Path.GetFileNameWithoutExtension(file.FileName) + ".webp",
                                StorageKey = storageKey,
                                ContentType = processed.ContentType,
                                FileSize = processed.FileSize,
                                Width = processed.Width,
                                Height = processed.Height,
                                Description = "Message Attachment",
                                Caption = string.Empty,
                                IsPublic = false
                            };
                            img.UserImages.Add(new UserImage {
                                UserId = senderId,
                                Image = img
                            });
                            _context.Images.Add(img);
                            await _context.SaveChangesAsync();

                            message.MessageImages.Add(new MessageImage {
                                ImageId = img.Id,
                                DisplayOrder = order++
                            });
                        } catch {
                            // 画像処理失敗時はスキップ
                        }
                    }
                }
            }

            _context.Messages.Add(message);

            var conv = await _context.Conversations.FindAsync(conversationId);
            if (conv != null) {
                conv.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
