# LimboPuzzleAR アーキテクチャ

## 1. 目的

AR実機に依存する処理とゲームルールを分離し、Editor上でも主要な実装とテストを進められる構成にする。

設計はFeature-BasedなMVVMを基本とし、必要になる前の過剰な抽象化は避ける。

## 2. 依存方向

基本的な依存方向は次のとおりとする。

```text
View -> ViewModel -> Model
          |
          v
       Service
```

- ViewはViewModelを参照する。
- ViewModelはModelと必要なServiceを参照する。
- ModelはView、ViewModel、Unityの表示オブジェクトを参照しない。
- ViewModelは具体的なViewを参照しない。
- VContainerが各クラスの生成と接続を担当する。

ViewModelからViewを直接呼び出す構成にはせず、ViewModelがR3で公開する状態やイベントをViewが購読する。

## 3. 各レイヤーの責務

### Model

- ゲームフェーズ、スコア、鬼の状態などを保持する。
- クリア、アウト、状態遷移などのゲームルールを実装する。
- 可能な範囲でUnityのライフサイクルやシーンオブジェクトへ依存しない。
- EditModeテストで検証可能な形を優先する。

### ViewModel

- ViewやServiceから受け取った入力をModelへ伝える。
- Modelの状態を表示用データへ変換する。
- `ReactiveProperty`や`Observable`を通じて、Viewが購読できる状態とイベントを公開する。
- `GameObject`、`Transform`、具体的なViewコンポーネントを保持しない。

ただし、ARの設置位置を表す`Pose`など、Unityの値型を境界データとして使用することは許容する。純粋C#テストを妨げる場合は、プロジェクト固有の値型への置き換えを検討する。

### View

- `MonoBehaviour`としてUnityイベントとシーン参照を扱う。
- ViewModelの状態を購読し、UI、土台、ガイド、石、鬼などの表示を更新する。
- タッチやボタン操作をViewModelまたは入力Serviceへ渡す。
- ゲームルールや状態遷移を判断しない。

uGUIには自動データバインディングを導入せず、R3による明示的な購読を使用する。

### Service

- AR Raycast、永続化、時刻など外部環境との境界を担当する。
- ViewModelから利用されるが、ゲームルールは持たない。
- 実機用とEditor用で動作を差し替える必要がある境界はインターフェース化する。

## 4. インターフェース方針

ViewとViewModelを機械的にインターフェース化しない。次のいずれかに該当する境界へ導入する。

- 実機用とEditor用の実装を差し替える。
- 単体テストでテストダブルへ置き換える。
- 保存方式など、複数実装が実際に必要になる。

AR設置では次のような境界を想定する。

```csharp
public interface IARPlacementService
{
    Observable<Pose> PlacementRequested { get; }
}
```

想定実装:

- `ARPlacementService`: AR FoundationのRaycastで平面上のPoseを取得する。
- `EditorPlacementService`: Editor上のマウス入力から確認用Poseを生成する。

VContainerの登録時に実行環境に対応する実装を選択し、ViewModelは具体的な実装を知らない。

インターフェースは必要な操作が明確になった段階で追加し、将来の可能性だけを理由に増やさない。

## 5. R3の役割

R3は次の用途に限定して使用する。

- ViewModelが公開する読み取り専用状態
- タッチ、ボタン、設置要求などのイベント
- タイマーと状態変化の通知
- Viewの購読解除をUnityのライフサイクルへ結び付ける処理

単純な同期処理まで無理にストリーム化せず、通常のメソッドの方が読みやすい場合はメソッドを使用する。

## 6. VContainerの役割

- Scene単位の`LifetimeScope`で依存関係を登録する。
- Model、ViewModel、Serviceのライフタイムを明示する。
- 実機用ServiceとEditor用Serviceを切り替える。
- グローバルなSingletonや静的Service Locatorを使用しない。

Scene上のViewはコンポーネント登録し、ViewModelやModelは通常のC#クラスとして生成する。

## 7. ディレクトリ構成

```text
Assets/LimboPuzzleAR/Scripts/
├── Common/
│   ├── Models/
│   └── Services/
├── Title/
│   ├── Models/
│   ├── Views/
│   ├── ViewModels/
│   └── Scopes/
└── Main/
    ├── Models/
    ├── Views/
    ├── ViewModels/
    ├── Services/
    └── Scopes/
```

テストは対象機能に対応させ、EditModeとPlayModeを分ける。

```text
Assets/LimboPuzzleAR/Tests/
├── EditMode/
└── PlayMode/
```

## 8. 検証方針

### Editorで確認する項目

- ゲームフェーズの遷移
- Watching中のReleaseによるアウト
- 石の安定時間とセットクリア
- Attack中の入力禁止
- タイマー、スコア、ハイスコア更新
- Editor用Serviceによる設置フロー

### 実機で確認するタイミング

#### AR設置完成時

- 平面検知とタップ位置
- 土台と実空間のずれ
- クリア高さ30cmの見え方

#### 石操作完成時

- 指への追従性
- 2.5D操作面の距離感
- 端末移動時の石の挙動

#### コアループ完成時

- 石積み、鬼、アウト、再開の一連の流れ
- フレームレート、発熱、物理演算の安定性

#### リリース前

- アプリの中断と復帰
- カメラ権限
- 長時間プレイ
- 対象端末間の表示と操作感の差

Editor確認は実機確認の代替ではなく、実機確認へ進む前にゲームロジックと基本フローを素早く検証するために使用する。

## 9. 最初の実装ステップ

最初はAR設置機能を次の小さな単位で進める。

1. `IARPlacementService`が公開する入力契約を定義する。
2. Editor用の設置入力を実装し、EditorでPoseを確認する。
3. AR Foundationを使用する実機用設置入力を実装する。
4. ViewModelで設置状態を保持する。
5. Viewで仮の土台を表示する。
6. Editor確認後、iOS実機で設置位置を確認する。

各ステップの編集前に、変更ファイル、実装内容、確認方法を共有し、了承を得てから進める。

## 10. 石操作の初期設計

石操作もMVVMで分ける。

```text
StoneView -> StoneViewModel -> StoneModel
                 |
                 v
          StonePlacementService
```

### StoneModel

- 現在掴んでいる石があるかを保持する。
- 石が落下中または安定待ちかを保持する。
- 次の石を生成できるかを判断する。
- 鬼の`Watching`中にReleaseされたかなど、ゲームルール側の判定と連携する。

### StoneViewModel

- Viewから押下、ドラッグ、リリースの入力を受け取る。
- `StonePlacementService`で画面座標をWorld座標へ変換する。
- Modelの状態を更新し、Viewが購読する表示用状態を公開する。
- 具体的なPrefab、Rigidbody、Colliderを参照しない。

### StonePlacementService

- 画面座標を石操作用の2.5D操作面へ変換する。
- 初期実装では、プレイエリアを通りカメラに平行な垂直面を使う。
- Editor用と実機用で操作感に差が出る場合は、Service内で差し替えられるようにする。

### StoneView

- 石Prefabの生成と表示を担当する。
- 掴み中は`Rigidbody.isKinematic = true`にする。
- リリース時は`Rigidbody.isKinematic = false`に戻す。
- Transform更新とRigidbody操作に限定し、ゲームルールは判断しない。

初期実装ではNextStone UIをまだ作らず、Editor確認しやすいように画面下側の押下から石操作を開始する。操作フローが固まった後、画面下中央のNextStone UIへ置き換える。
