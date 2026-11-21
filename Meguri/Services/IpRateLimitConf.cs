using System;
using System.Collections.Concurrent;

namespace Meguri.Services {
    /// <summary>
    /// IPアドレス制限の設定(appsettings.jsonの IpRateLimiting セクション)
    /// </summary>
    public class IpRateLimitConf {
        /// <summary>制限を有効にするか</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>集計する時間(分)</summary>
        public int WindowMinutes { get; set; } = 1;
        /// <summary>この回数以上のアクセスがあった場合にブロックする</summary>
        public int MaxRequests { get; set; } = 20;
        /// <summary>ブロックする時間(分)</summary>
        public int BlockMinutes { get; set; } = 60;
    }

    /// <summary>
    /// パスワードログイン試行のレート制限設定(appsettings.jsonの PasswordLoginRateLimiting セクション)
    /// </summary>
    public class PasswordLoginRateLimitConf {
        public const string PolicyName = "PasswordLogin";

        /// <summary>制限を有効にするか</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>集計する時間(分)</summary>
        public int WindowMinutes { get; set; } = 5;
        /// <summary>集計時間内に許可する最大試行回数</summary>
        public int MaxAttempts { get; set; } = 10;
    }

    /// <summary>
    /// パスキー認証・登録試行のレート制限設定(appsettings.jsonの PasskeyRateLimiting セクション)
    /// </summary>
    public class PasskeyRateLimitConf {
        public const string PolicyName = "PasskeyAuth";

        /// <summary>制限を有効にするか</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>集計する時間(分)</summary>
        public int WindowMinutes { get; set; } = 5;
        /// <summary>集計時間内に許可する最大リクエスト回数（1回の操作でOptions取得と送信の2リクエストが発生）</summary>
        public int MaxAttempts { get; set; } = 20;
    }

    /// <summary>
    /// ブロック中のIPアドレスを保持する
    /// </summary>
    public class IpBlockList {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _blocked = new();

        public void Block(string ip, TimeSpan duration) {
            var newUntil = DateTimeOffset.UtcNow.Add(duration);
            _blocked.AddOrUpdate(ip, newUntil, (_, existingUntil) =>
                existingUntil > DateTimeOffset.UtcNow ? existingUntil : newUntil);
        }

        public bool TryGetBlockedUntil(string ip, out DateTimeOffset until) {
            if (_blocked.TryGetValue(ip, out until)) {
                if (until > DateTimeOffset.UtcNow) {
                    return true;
                }
                _blocked.TryRemove(ip, out _);
            }
            return false;
        }
    }
}
