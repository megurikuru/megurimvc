using Meguri.Models;

namespace Meguri.Extensions {

    /// <summary>
    /// <see cref="ReactionType"/> に対応する絵文字や表示ラベル、説明文を取得するための拡張メソッド
    /// </summary>
    public static class ReactionTypeExtensions {

        /// <summary>
        /// リアクションに対応する絵文字を取得します。
        /// </summary>
        public static string GetEmoji(this ReactionType type) => type switch {
            ReactionType.Like => "❤️",
            ReactionType.Precious => "🙏",
            ReactionType.Heartstruck => "💘",
            ReactionType.Emotional => "😭",
            ReactionType.WantMore => "📖",
            ReactionType.Bravo => "👏",
            ReactionType.Insight => "💡",
            ReactionType.Impressed => "🧐",
            ReactionType.Learn => "🌱",
            ReactionType.Seen => "👀",
            ReactionType.Thinking => "🤔",
            ReactionType.Lol => "😂",
            _ => "👍"
        };

        /// <summary>
        /// リアクションに対応する短縮表示ラベルを取得します。
        /// </summary>
        public static string GetLabel(this ReactionType type) => type switch {
            ReactionType.Like => "すき",
            ReactionType.Precious => "尊い",
            ReactionType.Heartstruck => "刺さった",
            ReactionType.Emotional => "エモい",
            ReactionType.WantMore => "続き待機！",
            ReactionType.Bravo => "すごい",
            ReactionType.Insight => "なるほど",
            ReactionType.Impressed => "ほぅ",
            ReactionType.Learn => "へぇ",
            ReactionType.Seen => "見たよ",
            ReactionType.Thinking => "考えさせられる",
            ReactionType.Lol => "草",
            _ => string.Empty
        };

        /// <summary>
        /// 絵文字とラベルを結合した表示文字列（例: "❤️ すき"）を取得します。
        /// </summary>
        public static string GetDisplayName(this ReactionType type) {
            var label = type.GetLabel();
            return string.IsNullOrEmpty(label) ? type.GetEmoji() : $"{type.GetEmoji()} {label}";
        }

        /// <summary>
        /// リアクションの意味や説明文（ツールチップ等用）を取得します。
        /// </summary>
        public static string GetDescription(this ReactionType type) => type switch {
            ReactionType.Like => "すき・いいね",
            ReactionType.Precious => "尊い・神",
            ReactionType.Heartstruck => "刺さった・ツボに入った",
            ReactionType.Emotional => "泣けた・エモい・感動した",
            ReactionType.WantMore => "続き待機！・更新応援",
            ReactionType.Bravo => "すごい・拍手・素晴らしい完成度",
            ReactionType.Insight => "なるほど・納得・理解",
            ReactionType.Impressed => "ほぅ・興味深い・感銘",
            ReactionType.Learn => "へぇ・知見・新しい発見",
            ReactionType.Seen => "読了・見たよ・足あと",
            ReactionType.Thinking => "考えさせられる・深いテーマ",
            ReactionType.Lol => "草・面白い・ｗｗ",
            _ => string.Empty
        };
    }

}
