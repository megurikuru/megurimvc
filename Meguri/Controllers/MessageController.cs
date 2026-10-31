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
using Microsoft.Extensions.Localization;
using Meguri.Data;
using Meguri.Models;
using Meguri.ViewModels.Message;
using Meguri.Services;

namespace Meguri.Controllers {
    [Authorize]
    public class MessageController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MessageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IR2StorageService r2StorageService,
            IImageProcessingService imageProcessingService,
            IStringLocalizer<SharedResource> localizer) {
            _context = context;
            _userManager = userManager;
            _r2StorageService = r2StorageService;
            _imageProcessingService = imageProcessingService;
            _localizer = localizer;
        }

        // GET: /Message
        public async Task<IActionResult> Index(DateTime? date, int? skip) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            const int pageSize = 10;
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
                    title = otherMember?.User == null ? _localizer["Message_Direct"].Value : otherMember.User.UserName + (otherMember.User.IsWithdrawn ? _localizer["Message_WithdrawnUserSuffix"].Value : "");
                }

                vmList.Add(new ConversationItemViewModel {
                    Id = conv.Id,
                    Title = title,
                    IsGroup = conv.IsGroup,
                    LastMessageText = lastMsg == null ? _localizer["Message_NoMessagesInConv"].Value : (lastMsg.IsDeleted ? _localizer["Message_Deleted"].Value : lastMsg.Body),
                    LastMessageTime = lastMsg?.CreatedAt ?? conv.CreatedAt,
                    MemberNames = conv.Members.Select(m => m.User?.UserName ?? _localizer["Common_UnknownUser"].Value).ToList()
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

        // GET: /Message/Details/5
        public async Task<IActionResult> Details(long id, DateTime? date, int? skip) {
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
                displayTitle = otherMember?.User == null ? _localizer["Message_Direct"].Value : otherMember.User.UserName + (otherMember.User.IsWithdrawn ? _localizer["Message_WithdrawnUserSuffix"].Value : "");
            }

            const int pageSize = 10;
            const int halfWindow = pageSize / 2;

            var baseQuery = _context.Messages.Where(m => m.ConversationId == id);
            var headers = await baseQuery
                .Select(m => new { m.Id, m.ParentMessageId, m.CreatedAt })
                .ToListAsync();

            var rootOf = new Dictionary<long, long>();
            var byId = headers.ToDictionary(h => h.Id);
            long FindRoot(long messageId) {
                if (rootOf.TryGetValue(messageId, out var cached)) return cached;
                var current = messageId;
                var guard = 0;
                while (byId[current].ParentMessageId is long p && byId.ContainsKey(p) && guard++ < 10000) {
                    current = p;
                }
                rootOf[messageId] = current;
                return current;
            }
            var threads = headers
                .GroupBy(h => FindRoot(h.Id))
                .Select(g => new { RootId = g.Key, Latest = g.Max(x => x.CreatedAt) })
                .OrderByDescending(t => t.Latest)
                .ToList();
            var totalCount = threads.Count;

            int resolvedSkip;
            if (skip.HasValue) {
                resolvedSkip = skip.Value;
            } else if (date.HasValue) {
                var anchorUtc = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc).AddDays(1);
                var newerCount = threads.Count(t => t.Latest >= anchorUtc);
                resolvedSkip = Math.Max(0, newerCount - halfWindow);
            } else {
                resolvedSkip = 0;
            }

            if (totalCount == 0 || resolvedSkip < 0) {
                resolvedSkip = 0;
            } else if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            var pageRootIds = threads.Skip(resolvedSkip).Take(pageSize).Select(t => t.RootId).ToHashSet();
            var pageMessageIds = headers.Where(h => pageRootIds.Contains(FindRoot(h.Id))).Select(h => h.Id).ToList();

            var messages = await _context.Messages
                .Where(m => pageMessageIds.Contains(m.Id))
                .Include(m => m.Sender)
                .Include(m => m.MessageImages).ThenInclude(mi => mi.Image)
                .Include(m => m.Reactions).ThenInclude(r => r.User)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            messages = OrderRepliesUnderParents(messages);

            var hasPrevious = resolvedSkip > 0;
            var hasNext = resolvedSkip + pageRootIds.Count < totalCount;

            var vm = new MessageRoomViewModel {
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


        // GET: /Message/Create
        public async Task<IActionResult> Create() {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var model = new CreateConversationViewModel();
            await LoadAvailableUsersAsync(model, currentUserId);
            return View(model);
        }

        // POST: /Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateConversationViewModel model) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            if (model.SelectedUserIds == null || model.SelectedUserIds.Count == 0) {
                ModelState.AddModelError("SelectedUserIds", _localizer["Message_Error_SelectRecipientRequired"]);
            }

            if (!ModelState.IsValid) {
                await LoadAvailableUsersAsync(model, currentUserId);
                return View(model);
            }

            // 1対1の場合、既に同一相手との会話があればそれを再利用
            if (!model.IsGroup && model.SelectedUserIds.Count == 1) {
                var targetUserId = model.SelectedUserIds[0];
                var existingConv = await _context.Conversations
                    .Where(c => !c.IsGroup)
                    .Where(c => c.Members.Any(m => m.UserId == currentUserId) && c.Members.Any(m => m.UserId == targetUserId))
                    .FirstOrDefaultAsync();

                if (existingConv != null) {
                    await SendMessageInternal(existingConv.Id, currentUserId, model.InitialMessage, model.ImageFiles, model.Subject);
                    return RedirectToAction(nameof(Details), new { id = existingConv.Id });                }
            }

            var conv = new Conversation {
                Title = model.IsGroup ? (model.Title ?? _localizer["Message_Group"].Value) : string.Empty,
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

            await SendMessageInternal(conv.Id, currentUserId, model.InitialMessage, model.ImageFiles, model.Subject);

            return RedirectToAction(nameof(Details), new { id = conv.Id });
        }

        // GET: /Message/Edit/5 (メッセージID)
        public async Task<IActionResult> Edit(long id) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            if (message.SenderId != currentUserId) return Forbid();
            if (message.IsDeleted) return NotFound();

            return View(new EditMessageViewModel {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Subject = message.Subject,
                                Body = message.Body
            });
        }

        // POST: /Message/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditMessageViewModel model) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();
            if (id != model.Id) return BadRequest();

            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            if (message.SenderId != currentUserId) return Forbid();
            if (message.IsDeleted) return NotFound();

            if (!ModelState.IsValid) {
                model.ConversationId = message.ConversationId;
                return View(model);
            }

            message.Subject = model.Subject ?? string.Empty;
                        message.Body = model.Body;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = message.ConversationId });
        }

        // POST: /Message/Delete/5 (メッセージID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var message = await _context.Messages
                .Include(m => m.MessageImages)
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();
            if (message.SenderId != currentUserId) return Forbid();

            var conversationId = message.ConversationId;

            if (!message.IsDeleted) {
                message.IsDeleted = true;
                message.Subject = string.Empty;
                message.Body = string.Empty;
                message.UpdatedAt = DateTime.UtcNow;
                _context.MessageImages.RemoveRange(message.MessageImages);
                _context.Reactions.RemoveRange(message.Reactions);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = conversationId });
        }

        private const int MaxRepliesPerParent = 100;

        // POST: /Message/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(long conversationId, string? subject, string text, List<IFormFile>? imageFiles, long? parentMessageId = null) {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId);

            if (!isMember) return Forbid();

            if (!string.IsNullOrWhiteSpace(text)) {
                if (parentMessageId.HasValue && !await _context.Messages.AnyAsync(m => m.Id == parentMessageId.Value && m.ConversationId == conversationId)) {
                    parentMessageId = null;
                }
                if (parentMessageId.HasValue && await _context.Messages.CountAsync(m => m.ParentMessageId == parentMessageId.Value) >= MaxRepliesPerParent) {
                    TempData["Error"] = string.Format(_localizer["Message_Error_MaxRepliesPerParentFormat"].Value, MaxRepliesPerParent);
                    return RedirectToAction(nameof(Details), new { id = conversationId });
                }
                await SendMessageInternal(conversationId, currentUserId, text, imageFiles, subject, parentMessageId);
            }

            return RedirectToAction(nameof(Details), new { id = conversationId });
        }

        private async Task LoadAvailableUsersAsync(CreateConversationViewModel model, string currentUserId) {
            model.AvailableUsers = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .Take(50)
                .ToListAsync();
        }

        private static List<Message> OrderRepliesUnderParents(List<Message> messages) {
            var ids = messages.Select(m => m.Id).ToHashSet();
            var children = messages
                .Where(m => m.ParentMessageId.HasValue && ids.Contains(m.ParentMessageId.Value))
                .GroupBy(m => m.ParentMessageId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).ToList());
            var result = new List<Message>(messages.Count);
            DateTime LatestInThread(Message m) {
                var latest = m.CreatedAt;
                if (children.TryGetValue(m.Id, out var list)) {
                    foreach (var c in list) {
                        var t = LatestInThread(c);
                        if (t > latest) latest = t;
                    }
                }
                return latest;
            }
            void Add(Message m) {
                result.Add(m);
                if (children.TryGetValue(m.Id, out var list)) {
                    foreach (var c in list) Add(c);
                }
            }
            var roots = messages
                .Where(m => !m.ParentMessageId.HasValue || !ids.Contains(m.ParentMessageId.Value))
                .OrderByDescending(LatestInThread);
            foreach (var m in roots) {
                Add(m);
            }
            return result;
        }

        private async Task SendMessageInternal(long conversationId, string senderId, string text,
        List<IFormFile>? imageFiles, string? subject = null, long? parentMessageId = null) {
            var message = new Message {
                ConversationId = conversationId,
                ParentMessageId = parentMessageId,
                SenderId = senderId,
                Subject = subject ?? string.Empty,
                                Body = text ?? string.Empty,
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
                                Alt = _localizer["Message_AttachmentAlt"].Value
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
