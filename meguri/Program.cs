using Meguri.Data;
using Meguri.Models;
using Meguri.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

// ビルダーを作成
var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // E-Mail確認を必須にする
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()   // データベースにユーザー情報を保存
    .AddDefaultTokenProviders();                        // パスワードリセット、2要素認証用のトークン生成機能

// メール送信サービスの設定と登録
var smtpServerConf = builder.Configuration.GetSection("SMTPServerConf");                                    // appsettings.jsonからSMTPサーバー設定を取得
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, EmailSender>();  // Identity用メール送信サービスをDIコンテナに登録(使用時に毎回新しいインスタンスを生成)
builder.Services.Configure<SMTPServerConf>(smtpServerConf);                                                 // SMTPサーバー設定をオプションパターンで利用可能にする


// MVC機能を有効化(コントローラーとビューのサポートを追加)
builder.Services.AddControllersWithViews();

// アプリケーションのビルド(サービス登録完了後、リクエスト処理パイプラインを構築するWebApplicationインスタンスを作成)
var app = builder.Build();

// 開発時のみデータベース開発者ページを有効化する。
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
} else {
    app.UseExceptionHandler("/Home/Error");
}

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

// アプリケーションを起動し、HTTPリクエストの待ち受けを開始
app.Run();

