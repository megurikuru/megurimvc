using Meguri;
using Meguri.Data;
using Meguri.Models;
using Meguri.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;

// ビルダーを作成
var builder = WebApplication.CreateBuilder(args);

// appsettings.jsonや環境変数のURLを最優先として
// KestralのデフォルトのURL（http://localhost:5000）が使われないようにする
builder.WebHost.UseSetting(WebHostDefaults.PreferHostingUrlsKey, "true");

// DB接続文字列をappsettings.jsonから取得
var connectionString = builder.Configuration.GetConnectionString(
    "DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."
);
// DBコンテキストをサービスに登録（PostgreSQL使用）
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(connectionString)
);
// データベースエラー詳細表示機能を登録
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ASP.NET Core Identityを設定（ユーザー認証・認可システム）
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.SignIn.RequireConfirmedEmail = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;   // Passkeyテーブルを使用
    })
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();                    // パスワードリセット、2要素認証用のトークン生成機能

// 存在しないユーザーの認証Cookieを毎リクエストで無効化する(DB初期化後の古いCookie対策)
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

// メール送信サービスの設定と登録
var smtpServerConf = builder.Configuration.GetSection("SMTPServerConf");                                    // appsettings.jsonからSMTPサーバー設定を取得
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, EmailSender>();  // Identity用メール送信サービスをDIコンテナに登録(使用時に毎回新しいインスタンスを生成)
builder.Services.Configure<SMTPServerConf>(smtpServerConf);                                                 // SMTPサーバー設定をオプションパターンで利用可能にする

// タグ・シソーラスサービス
builder.Services.AddScoped<ITagService, TagService>();

// Cloudflare R2 ストレージ設定とサービス
var r2StorageConf = builder.Configuration.GetSection("R2Storage");
builder.Services.Configure<R2StorageConf>(r2StorageConf);
builder.Services.AddScoped<IR2StorageService, R2StorageService>();

// 画像検証・変換・圧縮サービス
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

// Cloudflare Turnstile 設定とサービス
var turnstileConf = builder.Configuration.GetSection("Turnstile");
builder.Services.Configure<TurnstileConf>(turnstileConf);
builder.Services.AddHttpClient<ITurnstileService, TurnstileService>();


// MVC機能を有効化(コントローラーとビューのサポートを追加)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
var sharedResManager = new System.Resources.ResourceManager("Meguri.Resources.SharedResource", typeof(SharedResource).Assembly);
builder.Services
    .AddControllersWithViews(options => {
        var p = options.ModelBindingMessageProvider;
        p.SetValueIsInvalidAccessor(x => string.Format(sharedResManager.GetString("Validation_ValueIsInvalid", CultureInfo.CurrentUICulture) ?? "'{0}' is invalid.", x));
        p.SetValueMustBeANumberAccessor(x => string.Format(sharedResManager.GetString("Validation_ValueMustBeANumber", CultureInfo.CurrentUICulture) ?? "The field {0} must be a number.", x));
        p.SetMissingBindRequiredValueAccessor(x => string.Format(sharedResManager.GetString("Validation_MissingBindRequiredValue", CultureInfo.CurrentUICulture) ?? "A value for the '{0}' parameter or property was not provided.", x));
        p.SetAttemptedValueIsInvalidAccessor((x, y) => string.Format(sharedResManager.GetString("Validation_AttemptedValueIsInvalid", CultureInfo.CurrentUICulture) ?? "The value '{0}' is not valid for {1}.", x, y));
        p.SetMissingKeyOrValueAccessor(() => sharedResManager.GetString("Validation_MissingKeyOrValue", CultureInfo.CurrentUICulture) ?? "A value is required.");
        p.SetUnknownValueIsInvalidAccessor(x => string.Format(sharedResManager.GetString("Validation_UnknownValueIsInvalid", CultureInfo.CurrentUICulture) ?? "The supplied value is invalid for {0}.", x));
        p.SetValueMustNotBeNullAccessor(x => sharedResManager.GetString("Validation_ValueMustNotBeNull", CultureInfo.CurrentUICulture) ?? "The value is invalid.");
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(x => string.Format(sharedResManager.GetString("Validation_ValueIsInvalid", CultureInfo.CurrentUICulture) ?? "'{0}' is invalid.", x));
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => sharedResManager.GetString("Validation_ValueMustNotBeNull", CultureInfo.CurrentUICulture) ?? "The value is invalid.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => string.Format(sharedResManager.GetString("Validation_ValueMustBeANumber", CultureInfo.CurrentUICulture) ?? "The field {0} must be a number.", ""));
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource));
    });



// Apacheなどのリバースプロキシ対応設定
// リバースプロキシ経由でアクセスされた際に、クライアントの実際のIPアドレスやプロトコル(HTTP/HTTPS)を
// 正しく取得できるようにする設定
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    // X-Forwarded-For: クライアントの実IPアドレスを取得
    // X-Forwarded-Proto: クライアントが使用したプロトコル(http/https)を取得
    // これらのヘッダーをASP.NET Coreが信頼して処理するように設定
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // デフォルトで信頼されているプライベートネットワークとプロキシをすべてクリア
    // これにより、明示的に指定したプロキシのみを信頼する設定にする
    // （セキュリティ上、すべてのプロキシを信頼しない場合はこの行を削除）
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    // appsettings.jsonから信頼するプロキシIPアドレスのリストを取得
    // 例: "ForwardedHeaders": { "KnownProxies": ["192.168.1.1", "10.0.0.1"] }
    var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
    if (knownProxies != null) {
        // 設定されている各プロキシIPアドレスを信頼リストに追加
        foreach (var proxy in knownProxies) {
            // IPアドレスとして正しい形式かチェック
            if (IPAddress.TryParse(proxy, out var ipAddress)) {
                // 有効なIPアドレスの場合、信頼するプロキシリストに追加
                options.KnownProxies.Add(ipAddress);
            }
        }
    }
});

// IPアドレス制限(一定時間内のアクセス回数超過で、そのIPを一定時間ブロック)
var ipRateLimitConf = builder.Configuration.GetSection("IpRateLimiting").Get<IpRateLimitConf>() ?? new IpRateLimitConf();
var ipBlockList = new IpBlockList();
if (ipRateLimitConf.Enabled) {
    var blockDuration = TimeSpan.FromMinutes(Math.Max(1, ipRateLimitConf.BlockMinutes));
    builder.Services.AddRateLimiter(options => {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions {
                // MaxRequests回目のアクセスで制限に達するため、許可数は MaxRequests - 1
                PermitLimit = Math.Max(1, ipRateLimitConf.MaxRequests - 1),
                Window = TimeSpan.FromMinutes(Math.Max(1, ipRateLimitConf.WindowMinutes)),
                QueueLimit = 0
            });
        });
        options.OnRejected = (context, _) => {
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            ipBlockList.Block(ip, blockDuration);
            if (ipBlockList.TryGetBlockedUntil(ip, out var until)) {
                var retryAfter = (int)Math.Ceiling((until - DateTimeOffset.UtcNow).TotalSeconds);
                context.HttpContext.Response.Headers.RetryAfter = Math.Max(retryAfter, 1).ToString();
            }
            return ValueTask.CompletedTask;
        };
    });
}

// アプリケーションのビルド(サービス登録完了後
var app = builder.Build();

var supportedCultures = new[] {
    new CultureInfo("ja"),
    new CultureInfo("en")
};

var localizationOptions = new RequestLocalizationOptions {
    DefaultRequestCulture = new RequestCulture("ja"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.ApplyCurrentCultureToResponseHeaders = true;

// リバースプロキシのヘッダーを処理（UseRouting()より前に配置）
app.UseForwardedHeaders();

// 開発時のみデータベース開発者ページを有効化する
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
} else {
    app.UseExceptionHandler("/Home/Error");
}

if (ipRateLimitConf.Enabled) {
    app.Use(async (context, next) => {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (ipBlockList.TryGetBlockedUntil(ip, out var until)) {
            var retryAfter = (int)Math.Ceiling((until - DateTimeOffset.UtcNow).TotalSeconds);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = Math.Max(retryAfter, 1).ToString();
            return;
        }
        await next();
    });
    app.UseRateLimiter();
}

app.UseRequestLocalization(localizationOptions);

// 静的ファイル(CSS、JavaScript、画像など)をwwwrootフォルダから配信
app.UseStaticFiles();

// ルーティングを有効化(URLをコントローラー/アクションにマッピング)
app.UseRouting();

// 認証ミドルウェア(ユーザーが誰かを確認)
app.UseAuthentication();
// 認可ミドルウェア(ユーザーがアクセス権限を持っているか確認)
app.UseAuthorization();

// MVCルーティングを設定(デフォルト: /Home/Index)
// {controller=Home}: デフォルトコントローラーはHome
// {action=Index}: デフォルトアクションはIndex
// {id?}: idパラメータは省略可能
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 設定に応じて全データ削除と初期データ登録（シード）を実行
using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Configuration.GetValue<bool>("DatabaseInitialization:DeleteAllData")) {
        await DbInitializer.DeleteAllDataAsync(dbContext);
    }

    await DbInitializer.InitializeAsync(
        dbContext,
        app.Configuration,
        scope.ServiceProvider.GetService<Meguri.Services.IR2StorageService>(),
        app.Environment.ContentRootPath);
}

// アプリケーションを起動し、HTTPリクエストの待ち受けを開始
app.Run();

