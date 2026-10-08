using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Meguri.Services {
    /// <summary>
    /// 画像検証・セキュリティ検査・WebP変換・250KB最適化サービス
    /// </summary>
    public class ImageProcessingService : IImageProcessingService {
        private readonly ILogger<ImageProcessingService> _logger;

        // 最大受け取りサイズ: 5MB
        private const long MaxInputSizeBytes = 5 * 1024 * 1024;
        // 目標最大出力サイズ: 250KB (256,000 bytes)
        private const long TargetMaxOutputSizeBytes = 250 * 1024;
        // 最大許可解像度（ポスターやイラスト等の超高解像度データも受け入れられるよう拡張）
        private const int MaxAllowedDimension = 32768; // ← 8192 から 32768 に変更
        // 初期最大リサイズ長
        private const int InitialMaxDimension = 2048;

        // Magic bytes
        private static readonly byte[] JpegHeader = new byte[] { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public ImageProcessingService(ILogger<ImageProcessingService> logger) {
            _logger = logger;
        }

        public async Task<ProcessedImageResult> ProcessAndOptimizeImageAsync(IFormFile file, CancellationToken cancellationToken = default) {
            if (file == null || file.Length == 0) {
                throw new ArgumentException("画像ファイルが空です。");
            }

            if (file.Length > MaxInputSizeBytes) {
                throw new InvalidOperationException($"画像サイズが上限の5MBを超えています (現在: {file.Length / 1024.0 / 1024.0:F2}MB)。");
            }

            using var stream = file.OpenReadStream();
            return await ProcessAndOptimizeImageAsync(stream, file.FileName, cancellationToken);
        }

        public async Task<ProcessedImageResult> ProcessAndOptimizeImageAsync(Stream inputStream, string fileName, CancellationToken cancellationToken = default) {
            if (inputStream == null) {
                throw new ArgumentNullException(nameof(inputStream));
            }

            // メモリストリームに読み込み
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream, cancellationToken);
            var rawBytes = memoryStream.ToArray();

            if (rawBytes.Length == 0) {
                throw new InvalidOperationException("画像データが空です。");
            }

            if (rawBytes.Length > MaxInputSizeBytes) {
                throw new InvalidOperationException($"画像サイズが上限の5MBを超えています (現在: {rawBytes.Length / 1024.0 / 1024.0:F2}MB)。");
            }

            // 1. ファイル拡張子・MIMEチェック
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png") {
                throw new InvalidOperationException("画像の形式はJPEGまたはPNGのみ対応しています。");
            }

            // 2. セキュリティチェック: Magic bytes検証 (ファイルシグネチャ)
            if (!IsValidImageSignature(rawBytes)) {
                _logger.LogWarning("Invalid magic bytes for file: {FileName}", fileName);
                throw new InvalidOperationException("無効な画像ファイル形式です。JPEGまたはPNGファイルのみアップロード可能です。");
            }

            // 3. 画像のデコード & セキュリティ検証 (悪意のあるヘッダー/破損検出)
            SKBitmap? image = SKBitmap.Decode(rawBytes);
            if (image == null) {
                throw new InvalidOperationException("画像を読み込めませんでした。");
            }
            try {

            if (image.Width <= 0 || image.Height <= 0 || image.Width > MaxAllowedDimension || image.Height > MaxAllowedDimension) {
                _logger.LogWarning("Image dimension out of allowed bounds: {Width}x{Height}", image.Width, image.Height);
                throw new InvalidOperationException("画像の解像度が制限範囲外です。");
            }

            // 4. 初回リサイズ (巨大画像の場合は最大辺を2048pxに抑える)
            if (image.Width > InitialMaxDimension || image.Height > InitialMaxDimension) {
                double scale = Math.Min((double)InitialMaxDimension / image.Width, (double)InitialMaxDimension / image.Height);
                var resized = ResizeBitmap(image, Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
                image.Dispose();
                image = resized;
            }

            // 5. WebP変換および250KB以下への画質/解像度調整
            var (optimizedWebpBytes, finalBitmap) = OptimizeToWebp(image, cancellationToken);
            image = finalBitmap;

            return new ProcessedImageResult {
                Data = optimizedWebpBytes,
                ContentType = "image/webp",
                Width = image.Width,
                Height = image.Height
            };
            } finally {
                image.Dispose();
            }
        }

        private static SKBitmap ResizeBitmap(SKBitmap source, int width, int height) {
            var resized = source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
            return resized ?? throw new InvalidOperationException("画像のリサイズに失敗しました。");
        }

        private static byte[] EncodeWebp(SKBitmap bitmap, int quality) {
            using var skImage = SKImage.FromBitmap(bitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Webp, quality);
            if (data == null) {
                throw new InvalidOperationException("WebPへの変換に失敗しました。");
            }
            return data.ToArray();
        }

        private static bool IsValidImageSignature(byte[] bytes) {
            if (bytes.Length < 8) return false;

            // JPEGチェック (FF D8 FF)
            if (bytes[0] == JpegHeader[0] && bytes[1] == JpegHeader[1] && bytes[2] == JpegHeader[2]) {
                return true;
            }

            // PNGチェック (89 50 4E 47 0D 0A 1A 0A)
            bool isPng = true;
            for (int i = 0; i < PngHeader.Length; i++) {
                if (bytes[i] != PngHeader[i]) {
                    isPng = false;
                    break;
                }
            }
            if (isPng) {
                return true;
            }

            return false;
        }

        private (byte[] Data, SKBitmap Bitmap) OptimizeToWebp(SKBitmap image, CancellationToken cancellationToken) {
            // 画質を段階的に調整し、250KB以下に収める
            int[] qualityLevels = new[] { 85, 75, 65, 55, 45, 35, 25 };

            byte[] lastResult = Array.Empty<byte>();

            foreach (var quality in qualityLevels) {
                cancellationToken.ThrowIfCancellationRequested();
                lastResult = EncodeWebp(image, quality);

                if (lastResult.Length <= TargetMaxOutputSizeBytes) {
                    return (lastResult, image);
                }
            }

            // 品質25でも超える場合は段階的に解像度を縮小
            while (lastResult.Length > TargetMaxOutputSizeBytes && (image.Width > 320 && image.Height > 320)) {
                int newWidth = (int)(image.Width * 0.8);
                int newHeight = (int)(image.Height * 0.8);

                cancellationToken.ThrowIfCancellationRequested();
                var resized = ResizeBitmap(image, newWidth, newHeight);
                image.Dispose();
                image = resized;

                lastResult = EncodeWebp(image, 40);

                if (lastResult.Length <= TargetMaxOutputSizeBytes) {
                    return (lastResult, image);
                }
            }

            return (lastResult, image);
        }
    }
}
