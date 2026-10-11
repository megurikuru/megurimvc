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
    /// ブロック中のIPアドレスを保持する
    /// </summary>
    public class IpBlockList {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _blocked = new();

        public void Block(string ip, TimeSpan duration) {
            _blocked.TryAdd(ip, DateTimeOffset.UtcNow.Add(duration));
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
