namespace Meguri.Services {
    /// <summary>
    /// Cloudflare R2 ストレージ設定クラス
    /// </summary>
    public class R2StorageConf {
        public string AccountId { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Cloudflare R2 S3互換エンドポイントURL
        /// </summary>
        public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
    }
}
