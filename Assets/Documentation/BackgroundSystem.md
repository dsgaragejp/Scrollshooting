# Stellar Vanguard 背景シェーダーシステム

## 概要

XorDev氏のGLSLシェーダーコードをUnity URP (Universal Render Pipeline) 用のHLSLに変換した背景システム。
フラクタルパターンやグローエフェクトを使用して、宇宙空間の美しい背景を描画する。

## シェーダー一覧

### 1. CosmicBackground（宇宙フラクタル背景）

**ファイル:** `Assets/Shaders/CosmicBackground.shader`

**オリジナルコード (XorDev GLSL):**
```glsl
vec2 p=(FC.xy*2.-r)/r.y/.3,v;
for(float i,l,f;i++<9.;o+=.1/abs(l=dot(p,p)-5.-2./v.y)*(cos(i/3.+.1/l+vec4(1,2,3,4))+1.))
  for(v=p,f=0.;f++<9.;v+=sin(ceil(v.yx*f+i*.3)+r-t/2.)/f);
o=max(tanh(o+(o=texture(b,(FC.xy+r.y*.04*sin(FC.xy+FC.yx/.6))/r))*o),.0);
```

**特徴:**
- 9x9のネストループによるフラクタルパターン生成
- 虹色のカラーグラデーション
- 時間ベースのアニメーション
- tanhトーンマッピング

**パラメータ:**
| パラメータ | 範囲 | デフォルト | 説明 |
|-----------|------|-----------|------|
| Animation Speed | 0.1 - 3.0 | 1.0 | アニメーション速度 |
| Pattern Scale | 10 - 1000 | 80 | パターンの大きさ（推奨: 80 @ 16:9） |
| Color Intensity | 0.05 - 0.5 | 0.1 | 色の強さ |

---

### 2. DiagonalGlow（対角線グロー）

**ファイル:** `Assets/Shaders/DiagonalGlow.shader`

**オリジナルコード (XorDev GLSL):**
```glsl
vec2 p=(FC.xy*2.-r)/r.y,v;
o=tanh(.03*vec4(2,1,1.+p)
/(.05+max(v+=length(p)-.5,-v/.1)).x/(.1+abs(p.x-p.y)));
```

**特徴:**
- 中心からの円形フォールオフ
- 対角線のグローライン
- 位置に応じた色変化
- シンプルで軽量

**パラメータ:**
| パラメータ | 範囲 | デフォルト | 説明 |
|-----------|------|-----------|------|
| Intensity | 0.01 - 0.1 | 0.03 | 光の強さ |
| Circle Radius | 0.1 - 1.0 | 0.5 | 中心円の半径 |
| Diagonal Width | 0.05 - 0.5 | 0.1 | 対角線の太さ |
| ColorR | 0.5 - 4.0 | 2.0 | 赤チャンネル |
| ColorG | 0.5 - 4.0 | 1.0 | 緑チャンネル |
| ColorB | 0.5 - 4.0 | 1.0 | 青ベース |

---

## セットアップ方法

### 方法1: エディタメニューから（推奨）

1. **Tools > Stellar Vanguard > Instant Background Setup** を選択
2. 自動的にQuadとマテリアルが作成される
3. マテリアルのInspectorでパラメータを調整

### 方法2: 手動セットアップ

1. **マテリアル作成**
   - Project ウィンドウで右クリック > Create > Material
   - Shaderを `StellarVanguard/CosmicBackground` または `StellarVanguard/DiagonalGlow` に変更

2. **背景オブジェクト作成**
   - GameObject > 3D Object > Quad
   - 名前を `CosmicBackground` に変更
   - Transform設定:
     - Position: (0, 0, 50) ※カメラの前方
     - Scale: (200, 120, 1) ※画面全体をカバー

3. **マテリアル適用**
   - 作成したマテリアルをQuadにドラッグ&ドロップ
   - MeshRendererで影を無効化:
     - Cast Shadows: Off
     - Receive Shadows: Off

---

## カメラ設定

背景を正しく表示するためのカメラ設定:

```
Main Camera:
  Position: (0, 0, -10)
  Far Clip Plane: 1000 以上
  Clear Flags: Solid Color または Skybox
```

---

## アルゴリズム解説

### CosmicBackground のフラクタル生成

```hlsl
// 1. 座標の正規化（-1 to 1、アスペクト比補正）
float2 p = uv * 2.0 - 1.0;
p.x *= 16.0 / 9.0;
p *= _Scale;

// 2. 外側ループ: フラクタル蓄積
for (float i = 1.0; i < 10.0; i += 1.0)
{
    // 3. 内側ループ: v ベクトルの計算
    v = p;
    for (float f = 1.0; f < 10.0; f += 1.0)
    {
        float2 cellPos = ceil(v.yx * f + i * 0.3);
        v += sin(cellPos + 500.0 - t * 0.5) / f;
    }

    // 4. 距離計算とカラー蓄積
    float l = dot(p, p) - 5.0 - 2.0 / v.y;
    float4 color = cos(i / 3.0 + 0.1 / abs(l) + float4(1,2,3,4)) + 1.0;
    o += (_ColorIntensity / abs(l)) * color;
}

// 5. トーンマッピング
o = max(tanh(o), 0.0);
```

### DiagonalGlow の円形・対角線エフェクト

```hlsl
// 1. 中心からの距離
float v = length(p) - _CircleRadius;

// 2. スムーズなフォールオフ
float falloff = 0.05 + max(v, -v / 0.1);

// 3. 対角線エフェクト
float diagonal = _DiagonalWidth + abs(p.x - p.y);

// 4. 最終カラー計算
float4 o = tanh(_Intensity * color / falloff / diagonal);
```

---

## パフォーマンス考慮事項

- **CosmicBackground**: 9x9=81回のループがあるため、モバイルでは負荷が高い可能性
  - 対策: `_Iterations` パラメータを追加して調整可能にする

- **DiagonalGlow**: 軽量でモバイルでも使用可能

---

## クレジット

- オリジナルシェーダー: [XorDev](https://twitter.com/XorDev)
- HLSL変換・Unity URP対応: Stellar Vanguard開発チーム

---

## 関連ファイル

- `Assets/Shaders/CosmicBackground.shader` - 宇宙フラクタルシェーダー
- `Assets/Shaders/DiagonalGlow.shader` - 対角線グローシェーダー
- `Assets/Shaders/BlackHole.shader` - ブラックホールシェーダー
- `Assets/Shaders/Wormhole.shader` - ワームホールシェーダー
- `Assets/Scripts/StageEditor/Editor/QuickBackgroundSetup.cs` - エディタツール
- `Assets/Scripts/StageEditor/Editor/BackgroundSetupTool.cs` - 背景セットアップウィンドウ
- `Assets/Scripts/Game/Background/FullScreenShaderEffect.cs` - フルスクリーンエフェクト用RendererFeature
