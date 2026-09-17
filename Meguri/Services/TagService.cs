using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;

namespace Meguri.Services {
    public interface ITagService {
        Task<List<TagConcept>> GetOrCreateTagConceptsAsync(IEnumerable<string> tagTexts, string? userId, string languageCode = "ja");
        Task<List<TagSearchDto>> SearchTagsAsync(string query, int maxResults = 10);
    }

    public class TagSearchDto {
        public long ConceptId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class TagService : ITagService {
        private readonly ApplicationDbContext _context;

        public TagService(ApplicationDbContext context) {
            _context = context;
        }

        public async Task<List<TagConcept>> GetOrCreateTagConceptsAsync(IEnumerable<string> tagTexts, string? userId, string languageCode = "ja") {
            var distinctTags = tagTexts
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            var result = new List<TagConcept>();

            foreach (var text in distinctTags) {
                var normalized = text.ToLowerInvariant();

                // 既存のタグを検索
                var existingTag = await _context.Tags
                    .Include(t => t.TagConcept)
                    .FirstOrDefaultAsync(t => t.NormalizedText == normalized);

                if (existingTag != null) {
                    result.Add(existingTag.TagConcept);
                } else {
                    // 新規シソーラス概念とタグを作成
                    var concept = new TagConcept {
                        Category = "general",
                        UserId = userId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _context.TagConcepts.Add(concept);
                    await _context.SaveChangesAsync();

                    var tag = new Tag {
                        TagConceptId = concept.Id,
                        TagText = text,
                        NormalizedText = normalized,
                        LanguageCode = languageCode,
                        IsCanonical = true
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();

                    result.Add(concept);
                }
            }

            return result;
        }

        public async Task<List<TagSearchDto>> SearchTagsAsync(string query, int maxResults = 10) {
            if (string.IsNullOrWhiteSpace(query)) {
                return new List<TagSearchDto>();
            }

            var normalized = query.Trim().ToLowerInvariant();

            var tags = await _context.Tags
                .Include(t => t.TagConcept)
                .Where(t => t.NormalizedText.Contains(normalized) || t.TagText.Contains(query.Trim()))
                .Take(maxResults)
                .Select(t => new TagSearchDto {
                    ConceptId = t.TagConceptId,
                    Text = t.TagText,
                    Category = t.TagConcept.Category
                })
                .ToListAsync();

            return tags;
        }
    }
}
