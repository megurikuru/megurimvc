using System;
using System.Net;
using Microsoft.AspNetCore.Html;

namespace Meguri.Helpers {
    public static class LocalTimeExtensions {
        /// <summary>
        /// UTC日時を &lt;time&gt; 要素として出力する。ブラウザ側のJSがローカルのタイムゾーン/ロケールに変換する。
        /// </summary>
        public static IHtmlContent ToLocalTime(this DateTime value, bool dateOnly = false) {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            var iso = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var fallback = utc.ToString(dateOnly ? "yyyy/MM/dd" : "yyyy/MM/dd HH:mm") + " UTC";
            var format = dateOnly ? "date" : "datetime";
            return new HtmlString($"<time class=\"local-time\" datetime=\"{iso}\" data-format=\"{format}\">{WebUtility.HtmlEncode(fallback)}</time>");
        }
    }
}
