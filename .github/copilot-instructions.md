# Copilot Instructions

## プロジェクト ガイドライン
- 今回のDbInitializer変更でいう初期化はシード登録ではなく、全データ削除処理の追加とappsettings.jsonによる実行制御を意味する。

## CSS Guidelines
- CSSでカラーコードを指定する際は直接記述せず、Meguri/wwwroot/css/meguri/base.css の :root にCSS変数として定義して使用する。
- CSSで上下の余白（margin-top / margin-bottom）を指定する際は、直接マージンを指定するのではなく、必要であればラッパー要素を追加して Flexbox または Grid の gap を使用する。