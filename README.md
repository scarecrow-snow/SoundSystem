# Unity Sound Manager システム


## 概要

このプロジェクトは、Unity向けの包括的なサウンド管理システムです。効率的なオーディオ再生、オブジェクトプーリング、音楽管理機能を提供します。  
**非同期処理には [UniTask](https://github.com/Cysharp/UniTask) を利用しています。**

## 主要機能

### 1. サウンド管理 (`SoundManager`)
- **オブジェクトプーリング**: `SoundEmitter`のプールを管理し、パフォーマンスを最適化
- **同時再生制限**: 同じサウンドの同時再生数を制限
- **ビルダーパターン**: 柔軟なサウンド作成・再生システム

### 2. 音楽管理 (`MusicManager`)
- **プレイリスト機能**: 音楽トラックのキューイング
- **クロスフェード**: 滑らかなトラック間の遷移
- **対数フェード**: 自然な音量変化

### 3. サウンドデータ (`SoundData`)
- **完全な設定**: AudioSourceの全パラメータをサポート
- **3D音響**: 空間音響設定
- **ミキサー統合**: AudioMixerGroupサポート

## アーキテクチャ

### コアクラス

#### `SoundManager` (SoundManager.cs:7)
- シングルトンパターンで実装
- オブジェクトプーリングによる効率的なリソース管理
- 同時再生数の制御機能

#### `MusicManager` (MusicManager.cs:7)
- 音楽トラックの管理
- クロスフェード機能
- プレイリスト機能

#### `SoundEmitter` (SoundEmitter.cs:10)
- 実際のオーディオ再生を担当
- **UniTaskを使用した非同期処理**
- 自動的なプールへの返却

#### `SoundBuilder` (SoundBuilder.cs:5)
- ビルダーパターンによるサウンド設定
- 位置指定、ピッチランダム化などの機能

#### `SoundData` (SoundData.cs:8)
- サウンドの設定を格納するシリアライズ可能なクラス
- AudioSourceの全パラメータをサポート

### 拡張クラス

#### `AudioExtensions` (AudioExtensions.cs:4)
- 対数音量変換機能
- UIスライダーとの統合サポート

#### `GameObjectExtensions` (GameObjectExtensions.cs:4)
- コンポーネントの取得・追加を簡素化

#### `PersistentSingleton` (PersistentSingleton.cs:4)
- シーン間で持続するシングルトンの基底クラス

## 使用方法

### 基本的なサウンド再生

```csharp
// SoundDataを作成
var soundData = new SoundData()
{
    clip = audioClip,
    mixerGroup = mixerGroup,
    loop = false,
    volume = 1.0f
};

// サウンドを再生
SoundManager.Instance.CreateSoundBuilder()
    .WithSoundData(soundData)
    .WithPosition(transform.position)
    .WithRandomPitch()
    .Play();
```

### 音楽再生

```csharp
// トラックをプレイリストに追加
MusicManager.Instance.AddToPlaylist(musicClip);

// 特定のトラックを即座に再生
MusicManager.Instance.Play(musicClip);
```

## 設定項目

### SoundManager設定
- `defaultCapacity`: プールの初期容量 (デフォルト: 10)
- `maxPoolSize`: プールの最大サイズ (デフォルト: 100)
- `maxSoundInstances`: 同じサウンドの最大同時再生数 (デフォルト: 30)

### MusicManager設定
- `crossFadeTime`: クロスフェード時間 (デフォルト: 1.0秒)
- `initialPlaylist`: 初期プレイリスト

## 依存関係

- **Unity Audio System**: 基本的なオーディオ機能
- **UniTask**: 非同期処理 ([Cysharp.Threading.Tasks](https://github.com/Cysharp/UniTask))  
  ※本システムはUniTaskに依存しています。  
- **Unity Object Pooling**: オブジェクトプーリング (UnityEngine.Pool)

## ファイル構造

```
Assets/SoundSystem/Scripts/
├── Core/                    # コア機能
│   ├── SoundManager.cs      # メインサウンド管理
│   ├── MusicManager.cs      # 音楽管理
│   ├── SoundEmitter.cs      # サウンド再生
│   └── SoundBuilder.cs      # ビルダーパターン
├── Data/                    # データクラス
│   └── SoundData.cs         # サウンドデータ
├── Utils/                   # ユーティリティ・拡張
│   ├── AudioExtensions.cs   # オーディオ拡張
│   ├── GameObjectExtensions.cs # GameObject拡張
│   └── PersistentSingleton.cs  # シングルトン基底
├── Demo/                    # デモ・サンプル
│   └── Manupilator.cs       # 使用例デモ

Assets/SoundSystem/
├── Prefabs/                 # プレハブ
    └── SoundEmitterPrefab.prefab # エミッタープレハブ
```

## フォルダ構造

### Core
- システムの中核となるクラス群
- サウンド管理の主要機能を集約
- 依存関係が明確

### Data
- データクラスの分離
- 設定情報の管理
- 再利用性の向上

### Utils
- 共通ユーティリティの集約
- 拡張メソッドの管理
- 他プロジェクトへの移植性

### Demo
- サンプルコードの分離
- 本番環境での除外が容易
- 学習リソースとして活用

### Prefabs
- プレハブの専用管理
- シーンでの参照が容易
- バージョン管理の効率化

## 推奨設定

1. **AudioMixer**: 音量調整とエフェクト処理
2. **3D Audio**: 空間音響を活用する場合
3. **Performance**: モバイル環境では同時再生数を制限

## 注意事項

- `SoundEmitter`は自動的にプールに返却されるため、手動での管理は不要
- 同時再生数の制限により、パフォーマンスが維持される
- 対数音量変換により、より自然な音量調整が可能
- フォルダ構造により機能別の管理が容易