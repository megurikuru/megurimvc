using System;

namespace Meguri.Services {
    /// <summary>
    /// Cloudflare Turnstile 設定クラス
    /// </summary>
    public class TurnstileConf {
        public bool Enabled { get; set; } = true;
        public string WidgetName { get; set; } = string.Empty;
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string[] Hostnames { get; set; } = Array.Empty<string>();
    }
}
