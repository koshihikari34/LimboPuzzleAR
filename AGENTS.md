# AGENTS.md

## 基本方針

- 日本語で簡潔に報告する。
- 作業前に短く方針を共有する。
- 実装はレビュー可能な小さなステップに分け、一度に完成形まで作らない。
- 各ステップの編集前に、目的、変更予定ファイル、実装内容、確認方法を説明し、ユーザーの了承を得る。
- コードや依存関係をユーザーと確認しながら進め、了承前にファイルを編集しない。
- 既存のユーザー変更を勝手に戻さない。
- 依頼と無関係なファイルを変更しない。
- 削除、大規模な移動、依存関係の追加、破壊的操作は事前に確認する。
- 可能な範囲で検証し、完了報告に結果または未検証理由を記載する。
- 不確実な点を推測で仕様化せず、前提または未決事項として明示する。
- ユーザーがプッシュ、PR作成、マージを担当する。こちらは明示依頼がない限りローカルコミットまでに留める。
- コミット前に必ず `git config user.name` と `git config user.email` を確認し、`koshihikari34` / `koshihika0@gmail.com` であることを確認する。
- コミット前に `git diff --cached --name-only` を確認し、意図したファイルだけがステージされていることを確認する。
- Unity自動差分が多い時は、全体差分を読まず、依頼対象ファイルだけ確認する。
- `git diff --check` は必要時またはコミット直前に限り、確認の時間とトークンを浪費しない。
- `.vscode/` はコミット対象にしない。

## プロジェクト概要

- プロジェクト名は `LimboPuzzleAR`。
- Unity製のiOS向けARゲーム。
- コア体験は、石積み物理パズルと「指を離してはいけない」緊張感の組み合わせ。
- ゲーム仕様の正本は `Docs/GAME_SPEC.md` とする。

## 技術方針

- Unity: `6000.3.13f1`
- AR: AR Foundation
- DI: VContainer
- Reactive Extensions: R3
- UI: uGUI
- 鬼のAIは外部Behaviorツールを使わず、C#の明示的なステートマシンで実装する。
- 設計はFeature-BasedなMVVMを基本とするが、小規模な処理を過剰に分割しない。
- ViewModelは具体的なViewを参照せず、R3で状態とイベントを公開する。
- ViewはViewModelを購読し、UnityオブジェクトとUIの表示を更新する。
- インターフェースはAR、保存、時刻など、差し替えやテストが必要な外部境界を中心に導入する。
- `MonoBehaviour` はUnityイベント、参照、表示、物理処理の橋渡しを担当する。
- ゲームルールや状態遷移は、可能な限り通常のC#クラスへ分離する。
- グローバルなSingletonを追加せず、ライフタイムはVContainerで管理する。
- 詳細な設計方針は `Docs/ARCHITECTURE.md` を正本とする。

## 実装ルール

- ゲームコードは原則 `Assets/LimboPuzzleAR/Scripts/` 以下に置く。
- シーン固有コードは `Title/` と `Main/`、共通処理は `Common/` に分ける。
- namespaceは `LimboPuzzleAR` をルートとし、機能単位で分ける。
- Inspectorで調整する値には `[SerializeField]` を使用し、意味のある単位を名前またはTooltipで示す。
- 速度、時間、高さなどの調整値をコードへ散在させない。
- public APIを必要以上に増やさない。
- 複雑な条件分岐、状態遷移、座標変換、回避しづらい制約には、処理の意図や理由が分かる短いコメントを書く。
- レビューで差分を追いやすいよう、仕様上重要な判定には対応するルールをコメントで示す。
- コードを読み上げるだけのコメントや、実装と重複する自明なコメントは追加しない。
- 実装変更により内容が古くなったコメントは、同じ変更内で更新または削除する。
- 新規スクリプトには、対象ロジックに応じてEditModeまたはPlayModeテストを検討する。

## Unity固有の注意

- `.meta` ファイルを対応するアセットと一緒に扱う。
- Scene、Prefab、ProjectSettingsの変更は差分が大きくなりやすいため、必要な範囲に限定する。
- AR実機依存部分と、Editorで検証できるゲームロジックを分離する。
- iOS実機でしか確認できない項目は、完了報告で未検証として明示する。
- `Assets/Settings/DefaultVolumeProfile.asset`、`Assets/Settings/Mobile_RPAsset.asset`、`Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`、`ProjectSettings/UnityConnectSettings.asset` はUnity自動差分が出やすい。意図した変更でない限りコミットしない。
- `ProjectSettings/ProjectSettings.asset` はBundle Identifier、productNameなど意図した差分だけを確認して扱う。
- 大きなアセットパックを追加した場合は、ライセンス、配置場所、コミット対象を確認してからステージする。
