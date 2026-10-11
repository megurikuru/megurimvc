namespace Meguri.Models {

    /// 投稿・コメント・画像に対するリアクションの種類
    public enum ReactionType {
        // --- 創作・感情・称賛系 ---
        /// ❤️ すき / いいね
        Like = 1,

        /// 😭 泣けた / エモい
        Emotional = 2,

        /// 👏 すごい / 拍手
        Bravo = 3,

        /// 💡 なるほど（納得・理解）
        Insight = 4,

        /// 🧐 ほぅ（興味・感銘・関心）
        Impressed = 5,

        /// 👀 読了 / 見たよ（足あと）
        Seen = 6 ,

        /// 🤔 考えさせられる（深考・考察）
        Thinking = 7,

        /// 😂 草 / ｗｗ（ユーモア・ギャグ）
        Lol = 8
    }

}
