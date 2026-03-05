# Auto Clicker (Safe)

Windows用オートクリッカー（.NET 8 / WPF）

## 機能

- **単一点モード**: 指定座標を繰り返しクリック
- **複数点モード**: 複数座標を順番にクリック（周回）
- **クリック種別**: 左 / 右 / 中クリック
- **間隔設定**: 1〜600,000 ms
- **回数設定**: 無限 / 指定回数
- **座標取得**: 即時取得 / 3秒後に取得
- **グローバルホットキー**: 開始/停止 (F6)、停止 (F7)（変更可能）
- **設定保存/復元**: JSON形式で自動保存
- **タスクトレイ常駐**: ウィンドウを閉じてもバックグラウンド動作

## 必要環境

- Windows 10 / 11
- .NET 8.0 SDK

## ビルド

```bash
cd mouse_clicker
dotnet build AutoClicker.sln
```

## 単体 exe ファイルの作成（配布用）

.NET ランタイムを同梱した単一 exe を生成するには:

```bash
cd mouse_clicker
dotnet publish src/AutoClicker/AutoClicker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

出力先:

```
src/AutoClicker/bin/Release/net8.0-windows/win-x64/publish/AutoClicker.exe
```

この exe は .NET SDK/ランタイム未インストールの PC でもそのまま実行できます。

> **オプション**: ファイルサイズを小さくしたい場合は `-p:PublishTrimmed=true` を追加してください（約60〜80MB → 約20〜30MB）。
> ただしリフレクションに依存する処理がある場合はトリミングで動作が変わる可能性があるため、トリム後は必ず動作確認してください。

### ランタイム依存（軽量）版

配布先に .NET 8 ランタイムがインストール済みの場合は `--self-contained false` で軽量な exe を生成できます:

```bash
dotnet publish src/AutoClicker/AutoClicker.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

この場合の exe サイズは数MB程度です。

## 実行

```bash
dotnet run --project src/AutoClicker/AutoClicker.csproj
```

または、ビルド後に直接実行:

```bash
src/AutoClicker/bin/Debug/net8.0-windows/AutoClicker.exe
```

## 注意事項

- 間隔が極端に短い場合（例: 1ms）はOS負荷が高くなります
- マルチモニタ環境では座標が負になる場合があります
- クリック送出は専用ワーカースレッドで実行され、デッドロックを防止しています

## アーキテクチャ

`ARCHITECTURE.md` に従い、以下の安全設計を採用:

1. **専用ワーカー集約**: `SendInput` は `ClickEngine` 内の単一Taskでのみ実行
2. **多重起動禁止**: `SemaphoreSlim(1,1)` でガード
3. **busy-wait禁止**: `Task.Delay` + `CancellationToken` による即時停止対応
4. **ステートマシン**: Stopped → Running → Stopping → Stopped

## プロジェクト構成

```
src/AutoClicker/
  Native/          P/Invoke ラッパー (SendInput, GetCursorPos)
  Models/          データモデル (ConfigSnapshot, ClickPoint, AppSettings)
  Core/            ビジネスロジック (ClickEngine, HotkeyManager, SettingsStore)
  ViewModels/      MVVM ViewModel (MainViewModel, RelayCommand)
  Converters/      WPF値コンバーター
  MainWindow.xaml  メインUI
  App.xaml         アプリケーション定義
```
