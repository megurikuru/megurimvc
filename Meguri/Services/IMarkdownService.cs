namespace Meguri.Services {
    public interface IMarkdownService {
        /// <summary>
        /// GitHub互換MarkdownをHTMLに変換し、XSS対策のサニタイズ（無害化処理）を行った安全なHTMLを返します。
        /// </summary>
        string ToSafeHtml(string? markdown);

        /// <summary>
        /// Markdownから装飾記号を取り除いたプレーンテキストを返します（一覧プレビュー用）。
        /// </summary>
        string ToPlainText(string? markdown);
    }
}
