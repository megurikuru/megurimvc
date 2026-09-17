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
        public async Task<IActionResult> Index() {
            var concepts = await _context.TagConcepts
                .Include(tc => tc.Tags)
                .Include(tc => tc.PostTags)
                .Include(tc => tc.ImageTags)
                .Include(tc => tc.User)
                .OrderByDescending(tc => tc.PostTags.Count + tc.ImageTags.Count)
                .Take(100)
                .ToListAsync();

            return View(concepts);
        }

        // GET: /Tag/Details/5
        public async Task<IActionResult> Details(long conceptId) {
            var concept = await _context.TagConcepts
                .Include(tc => tc.Tags)
                .Include(tc => tc.User)
                .Include(tc => tc.PostTags).ThenInclude(pt => pt.Post).ThenInclude(p => p.User)
                .Include(tc => tc.PostTags).ThenInclude(pt => pt.Post).ThenInclude(p => p.PostImages)
                .Include(tc => tc.ImageTags).ThenInclude(it => it.Image)
                .Include(tc => tc.SubjectRelationships).ThenInclude(sr => sr.ObjectConcept).ThenInclude(oc => oc.Tags)
                .Include(tc => tc.ObjectRelationships).ThenInclude(or => or.SubjectConcept).ThenInclude(sc => sc.Tags)
                .FirstOrDefaultAsync(tc => tc.Id == conceptId);

            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanEdit = concept.UserId == currentUserId || User.IsInRole("Admin");

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
                .Include(tc => tc.Tags)
                .Include(tc => tc.SubjectRelationships).ThenInclude(sr => sr.ObjectConcept).ThenInclude(oc => oc.Tags)
                .Include(tc => tc.ObjectRelationships).ThenInclude(or => or.SubjectConcept).ThenInclude(sc => sc.Tags)
                .FirstOrDefaultAsync(tc => tc.Id == id);

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

                // 上位語: Subject = this, Predicate = "broader"
                BroaderConcepts = concept.SubjectRelationships
                    .Where(r => r.Predicate == "broader")
                    .Select(r => new RelatedConceptDto {
                        RelationshipId = r.Id,
                        ConceptId = r.ObjectConceptId,
                        Text = r.ObjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.ObjectConceptId}"
                    }).ToList(),

                // 下位語: Object = this, Predicate = "broader"
                NarrowerConcepts = concept.ObjectRelationships
                    .Where(r => r.Predicate == "broader")
                    .Select(r => new RelatedConceptDto {
                        RelationshipId = r.Id,
                        ConceptId = r.SubjectConceptId,
                        Text = r.SubjectConcept.Tags.FirstOrDefault()?.TagText ?? $"Concept#{r.SubjectConceptId}"
                    }).ToList(),

                // 関連語: Subject = this or Object = this, Predicate = "related"
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
            var concept = await _context.TagConcepts.FindAsync(tagConceptId);
            if (concept == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (concept.UserId != currentUserId && !User.IsInRole("Admin")) {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(synonymText)) {
                var text = synonymText.Trim();
                var normalized = text.ToLowerInvariant();

                var exists = await _context.Tags.AnyAsync(t => t.TagConceptId == tagConceptId && t.NormalizedText == normalized);
                if (!exists) {
                    _context.Tags.Add(new Tag {
                        TagConceptId = tagConceptId,
                        TagText = text,
                        NormalizedText = normalized,
                        LanguageCode = "ja",
                        IsCanonical = false
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Disambiguation), new { id = tagConceptId });
        }

        // POST: /Tag/AddRelationship
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRelationship(long tagConceptId, string predicate, string targetTagText) {
            var concept = await _context.TagConcepts.FindAsync(tagConceptId);
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
                        // tagConceptId の上位語が targetConceptId
                        var exists = await _context.TagRelationships.AnyAsync(r => r.SubjectConceptId == tagConceptId && r.ObjectConceptId == targetConcept.Id && r.Predicate == "broader");
                        if (!exists) {
                            _context.TagRelationships.Add(new TagRelationship {
                                SubjectConceptId = tagConceptId,
                                ObjectConceptId = targetConcept.Id,
                                Predicate = "broader"
                            });
                            await _context.SaveChangesAsync();
                        }
                    } else if (predicate == "narrower") {
                        // tagConceptId の下位語が targetConceptId (targetConcept の上位語が tagConceptId)
                        var exists = await _context.TagRelationships.AnyAsync(r => r.SubjectConceptId == targetConcept.Id && r.ObjectConceptId == tagConceptId && r.Predicate == "broader");
                        if (!exists) {
                            _context.TagRelationships.Add(new TagRelationship {
                                SubjectConceptId = targetConcept.Id,
                                ObjectConceptId = tagConceptId,
                                Predicate = "broader"
                            });
                            await _context.SaveChangesAsync();
                        }
                    } else if (predicate == "related") {
                        // 関連語
                        var exists = await _context.TagRelationships.AnyAsync(r =>
                            (r.SubjectConceptId == tagConceptId && r.ObjectConceptId == targetConcept.Id && r.Predicate == "related") ||
                            (r.SubjectConceptId == targetConcept.Id && r.ObjectConceptId == tagConceptId && r.Predicate == "related"));
                        if (!exists) {
                            _context.TagRelationships.Add(new TagRelationship {
                                SubjectConceptId = tagConceptId,
                                ObjectConceptId = targetConcept.Id,
                                Predicate = "related"
                            });
                            await _context.SaveChangesAsync();
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
            var rel = await _context.TagRelationships.FindAsync(relationshipId);
            if (rel != null) {
                var concept = await _context.TagConcepts.FindAsync(tagConceptId);
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (concept != null && (concept.UserId == currentUserId || User.IsInRole("Admin"))) {
                    _context.TagRelationships.Remove(rel);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Disambiguation), new { id = tagConceptId });
        }
    }
}
