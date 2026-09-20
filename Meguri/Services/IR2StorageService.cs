using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Meguri.Services {
    /// <summary>
    /// Cloudflare R2 ストレージ操作用インターフェース
    /// </summary>
    public interface IR2StorageService {
        /// <summary>
        /// R2バケットにファイルをアップロード
        /// </summary>
        /// <param name="stream">ファイルデータストリーム</param>
        /// <param name="key">保存先キー (例: {userId}/{filename}.webp)</param>
        /// <param name="contentType">MIMEタイプ (例: image/webp)</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>保存されたキー</returns>
        Task<string> UploadFileAsync(Stream stream, string key, string contentType = "image/webp", CancellationToken cancellationToken = default);

        /// <summary>
        /// R2バケットからファイルデータを取得
        /// </summary>
        /// <param name="key">ファイルキー</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>ストリームとContentType、存在しない場合はnull</returns>
        Task<(Stream Stream, string ContentType)?> GetFileAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// R2バケットからファイルを削除
        /// </summary>
        /// <param name="key">ファイルキー</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>成功したかどうか</returns>
        Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);
    }
}
