namespace Meguri.Constants {
    public class IdConst {
        // 最小文字数
        public const int MinLength = 15;

        // 最大文字数（StringLength用）
        public const int MaxLength = 128;

        // 必要に応じて他の Identity 設定値も定数化可能
        public const int RequiredUniqueChars = 5;
        public const int MaxFailedAccessAttempts = 5;
        public const int LockoutMinutes = 30;
    }
}
