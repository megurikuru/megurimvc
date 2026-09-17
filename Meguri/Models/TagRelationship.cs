// 概念同士の階層関係（上位語・関連語）を表すクラス
namespace Meguri.Models {
    public class TagRelationship {
        public long Id { get; set; }
        public long SubjectConceptId { get; set; }
        public string Predicate { get; set; } = string.Empty; // "broader", "related"
        public long ObjectConceptId { get; set; }

        // ナビゲーションプロパティ
        public virtual TagConcept SubjectConcept { get; set; } = null!;
        public virtual TagConcept ObjectConcept { get; set; } = null!;
    }
}
