using Meguri.Models;

namespace Meguri.ViewModels.Shared {
    /// <summary>投稿一覧アイテム表示（Shared/_PostItem）用のモデル</summary>
    public class PostItemViewModel {
        /// <summary>対象の投稿データ</summary>
        public required Meguri.Models.Post Post { get; init; }

        /// <summary>Finder形式（フォルダ・ファイル一覧形式）で表示するか</summary>
        public bool IsFinderMode { get; init; }

        /// <summary>本文プレビューの最大文字数（0以下の場合は本文非表示）</summary>
        public int BodyMaxLength { get; init; } = 120;

        /// <summary>表示する最大画像枚数（0以下の場合は画像非表示）</summary>
        public int MaxImages { get; init; } = 4;

        /// <summary>画像クリック時に画像ファイルを別タブで開くか（falseの場合は投稿詳細へ遷移）</summary>
        public bool LinkImagesToFile { get; init; }

        /// <summary>界隈名を表示するか</summary>
        public bool ShowFandom { get; init; } = true;

        /// <summary>界隈名を界隈詳細へのリンクにするか</summary>
        public bool LinkFandom { get; init; } = true;

        /// <summary>固定・R-18・R-18Gバッジを表示するか</summary>
        public bool ShowBadges { get; init; }

        /// <summary>投稿者名を表示するか</summary>
        public bool ShowAuthor { get; init; } = true;

        /// <summary>投稿日時を表示するか</summary>
        public bool ShowCreatedAt { get; init; } = true;

        /// <summary>投稿日時を日付のみ（true）で表示するか</summary>
        public bool DateOnly { get; init; }

        /// <summary>タグ一覧を表示するか</summary>
        public bool ShowTags { get; init; }

        /// <summary>コメント数・閲覧数・リアクション数を表示するか</summary>
        public bool ShowStats { get; init; } = true;
    }
}
