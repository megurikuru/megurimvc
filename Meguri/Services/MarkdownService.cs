using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Ganss.Xss;
using Markdig;

namespace Meguri.Services {
    public class MarkdownService : IMarkdownService {
        private static readonly Regex SafeLanguageClassRegex = new(@"^language-[a-zA-Z0-9_+-]+$", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedStaticClasses = new(StringComparer.Ordinal) {
            "contains-task-list",
            "task-list-item",
            "task-list-item-checkbox",
            "footnotes",
            "footnote-ref",
            "footnote-back-ref"
        };

        private readonly MarkdownPipeline _pipeline;
        private readonly HtmlSanitizerOptions _sanitizerOptions;

        public MarkdownService() {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

            _sanitizerOptions = CreateSanitizerOptions();
        }

        public string ToSafeHtml(string? markdown) {
            if (string.IsNullOrWhiteSpace(markdown)) {
                return string.Empty;
            }

            var rawHtml = Markdown.ToHtml(markdown, _pipeline);
            var sanitizer = CreateSanitizer();
            return sanitizer.Sanitize(rawHtml);
        }

        public string ToPlainText(string? markdown) {
            if (string.IsNullOrWhiteSpace(markdown)) {
                return string.Empty;
            }

            var plain = Markdown.ToPlainText(markdown, _pipeline);
            return WhitespaceRegex.Replace(plain, " ").Trim();
        }

        private HtmlSanitizer CreateSanitizer() {
            var sanitizer = new HtmlSanitizer(_sanitizerOptions);
            sanitizer.AllowDataAttributes = false;
            sanitizer.KeepChildNodes = false;

            sanitizer.PostProcessNode += (_, args) => {
                if (args.Node is not IElement element) {
                    return;
                }

                var tagName = element.TagName.ToLowerInvariant();

                // タスクリスト用の <input type="checkbox" disabled> のみ許可し、その他の <input> は除去
                if (tagName == "input") {
                    var type = element.GetAttribute("type");
                    if (!string.Equals(type, "checkbox", StringComparison.OrdinalIgnoreCase)) {
                        args.Node.Parent?.RemoveChild(args.Node);
                        return;
                    }

                    var isChecked = element.HasAttribute("checked");
                    var className = element.GetAttribute("class");
                    var attrsToRemove = element.Attributes.Select(a => a.Name).ToList();
                    foreach (var attr in attrsToRemove) {
                        element.RemoveAttribute(attr);
                    }
                    element.SetAttribute("type", "checkbox");
                    element.SetAttribute("disabled", string.Empty);
                    if (isChecked) {
                        element.SetAttribute("checked", string.Empty);
                    }
                    if (!string.IsNullOrWhiteSpace(className)) {
                        element.SetAttribute("class", className);
                    }
                }

                // 許可されたクラスのみ残す（CSSインジェクション・UIなりすまし防止）
                var rawClass = element.GetAttribute("class");
                if (!string.IsNullOrWhiteSpace(rawClass)) {
                    var safeClasses = rawClass
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(c => AllowedStaticClasses.Contains(c)
                            || (tagName == "code" && SafeLanguageClassRegex.IsMatch(c)))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    if (safeClasses.Count > 0) {
                        element.SetAttribute("class", string.Join(" ", safeClasses));
                    } else {
                        element.RemoveAttribute("class");
                    }
                }

                // リンクの安全な属性設定
                if (tagName == "a") {
                    var href = element.GetAttribute("href");
                    if (!string.IsNullOrWhiteSpace(href) &&
                        (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) {
                        element.SetAttribute("target", "_blank");
                        element.SetAttribute("rel", "noopener noreferrer");
                    } else {
                        element.RemoveAttribute("target");
                        element.SetAttribute("rel", "noopener noreferrer");
                    }
                }

                // 画像は遅延読み込みを付与し、プロトコル相対URL（//...）を禁止
                if (tagName == "img") {
                    var src = element.GetAttribute("src");
                    if (string.IsNullOrWhiteSpace(src) || src.StartsWith("//", StringComparison.Ordinal)) {
                        args.Node.Parent?.RemoveChild(args.Node);
                        return;
                    }
                    element.SetAttribute("loading", "lazy");
                }
            };

            return sanitizer;
        }

        private static HtmlSanitizerOptions CreateSanitizerOptions() {
            return new HtmlSanitizerOptions {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "p", "br", "hr",
                    "h1", "h2", "h3", "h4", "h5", "h6",
                    "blockquote", "pre", "code",
                    "ul", "ol", "li", "dl", "dt", "dd",
                    "table", "thead", "tbody", "tfoot", "tr", "th", "td",
                    "a", "img",
                    "strong", "b", "em", "i", "del", "s", "ins", "u",
                    "sub", "sup", "mark", "kbd", "abbr", "q", "cite",
                    "span", "div", "details", "summary",
                    "input",
                    "figure", "figcaption", "small", "ruby", "rt", "rp"
                },
                AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "href", "src", "alt", "title",
                    "width", "height",
                    "colspan", "rowspan", "align",
                    "start", "type", "checked", "disabled", "open",
                    "class", "loading"
                },
                AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "http", "https", "mailto"
                },
                AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>(),
                UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "href", "src"
                }
            };
        }
    }
}
