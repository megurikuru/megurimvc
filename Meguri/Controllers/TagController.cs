using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Models.TagViewModels;
using Meguri.Services;

namespace Meguri.Controllers {
    public class TagController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly ITagService _tagService;

        public TagController(ApplicationDbContext context, ITagService tagService) {
            _context = context;
            _tagService = tagService;
        }

        // GET: /Tag
        public async Task<IActionResult> Index(string? q, int? skip) {
            const int pageSize = 10;

            // 総件数の取得 (SQL)
            int totalCount;
            if (!string.IsNullOrWhiteSpace(q)) {
                var keywordPattern = $"%{q.Trim()}%";
                totalCount = await _context.Database
                    .SqlQuery<int>($@"
                        SELECT COUNT(DISTINCT tc.""Id"") AS ""Value""
                        FROM ""TagConcepts"" tc
                        INNER JOIN ""Tags"" t ON tc.""Id"" = t.""TagConceptId""
                        WHERE t.""TagText"" LIKE {keywordPattern}")
                    .SingleAsync();
            } else {
                totalCount = await _context.TagConcepts.CountAsync();
            }

            var resolvedSkip = skip.HasValue && skip.Value > 0 ? skip.Value : 0;
            if (resolvedSkip >= totalCount) {
                resolvedSkip = Math.Max(0, totalCount - pageSize);
            }

            // データの取得 (SQL)
            List<TagConcept> concepts;
            if (!string.IsNullOrWhiteSpace(q)) {
                var keywordPattern = $"%{q.Trim()}%";
                concepts = await _context.TagConcepts
                    .FromSqlInterpolated($@"
                        SELECT tc.*
                        FROM ""TagConcepts"" tc
                        INNER JOIN ""Tags"" t ON tc.""Id"" = t.""TagConceptId""
                        LEFT JOIN ""PostTags"" pt ON tc.""Id"" = pt.""TagConceptId""
                        LEFT JOIN ""ImageTags"" it ON tc.""Id"" = it.""TagConceptId""
                        WHERE t.""TagText"" LIKE {keywordPattern}
                        GROUP BY tc.""Id""
                        ORDER BY (COUNT(DISTINCT pt.""Id"") + COUNT(DISTINCT it.""Id"")) DESC
                        LIMIT {pageSize} OFFSET {resolvedSkip}")
                    .Include(tc => tc.Tags)
                    .Include(tc => tc.PostTags)
                    .Include(tc => tc.ImageTags)
                    .Include(tc => tc.User)
                    .ToListAsync();
            } else {
                concepts = await _context.TagConcepts
                    .FromSqlInterpolated($@"
                        SELECT tc.*
                        FROM ""TagConcepts"" tc
                        LEFT JOIN ""PostTags"" pt ON tc.""Id"" = pt.""TagConceptId""
                        LEFT JOIN ""ImageTags"" it ON tc.""Id"" = it.""TagConceptId""
                        GROUP BY tc.""Id""
                        ORDER BY (COUNT(DISTINCT pt.""Id"") + COUNT(DISTINCT it.""Id"")) DESC
                        LIMIT {pageSize} OFFSET {resolvedSkip}")
                    .Include(tc => tc.Tags)
                    .Include(tc => tc.PostTags)
                    .Include(tc => tc.ImageTags)
                    .Include(tc => tc.User)
                    .ToListAsync();
            }

            ViewBag.SearchQuery = q;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Skip = resolvedSkip;
            ViewBag.HasPrevious = resolvedSkip > 0;
            ViewBag.HasNext = resolvedSkip + concepts.Count < totalCount;
            ViewBag.PreviousSkip = Math.Max(0, resolvedSkip - pageSize);
            ViewBag.NextSkip = resolvedSkip + pageSize;

            return View(concepts);
        }

        // GET: /Tag/Details/5
        public async Task<IActionResult> Details(long conceptId, int? postSkip, int? imageSkip) {
            const int pageSize = 10;

            var concept = await _context.TagConcepts
                .FromSqlInterpolated($"SELECT * FROM \"TagConcepts\" WHERE \"Id\" = {conceptId}")
                .Include(tc => tc.Tags)
                .Include(tc => tc.User)
                .Include(tc => tc.SubjectRelationships).ThenInclude(sr => sr.ObjectConcept).ThenInclude(oc => oc.Tags)
                .Include(tc => tc.ObjectRelationships).ThenInclude(or => or.SubjectConcept).ThenInclude(sc => sc.Tags)
                .FirstOrDefaultAsync();

            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanEdit = concept.UserId == currentUserId || User.IsInRole("Admin");

            // PostTags の件数取得とデータ取得 (SQL)
            var postTagsTotalCount = await _context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"PostTags\" WHERE \"TagConceptId\" = {conceptId}")
                .SingleAsync();

            var resolvedPostSkip = postSkip.HasValue && postSkip.Value > 0 ? postSkip.Value : 0;
            if (resolvedPostSkip >= postTagsTotalCount) {
                resolvedPostSkip = Math.Max(0, postTagsTotalCount - pageSize);
            }

            var postTags = await _context.PostTags
                .FromSqlInterpolated($@"
                    SELECT pt.*
                    FROM ""PostTags"" pt
                    INNER JOIN ""Posts"" p ON pt.""PostId"" = p.""Id""
                    WHERE pt.""TagConceptId"" = {conceptId}
                    ORDER BY p.""CreatedAt"" DESC
                    LIMIT {pageSize} OFFSET {resolvedPostSkip}")
                .Include(pt => pt.Post).ThenInclude(p => p.User)
                .Include(pt => pt.Post).ThenInclude(p => p.PostImages)
                .ToListAsync();

            // ImageTags の件数取得とデータ取得 (SQL)
            var imageTagsTotalCount = await _context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"ImageTags\" WHERE \"TagConceptId\" = {conceptId}")
                .SingleAsync();

            var resolvedImageSkip = imageSkip.HasValue && imageSkip.Value > 0 ? imageSkip.Value : 0;
            if (resolvedImageSkip >= imageTagsTotalCount) {
                resolvedImageSkip = Math.Max(0, imageTagsTotalCount - pageSize);
            }

            var imageTags = await _context.ImageTags
                .FromSqlInterpolated($@"
                    SELECT it.*
                    FROM ""ImageTags"" it
                    INNER JOIN ""Images"" i ON it.""ImageId"" = i.""Id""
                    WHERE it.""TagConceptId"" = {conceptId}
                    ORDER BY i.""CreatedAt"" DESC
                    LIMIT {pageSize} OFFSET {resolvedImageSkip}")
                .Include(it => it.Image)
                .ToListAsync();

            ViewBag.PostTags = postTags;
            ViewBag.PostTagsPageSize = pageSize;
            ViewBag.PostTagsTotalCount = postTagsTotalCount;
            ViewBag.PostTagsSkip = resolvedPostSkip;
            ViewBag.PostTagsHasPrevious = resolvedPostSkip > 0;
            ViewBag.PostTagsHasNext = resolvedPostSkip + postTags.Count < postTagsTotalCount;
            ViewBag.PostTagsPreviousSkip = Math.Max(0, resolvedPostSkip - pageSize);
            ViewBag.PostTagsNextSkip = resolvedPostSkip + pageSize;

            ViewBag.ImageTags = imageTags;
            ViewBag.ImageTagsPageSize = pageSize;
            ViewBag.ImageTagsTotalCount = imageTagsTotalCount;
            ViewBag.ImageTagsSkip = resolvedImageSkip;
            ViewBag.ImageTagsHasPrevious = resolvedImageSkip > 0;
            ViewBag.ImageTagsHasNext = resolvedImageSkip + imageTags.Count < imageTagsTotalCount;
            ViewBag.ImageTagsPreviousSkip = Math.Max(0, resolvedImageSkip - pageSize);
            ViewBag.ImageTagsNextSkip = resolvedImageSkip + pageSize;

            return View(concept);
        }

        // GET: /Tag/Search?q=... (AJAX オートコンプリート)
        [HttpGet]
        public async Task<IActionResult> Search(string q) {
            var results = await _tagService.SearchTagsAsync(q, 10);
            return Json(results);
        }

        // GET: /Tag/Disambiguation/5
        [Authorize]
        public async Task<IActionResult> Disambiguation(long id) {
            var concept = await _context.TagConcepts
                .FromSqlInterpolated($"SELECT * FROM \"TagConcepts\" WHERE \"Id\" = {id}")
                .Include(tc => tc.Tags)
                .Include(tc => tc.SubjectRelationships).ThenInclude(sr => sr.ObjectConcept).ThenInclude(oc => oc.Tags)
                .Include(tc => tc.ObjectRelationships).ThenInclude(or => or.SubjectConcept).ThenInclude(sc => sc.Tags)
                .FirstOrDefaultAsync();

            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canEdit = concept.UserId == currentUserId || User.IsInRole("Admin");

            var primaryTag = concept.Tags.FirstOrDefault(t => t.IsCanonical)?.TagText
                ?? concept.Tags.FirstOrDefault()?.TagText ?? $"Tag#{concept.Id}";

            var vm = new DisambiguationViewModel {
                TagConceptId = concept.Id,
                PrimaryTagText = primaryTag,
                Category = concept.Category,
                CanEdit = canEdit,
                Synonyms = concept.Tags.Where(t => t.TagText != primaryTag).Select(t => t.TagText).ToList(),

                BroaderConcepts = concept.SubjectRelationships
                    .Where(r => r.Predicate == "broader")
                    .Select(r => new RelatedConceptDto {
                        RelationshipId = r.Id,
                        ConceptId = r.ObjectConceptId,
                        Text = r.ObjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.ObjectConceptId}"
                    }).ToList(),

                NarrowerConcepts = concept.ObjectRelationships
                    .Where(r => r.Predicate == "broader")
                    .Select(r => new RelatedConceptDto {
                        RelationshipId = r.Id,
                        ConceptId = r.SubjectConceptId,
                        Text = r.SubjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.SubjectConceptId}"
                    }).ToList(),

                RelatedConcepts = concept.SubjectRelationships
                    .Where(r => r.Predicate == "related")
                    .Select(r => new RelatedConceptDto {
                        RelationshipId = r.Id,
                        ConceptId = r.ObjectConceptId,
                        Text = r.ObjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.ObjectConceptId}"
                    })
                    .Concat(concept.ObjectRelationships
                        .Where(r => r.Predicate == "related")
                        .Select(r => new RelatedConceptDto {
                            RelationshipId = r.Id,
                            ConceptId = r.SubjectConceptId,
                            Text = r.SubjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.SubjectConceptId}"
                        }))
                    .ToList()
            };

            return View(vm);
        }

        // POST: /Tag/AddSynonym
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSynonym(long tagConceptId, string synonymText) {
            var concept = await _context.TagConcepts
                .FromSqlInterpolated($"SELECT * FROM \"TagConcepts\" WHERE \"Id\" = {tagConceptId}")
                .FirstOrDefaultAsync();

            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (concept.UserId != currentUserId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(synonymText)) {
                var text = synonymText.Trim();
                var normalized = text.ToLowerInvariant();

                var existsCount = await _context.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"Tags\" WHERE \"TagConceptId\" = {tagConceptId} AND \"NormalizedText\" = {normalized}")
                    .SingleAsync();

                if (existsCount == 0) {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO ""Tags"" (""TagConceptId"", ""TagText"", ""NormalizedText"", ""LanguageCode"", ""IsCanonical"")
                        VALUES ({tagConceptId}, {text}, {normalized}, 'ja', false)");
                }
            }

            return RedirectToAction(nameof(Disambiguation), new { id = tagConceptId });
        }

        // POST: /Tag/AddRelationship
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRelationship(long tagConceptId, string predicate, string targetTagText) {
            var concept = await _context.TagConcepts
                .FromSqlInterpolated($"SELECT * FROM \"TagConcepts\" WHERE \"Id\" = {tagConceptId}")
                .FirstOrDefaultAsync();

            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (concept.UserId != currentUserId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(targetTagText)) {
                var targetConcepts = await _tagService.GetOrCreateTagConceptsAsync(new[] { targetTagText }, currentUserId);
                var targetConcept = targetConcepts.FirstOrDefault();

                if (targetConcept != null && targetConcept.Id != tagConceptId) {
                    if (predicate == "broader") {
                        var existsCount = await _context.Database
                            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"TagRelationships\" WHERE \"SubjectConceptId\" = {tagConceptId} AND \"ObjectConceptId\" = {targetConcept.Id} AND \"Predicate\" = 'broader'")
                            .SingleAsync();

                        if (existsCount == 0) {
                            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                                INSERT INTO ""TagRelationships"" (""SubjectConceptId"", ""ObjectConceptId"", ""Predicate"")
                                VALUES ({tagConceptId}, {targetConcept.Id}, 'broader')");
                        }
                    } else if (predicate == "narrower") {
                        var existsCount = await _context.Database
                            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"TagRelationships\" WHERE \"SubjectConceptId\" = {targetConcept.Id} AND \"ObjectConceptId\" = {tagConceptId} AND \"Predicate\" = 'broader'")
                            .SingleAsync();

                        if (existsCount == 0) {
                            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                                INSERT INTO ""TagRelationships"" (""SubjectConceptId"", ""ObjectConceptId"", ""Predicate"")
                                VALUES ({targetConcept.Id}, {tagConceptId}, 'broader')");
                        }
                    } else if (predicate == "related") {
                        var existsCount = await _context.Database
                            .SqlQuery<int>($@"
                                SELECT COUNT(*) AS ""Value"" FROM ""TagRelationships"" 
                                WHERE (""SubjectConceptId"" = {tagConceptId} AND ""ObjectConceptId"" = {targetConcept.Id} AND ""Predicate"" = 'related')
                                   OR (""SubjectConceptId"" = {targetConcept.Id} AND ""ObjectConceptId"" = {tagConceptId} AND ""Predicate"" = 'related')")
                            .SingleAsync();

                        if (existsCount == 0) {
                            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                                INSERT INTO ""TagRelationships"" (""SubjectConceptId"", ""ObjectConceptId"", ""Predicate"")
                                VALUES ({tagConceptId}, {targetConcept.Id}, 'related')");
                        }
                    }
                }
            }

            return RedirectToAction(nameof(Disambiguation), new { id = tagConceptId });
        }

        // POST: /Tag/RemoveRelationship
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRelationship(long relationshipId, long tagConceptId) {
            var rel = await _context.TagRelationships
                .FromSqlInterpolated($"SELECT * FROM \"TagRelationships\" WHERE \"Id\" = {relationshipId}")
                .FirstOrDefaultAsync();

            if (rel != null) {
                var concept = await _context.TagConcepts
                    .FromSqlInterpolated($"SELECT * FROM \"TagConcepts\" WHERE \"Id\" = {tagConceptId}")
                    .FirstOrDefaultAsync();

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (concept != null && (concept.UserId == currentUserId || User.IsInRole("Admin"))) {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"TagRelationships\" WHERE \"Id\" = {relationshipId}");
                }
            }
            return RedirectToAction(nameof(Disambiguation), new { id = tagConceptId });
        }
    }
}