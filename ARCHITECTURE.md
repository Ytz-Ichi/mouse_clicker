# ARCHITECTURE

本書は **Windows 10で発生しうるマウス入力デッドロック**を避けるための設計規約を定義する。
UI実装よりも優先される（ただしUIの見た目/文言は `UI_SPEC.md` と `ui_contract.json` に従う）。

---

## 背景: Windows 10のデッドロック注意
- Windows 10はマウス処理でデッドロックが発生しやすい。
- 一度デッドロックが起きると再起動しないと回復しない場合がある。
- 条件例: **マウス処理の途中で別のマウス処理を開始**し、前処理の完了待ちが発生すると永久ロックになることがある。
- 例: 「右ボタン押下で左ボタン連打開始」等、入力イベントが入力処理を直接呼ぶ構造で起きやすい。

---

## 設計原則（必須）
### 1) クリック送出は“専用ワーカー”に集約
- `SendInput` 等の入力送出は **必ず単一の専用スレッド/Task** で行う。
- UIスレッド、ホットキーハンドラ、タイマーコールバックなどから直接入力送出しない。

### 2) 多重起動禁止（排他制御）
- 実行ループは常に最大1つ。
- 開始要求が連打されても新しいループを起動しない。
- 具体案:
  - `SemaphoreSlim(1,1)` で実行ループ進入をガード
  - 実行状態を `Atomic/volatile` + `lock` で一貫管理

### 3) ステートマシン
推奨状態:
- `Stopped`
- `Starting`（オプショナル）
- `Running`
- `Stopping`

許可遷移:
- Stopped -> Running
- Running -> Stopping -> Stopped
- Running で Start が来ても **何もしない（またはトグルで停止）**。新規起動は禁止。

### 4) キュー/コマンドモデル
UIやホットキーはワーカーに「コマンド」を送るだけにする。
- `StartRequested(configSnapshot)`
- `StopRequested()`
- `UpdateConfigRequested(configSnapshot)`（実行中に反映するなら。反映しないなら送らない）

ワーカー側でコマンドを順序通り処理し、入力送出を行う。

### 5) 待機は busy-wait 禁止
- `Thread.Sleep` / `Task.Delay` / WaitHandle / Timer を用いる
- 停止要求は最大でも「1クリック間隔以内」に反応する
  - `Task.Delay(interval, cancellationToken)` のようにキャンセル可能な待機を使う

---

## コンポーネント構成（例）
- `Ui`（WPF）
  - `MainWindow`
  - `ViewModel`（MVVM）
- `Core`
  - `ClickEngine`（ワーカー本体）
  - `ClickPlanBuilder`（単一点/複数点から実行プラン生成）
  - `HotkeyManager`（RegisterHotKey/UnregisterHotKey）
  - `SettingsStore`（settings.json 読み書き）
  - `DpiCoordinateTranslator`（任意: DPI/マルチモニタ補正）
- `Native`
  - `SendInputWrapper`（P/Invokeの薄いラッパ）
  - `CursorPositionProvider`（GetCursorPos など）

---

## 実行モデル（具体）
### 1) 設定スナップショット
- 実行開始時点のUI入力を `ConfigSnapshot` として固める
- ワーカーはスナップショットを参照し続ける（実行中にUIが変わっても影響させない）
  - もし実行中反映をするなら、コマンドでスナップショットを差し替える

### 2) 単一点ループ
- クリック -> 共通間隔待機 -> 次
- 回数指定なら N回で停止へ

### 3) 複数点ループ
- 1周: points[0..end] を順に 1クリックずつ
- 各点で:
  - クリック
  - `extra_wait_ms` 待機（0可）
  - 共通 `interval_ms` 待機
- 回数指定は **周回数**

---

## DPI / マルチモニタの扱い（最小要件）
- UIは座標が負になる可能性を許容する（仕様で明記済み）
- 取得した座標はそのまま保存・実行に使う
- 可能なら:
  - `GetCursorPos` は物理ピクセル基準になりやすい
  - WPF側のDPIスケールとのズレを避けるため、取得/実行ともに同一API系で統一する

---

## 安全装置（推奨）
- 異常停止に備え、`Stop` コマンドは常に優先（キュー先頭処理 or フラグ即時）
- 例外はワーカーで捕捉し、状態を Stopped に戻す
- 連打間隔が極端に短い（例: 1ms）場合はOS負荷が高い。仕様上は許容だが、注意文言はREADMEに記載する

---

## 禁止事項（再掲）
- UIイベント/ホットキーイベントから `SendInput` を直接呼ぶこと
- 実行ループの多重起動
- busy-wait
