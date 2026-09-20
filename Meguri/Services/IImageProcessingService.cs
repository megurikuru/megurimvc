using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Meguri.Services {
    /// <summary>
    /// 処理済み画像データ
    /// </summary>
    public class ProcessedImageResult {
        public byte[] Data { get; set; } = System.Array.Empty<byte>();
        public string ContentType { get; set; } = "image/webp";
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSize => Data.Length;
    }

    /// <summary>
    /// 画像検証・変換・圧縮処理インターフェース
    /// </summary>
    public interface IImageProcessingService {
        /// <summary>
        /// 画像のフォーマット、サイズ、セキュリティチェックを行い、WebP形式かつ250KB以下に最適化して変換します。
        /// </summary>
        /// <param name="file">アップロードされたIFormFile</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>変換後のWebP画像データ</returns>
        Task<ProcessedImageResult> ProcessAndOptimizeImageAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// ストリームからの画像検証・変換・最適化
        /// </summary>
        /// <param name="inputStream">入力ストリーム</param>
        /// <param name="fileName">ファイル名（拡張子検証用）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>変換後のWebP画像データ</returns>
        Task<ProcessedImageResult> ProcessAndOptimizeImageAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default);
    }
}
