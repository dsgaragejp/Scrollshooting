using UnityEngine;
using UnityEditor;

namespace StageEditor.Editor
{
    /// <summary>
    /// ダイアログなしで即座に背景を設定するエディタスクリプト
    /// </summary>
    public static class QuickBackgroundSetup
    {
        [MenuItem("Tools/Stellar Vanguard/Instant Background Setup", false, 102)]
        public static void SetupBackground()
        {
            // 既存のCosmicBackgroundを削除
            var existing = GameObject.Find("CosmicBackground");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // シェーダーを探す
            Shader shader = Shader.Find("StellarVanguard/CosmicBackground");
            if (shader == null)
            {
                // フォールバック: URP Unlitを使用
                shader = Shader.Find("Universal Render Pipeline/Unlit");
                Debug.LogWarning("CosmicBackground shader not found, using URP Unlit");
            }

            // マテリアル作成または取得
            string materialPath = "Assets/Materials/CosmicBackgroundMaterial.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(shader);
                material.name = "CosmicBackgroundMaterial";

                // フォルダ作成
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }

                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssets();
            }

            // Quadを作成
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CosmicBackground";
            Undo.RegisterCreatedObjectUndo(quad, "Create Cosmic Background");

            // Colliderを削除
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            // Transform設定
            quad.transform.position = new Vector3(0, 1, 50);
            quad.transform.localScale = new Vector3(200, 120, 1);
            quad.transform.rotation = Quaternion.identity;

            // マテリアル適用
            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // シーンを保存
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("CosmicBackground created: Position=(0,1,50), Scale=(200,120,1)");

            // 選択
            Selection.activeGameObject = quad;
        }

        [MenuItem("Tools/Stellar Vanguard/Fix Background Scale", false, 103)]
        public static void FixBackgroundScale()
        {
            var bg = GameObject.Find("CosmicBackground");
            if (bg == null)
            {
                Debug.LogError("CosmicBackground not found");
                return;
            }

            Undo.RecordObject(bg.transform, "Fix Background Scale");
            bg.transform.localScale = new Vector3(200, 120, 1);

            Debug.Log("Background scale fixed to (200, 120, 1)");
        }
    }
}
