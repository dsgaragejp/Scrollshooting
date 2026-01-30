using UnityEngine;
using UnityEditor;
using Game.Background;

namespace StageEditor.Editor
{
    /// <summary>
    /// 宇宙背景をシーンに追加するエディタツール
    /// </summary>
    public class BackgroundSetupTool : EditorWindow
    {
        private enum BackgroundType
        {
            CosmicNebula,
            BlackHole,
            Wormhole
        }

        private BackgroundType selectedType = BackgroundType.CosmicNebula;
        private Material createdMaterial;

        [MenuItem("Tools/Stellar Vanguard/Setup Cosmic Background", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<BackgroundSetupTool>("Cosmic Background Setup");
        }

        [MenuItem("Tools/Stellar Vanguard/Quick Add Background", false, 101)]
        public static void QuickAddBackground()
        {
            CreateCosmicBackground();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stellar Vanguard - 宇宙背景セットアップ", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            selectedType = (BackgroundType)EditorGUILayout.EnumPopup("背景タイプ", selectedType);

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(GetBackgroundDescription(), MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("背景を作成", GUILayout.Height(40)))
            {
                CreateBackground();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("作成されるもの:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("• シェーダーマテリアル");
            EditorGUILayout.LabelField("• 背景オブジェクト (SimpleCosmicBackground)");
            EditorGUILayout.LabelField("• カメラに自動追従");
        }

        private string GetBackgroundDescription()
        {
            switch (selectedType)
            {
                case BackgroundType.CosmicNebula:
                    return "XorDev風のフラクタル宇宙背景。\n虹色に輝く星雲とエネルギーの流れを表現します。";
                case BackgroundType.BlackHole:
                    return "インターステラー風ブラックホール「ガルガンチュア」。\n降着円盤、重力レンズ効果、フォトンリングを再現します。";
                case BackgroundType.Wormhole:
                    return "球体ワームホール。\n空間の歪みと虹色のトンネルエフェクトを表現します。";
                default:
                    return "";
            }
        }

        private void CreateBackground()
        {
            switch (selectedType)
            {
                case BackgroundType.CosmicNebula:
                    CreateCosmicBackground();
                    break;
                case BackgroundType.BlackHole:
                    CreateBlackHoleBackground();
                    break;
                case BackgroundType.Wormhole:
                    CreateWormholeBackground();
                    break;
            }
        }

        private static void CreateCosmicBackground()
        {
            // シェーダーを探す
            Shader shader = Shader.Find("StellarVanguard/CosmicBackground");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Error", "CosmicBackground シェーダーが見つかりません。\nAssets/Shaders/CosmicBackground.shader を確認してください。", "OK");
                return;
            }

            // マテリアル作成
            Material material = CreateMaterialAsset(shader, "CosmicBackgroundMaterial");

            // 背景オブジェクト作成
            GameObject bgObject = CreateBackgroundObject("CosmicBackground", material);

            Selection.activeGameObject = bgObject;
            EditorUtility.DisplayDialog("成功", "宇宙背景を作成しました！\n\nHierarchyで「CosmicBackground」を選択し、\nInspectorでパラメータを調整できます。", "OK");
        }

        private static void CreateBlackHoleBackground()
        {
            Shader shader = Shader.Find("StellarVanguard/BlackHole");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Error", "BlackHole シェーダーが見つかりません。", "OK");
                return;
            }

            Material material = CreateMaterialAsset(shader, "BlackHoleMaterial");

            // ブラックホール用のデフォルト設定
            material.SetFloat("_BlackHoleRadius", 0.15f);
            material.SetFloat("_AccretionDiskWidth", 0.1f);
            material.SetFloat("_GravityStrength", 2.0f);
            material.SetColor("_DiskColor1", new Color(1f, 0.6f, 0.1f, 1f));
            material.SetColor("_DiskColor2", new Color(1f, 0.9f, 0.5f, 1f));

            GameObject bgObject = CreateBackgroundObject("BlackHoleBackground", material);

            Selection.activeGameObject = bgObject;
            EditorUtility.DisplayDialog("成功", "ブラックホール背景を作成しました！", "OK");
        }

        private static void CreateWormholeBackground()
        {
            Shader shader = Shader.Find("StellarVanguard/Wormhole");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Error", "Wormhole シェーダーが見つかりません。", "OK");
                return;
            }

            Material material = CreateMaterialAsset(shader, "WormholeMaterial");

            // ワームホール用のデフォルト設定
            material.SetFloat("_WormholeRadius", 0.2f);
            material.SetFloat("_EdgeGlow", 2.0f);
            material.SetColor("_EdgeColor", new Color(0.5f, 0.7f, 1.0f, 1f));

            GameObject bgObject = CreateBackgroundObject("WormholeBackground", material);

            Selection.activeGameObject = bgObject;
            EditorUtility.DisplayDialog("成功", "ワームホール背景を作成しました！", "OK");
        }

        private static Material CreateMaterialAsset(Shader shader, string materialName)
        {
            // Materialsフォルダがなければ作成
            string materialFolder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(materialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // マテリアル作成
            Material material = new Material(shader);
            material.name = materialName;

            string materialPath = $"{materialFolder}/{materialName}.mat";

            // 既存のマテリアルを確認
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                return existingMaterial;
            }

            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();

            return material;
        }

        private static GameObject CreateBackgroundObject(string name, Material material)
        {
            // 既存の同名オブジェクトを削除
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 新規オブジェクト作成
            GameObject bgObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(bgObject, "Create Cosmic Background");

            // コンポーネント追加
            SimpleCosmicBackground bgController = bgObject.AddComponent<SimpleCosmicBackground>();
            bgController.SetMaterial(material);
            bgController.SetCamera(Camera.main);

            return bgObject;
        }
    }
}
