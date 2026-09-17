using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Meguri.Models;

namespace Meguri.Models.TagViewModels {
    public class DisambiguationViewModel {
        public long TagConceptId { get; set; }
        public string PrimaryTagText { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
        public bool CanEdit { get; set; }

        // 同義語・表記揺れ（同一概念内の別名タグ）
        public List<string> Synonyms { get; set; } = new List<string>();

        // 上位語 (Broader)
        public List<RelatedConceptDto> BroaderConcepts { get; set; } = new List<RelatedConceptDto>();

        // 下位語 (Narrower)
        public List<RelatedConceptDto> NarrowerConcepts { get; set; } = new List<RelatedConceptDto>();

        // 関連語 (Related)
        public List<RelatedConceptDto> RelatedConcepts { get; set; } = new List<RelatedConceptDto>();

        // 関連付け追加用
        public string? NewSynonymText { get; set; }
        public string? NewBroaderText { get; set; }
        public string? NewNarrowerText { get; set; }
        public string? NewRelatedText { get; set; }
    }

    public class RelatedConceptDto {
        public long RelationshipId { get; set; }
        public long ConceptId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
