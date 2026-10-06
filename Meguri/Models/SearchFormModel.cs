namespace Meguri.Models {
    /// <summary>共通検索フォーム（Shared/_SearchForm）用のモデル</summary>
    public class SearchFormModel {
        /// <summary>送信先アクション名</summary>
        public string Action { get; set; } = "Index";

        /// <summary>送信先コントローラー名</summary>
        public string Controller { get; set; } = "Home";

        /// <summary>検索キーワードのクエリパラメーター名</summary>
        public string ParameterName { get; set; } = "search";

        /// <summary>現在の検索キーワード</summary>
        public string? Value { get; set; }

        /// <summary>プレースホルダーのリソースキー</summary>
        public string PlaceholderKey { get; set; } = "Home_SearchPlaceholder";

        /// <summary>「見つかりません」を表示するか</summary>
        public bool NothingFound { get; set; }

        /// <summary>「見つかりません」のリソースキー</summary>
        public string NothingFoundKey { get; set; } = "Common_NothingFound";

        /// <summary>外側divのid</summary>
        public string ContainerId { get; set; } = "search-box";

        /// <summary>formのCSSクラス</summary>
        public string FormClass { get; set; } = "search";
    }
}
