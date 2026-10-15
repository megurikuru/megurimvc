using System.Globalization;
using System.Resources;
using Meguri.Models;

namespace Meguri.Extensions {

    /// <summary>
    /// <see cref="ReactionType"/> に対応する絵文字や表示ラベル、説明文を取得するための拡張メソッド
    /// </summary>
    public static class ReactionTypeExtensions {
        private static readonly ResourceManager ResourceManager = new("Meguri.Resources.SharedResource", typeof(SharedResource).Assembly);

        private static string GetResourceString(string key) =>
            ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;

        /// <summary>
        /// リアクションに対応する絵文字を取得します。
        /// </summary>
        public static string GetEmoji(this ReactionType type) => type switch {
            ReactionType.Like => "❤️",
            ReactionType.Emotional => "😭",
            ReactionType.Bravo => "👏",
            ReactionType.Insight => "😊",
            ReactionType.Impressed => "🧐",
            ReactionType.Seen => "🙇",
            ReactionType.Lol => "😂",
            ReactionType.Thinking => "🙆",
            _ => string.Empty
        };

        /// <summary>
        /// リアクションに対応する短縮表示ラベルを取得します。
        /// </summary>
        public static string GetLabel(this ReactionType type) => type switch {
            ReactionType.Like => GetResourceString("Reaction_Like_Label"),
            ReactionType.Emotional => GetResourceString("Reaction_Emotional_Label"),
            ReactionType.Bravo => GetResourceString("Reaction_Bravo_Label"),
            ReactionType.Insight => GetResourceString("Reaction_Insight_Label"),
            ReactionType.Impressed => GetResourceString("Reaction_Impressed_Label"),
            ReactionType.Seen => GetResourceString("Reaction_Seen_Label"),
            ReactionType.Thinking => GetResourceString("Reaction_Thinking_Label"),
            ReactionType.Lol => GetResourceString("Reaction_Lol_Label"),
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
            ReactionType.Like => GetResourceString("Reaction_Like_Desc"),
            ReactionType.Emotional => GetResourceString("Reaction_Emotional_Desc"),
            ReactionType.Bravo => GetResourceString("Reaction_Bravo_Desc"),
            ReactionType.Insight => GetResourceString("Reaction_Insight_Desc"),
            ReactionType.Impressed => GetResourceString("Reaction_Impressed_Desc"),
            ReactionType.Seen => GetResourceString("Reaction_Seen_Desc"),
            ReactionType.Thinking => GetResourceString("Reaction_Thinking_Desc"),
            ReactionType.Lol => GetResourceString("Reaction_Lol_Desc"),
            _ => string.Empty
        };
    }

}
