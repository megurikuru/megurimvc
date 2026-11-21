namespace Meguri.Constants {
    public class IdConst {
        // パスワード最小文字数（NIST SP 800-63B 推奨基準）
        public const int MinLength = 15;

        // パスワード最大文字数（パスフレーズ対応・StringLength用）
        public const int MaxLength = 128;

        // パスワードに含める必要がある異なる文字の種類数（文字種の構成規則を課さない場合は1）
        public const int RequiredUniqueChars = 1;
        public const int MaxFailedAccessAttempts = 5;
        public const int LockoutMinutes = 15;
    }
}
