namespace Meguri.Models {

    /// 投稿・コメント・画像に対するリアクションの種類
    public enum ReactionType {
        // --- 創作・感情・称賛系 ---
        /// ❤️ すき / いいね
        Like = 1,

        /// 🙏 尊い / 神
        Precious = 2,

        /// 💘 刺さった
        Heartstruck = 3,

        /// 😭 泣けた / エモい
        Emotional = 4,

        /// 📖 続き待機！
        WantMore = 5,

        /// 👏 すごい / 拍手
        Bravo = 6,

        // --- 知的・考察・あいまい・ネタ系 ---
        /// 💡 なるほど（納得・理解）
        Insight = 10,

        /// 🧐 ほぅ（興味・感銘・関心）
        Impressed = 11,

        /// 🌱 へぇ / 知見（新しい発見）
        Learn = 12,

        /// 👀 読了 / 見たよ（足あと）
        Seen = 13,

        /// 🤔 考えさせられる（深考・考察）
        Thinking = 14,

        /// 😂 草 / ｗｗ（ユーモア・ギャグ）
        Lol = 15
    }

}
