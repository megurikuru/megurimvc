using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meguri.Services {
    /// <summary>
    /// Cloudflare R2 ストレージサービス実装
    /// </summary>
    public class R2StorageService : IR2StorageService, IDisposable {
        private readonly IAmazonS3 _s3Client;
        private readonly R2StorageConf _options;
        private readonly ILogger<R2StorageService> _logger;
        private bool _disposed;

        public R2StorageService(IOptions<R2StorageConf> options, ILogger<R2StorageService> logger) {
            _options = options.Value;
            _logger = logger;

            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            var config = new AmazonS3Config {
                ServiceURL = _options.ServiceUrl,
                AuthenticationRegion = "auto", // R2互換のために必須
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> UploadFileAsync(Stream stream, string key, string contentType = "image/webp", CancellationToken cancellationToken = default) {
            try {
                if (stream.CanSeek) {
                    stream.Position = 0;
                }

                var request = new PutObjectRequest {
                    BucketName = _options.BucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = true,
                    UseChunkEncoding = false // チャンクエンコーディングを無効化
                };

                await _s3Client.PutObjectAsync(request, cancellationToken);
                return key;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to upload file {Key} to R2 bucket {Bucket}", key, _options.BucketName);
                throw;
            }
        }

        public async Task<(Stream Stream, string ContentType)?> GetFileAsync(string key, CancellationToken cancellationToken = default) {
            try {
                var request = new GetObjectRequest {
                    BucketName = _options.BucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(request, cancellationToken);
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                var contentType = !string.IsNullOrEmpty(response.Headers.ContentType)
                    ? response.Headers.ContentType
                    : "image/webp";

                return (memoryStream, contentType);
            } catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
                _logger.LogWarning("File not found in R2: {Key}", key);
                return null;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to get file {Key} from R2 bucket {Bucket}", key, _options.BucketName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default) {
            try {
                var request = new DeleteObjectRequest {
                    BucketName = _options.BucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request, cancellationToken);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to delete file {Key} from R2 bucket {Bucket}", key, _options.BucketName);
                return false;
            }
        }

        public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(prefix)) {
                throw new ArgumentException("prefix is required.", nameof(prefix));
            }

            try {
                string? continuationToken = null;
                do {
                    var listResponse = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request {
                        BucketName = _options.BucketName,
                        Prefix = prefix,
                        ContinuationToken = continuationToken
                    }, cancellationToken);

                    var objects = listResponse.S3Objects ?? new List<S3Object>();
                    if (objects.Count > 0) {
                        await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                            BucketName = _options.BucketName,
                            Objects = objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                        }, cancellationToken);
                    }

                    continuationToken = listResponse.IsTruncated == true ? listResponse.NextContinuationToken : null;
                } while (continuationToken != null);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to delete objects with prefix {Prefix} from R2 bucket {Bucket}", prefix, _options.BucketName);
                throw;
            }
        }

        public void Dispose() {
            if (!_disposed) {
                _s3Client?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
