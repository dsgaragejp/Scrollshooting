using UnityEngine;
using UnityEditor;
using System.IO;
using Game.Bullet;
using Game.Player;
using StageEditor.Runtime;

namespace StageEditor.Editor
{
    /// <summary>
    /// デモ用仮素材Prefabを生成するエディタツール
    /// </summary>
    public class DemoPrefabGenerator : EditorWindow
    {
        private static readonly string PrefabPath = "Assets/Resources/Prefabs";

        [MenuItem("Tools/Generate Demo Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<DemoPrefabGenerator>("Demo Prefab Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Demo Prefab Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "3Dプリミティブを使用した仮素材Prefabを生成します。\n" +
                "本番では3Dモデルに置き換えてください。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(40)))
            {
                GenerateAllPrefabs();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Individual Generation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player")) GeneratePlayerPrefab();
            if (GUILayout.Button("Enemies")) GenerateEnemyPrefabs();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bullets")) GenerateBulletPrefabs();
            if (GUILayout.Button("Demo Scene")) CreateDemoScene();
            EditorGUILayout.EndHorizontal();
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(PrefabPath))
            {
                Directory.CreateDirectory(PrefabPath);
                AssetDatabase.Refresh();
            }
        }

        public static void GenerateAllPrefabs()
        {
            EnsureDirectory();
            GeneratePlayerPrefab();
            GenerateEnemyPrefabs();
            GenerateBulletPrefabs();
            AssetDatabase.Refresh();
            Debug.Log("All demo prefabs generated!");
        }

        private static void GeneratePlayerPrefab()
        {
            EnsureDirectory();

            // プレイヤー（青い戦闘機風）
            var player = new GameObject("Player");

            // 本体（Cubeを斜めにして戦闘機風に）
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localScale = new Vector3(1f, 0.3f, 0.5f);
            body.transform.localPosition = Vector3.zero;
            SetMaterialColor(body, new Color(0.2f, 0.4f, 0.9f));

            // 翼
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing";
            wing.transform.SetParent(player.transform);
            wing.transform.localScale = new Vector3(0.3f, 0.1f, 1.2f);
            wing.transform.localPosition = new Vector3(-0.2f, 0, 0);
            SetMaterialColor(wing, new Color(0.3f, 0.5f, 1f));

            // コックピット
            var cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cockpit.name = "Cockpit";
            cockpit.transform.SetParent(player.transform);
            cockpit.transform.localScale = new Vector3(0.3f, 0.25f, 0.3f);
            cockpit.transform.localPosition = new Vector3(0.2f, 0.15f, 0);
            SetMaterialColor(cockpit, new Color(0.5f, 0.8f, 1f));

            // Colliderを設定（子のColliderは削除）
            foreach (var col in player.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(col);
            }
            var boxCollider = player.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1f, 0.4f, 1f);
            boxCollider.isTrigger = true;

            // コンポーネント追加
            player.AddComponent<PlayerController>();
            player.tag = "Player";

            // Prefab保存
            string path = $"{PrefabPath}/Player.prefab";
            PrefabUtility.SaveAsPrefabAsset(player, path);
            DestroyImmediate(player);

            Debug.Log($"Player prefab created: {path}");
        }

        private static void GenerateEnemyPrefabs()
        {
            EnsureDirectory();

            // ザコ敵（赤い小型機）
            CreateEnemyPrefab("Enemy_Small", new Color(0.9f, 0.2f, 0.2f), 0.6f);

            // 中型敵（オレンジ）
            CreateEnemyPrefab("Enemy_Medium", new Color(0.9f, 0.5f, 0.1f), 1f);

            // ボス（紫、大型）
            CreateBossPrefab("Enemy_Boss", new Color(0.6f, 0.2f, 0.8f), 2f);

            Debug.Log("Enemy prefabs created!");
        }

        private static void CreateEnemyPrefab(string name, Color color, float scale)
        {
            var enemy = new GameObject(name);

            // 本体
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(enemy.transform);
            body.transform.localScale = new Vector3(0.8f, 0.4f, 0.6f) * scale;
            body.transform.localPosition = Vector3.zero;
            SetMaterialColor(body, color);

            // 翼
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing";
            wing.transform.SetParent(enemy.transform);
            wing.transform.localScale = new Vector3(0.2f, 0.1f, 1f) * scale;
            wing.transform.localPosition = new Vector3(0.1f * scale, 0, 0);
            SetMaterialColor(wing, color * 0.8f);

            // Collider設定
            foreach (var col in enemy.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(col);
            }
            var boxCollider = enemy.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.8f, 0.4f, 1f) * scale;
            boxCollider.isTrigger = true;

            // EnemyController追加
            enemy.AddComponent<EnemyController>();
            enemy.tag = "Enemy";

            // Prefab保存
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(enemy, path);
            DestroyImmediate(enemy);
        }

        private static void CreateBossPrefab(string name, Color color, float scale)
        {
            var boss = new GameObject(name);

            // メイン本体
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(boss.transform);
            body.transform.localScale = new Vector3(1.5f, 0.6f, 1f) * scale;
            body.transform.localPosition = Vector3.zero;
            SetMaterialColor(body, color);

            // 上部パーツ
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Top";
            top.transform.SetParent(boss.transform);
            top.transform.localScale = new Vector3(0.8f, 0.4f, 0.6f) * scale;
            top.transform.localPosition = new Vector3(0, 0.4f * scale, 0);
            SetMaterialColor(top, color * 0.9f);

            // 翼（左）
            var wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingL.name = "WingL";
            wingL.transform.SetParent(boss.transform);
            wingL.transform.localScale = new Vector3(0.5f, 0.2f, 1.5f) * scale;
            wingL.transform.localPosition = new Vector3(0, 0, 0.8f * scale);
            SetMaterialColor(wingL, color * 0.8f);

            // 翼（右）
            var wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingR.name = "WingR";
            wingR.transform.SetParent(boss.transform);
            wingR.transform.localScale = new Vector3(0.5f, 0.2f, 1.5f) * scale;
            wingR.transform.localPosition = new Vector3(0, 0, -0.8f * scale);
            SetMaterialColor(wingR, color * 0.8f);

            // コア（光る部分）
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(boss.transform);
            core.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f) * scale;
            core.transform.localPosition = new Vector3(-0.3f * scale, 0, 0);
            SetMaterialColor(core, new Color(1f, 0.3f, 0.3f));

            // Collider設定
            foreach (var col in boss.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(col);
            }
            var boxCollider = boss.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1.5f, 0.8f, 2f) * scale;
            boxCollider.isTrigger = true;

            // EnemyController追加
            boss.AddComponent<EnemyController>();
            boss.tag = "Enemy";

            // Prefab保存
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(boss, path);
            DestroyImmediate(boss);
        }

        private static void GenerateBulletPrefabs()
        {
            EnsureDirectory();

            // 自機弾（黄色い小さな弾）
            CreateBulletPrefab("PlayerBullet", new Color(1f, 0.9f, 0.2f), 0.15f, true);

            // 敵弾（赤い丸弾）
            CreateBulletPrefab("EnemyBullet", new Color(1f, 0.2f, 0.2f), 0.2f, false);

            // 敵弾（紫の大きな弾）
            CreateBulletPrefab("EnemyBullet_Large", new Color(0.8f, 0.2f, 1f), 0.35f, false);

            Debug.Log("Bullet prefabs created!");
        }

        private static void CreateBulletPrefab(string name, Color color, float scale, bool isPlayerBullet)
        {
            var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = name;
            bullet.transform.localScale = Vector3.one * scale;

            // マテリアル設定（発光風）
            var renderer = bullet.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.9f);
            renderer.material = mat;

            // Collider設定
            var collider = bullet.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            // Bulletコンポーネント追加
            if (isPlayerBullet)
            {
                bullet.AddComponent<PlayerBullet>();
                bullet.tag = "PlayerBullet";
            }
            else
            {
                bullet.AddComponent<EnemyBullet>();
                bullet.tag = "EnemyBullet";
            }

            // Prefab保存
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(bullet, path);
            DestroyImmediate(bullet);
        }

        private static void SetMaterialColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                renderer.material = mat;
            }
        }

        private static void CreateDemoScene()
        {
            // 新しいシーンを作成
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // カメラ設定
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0, 10, 0);
                camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                camera.orthographic = true;
                camera.orthographicSize = 6;
                camera.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
            }

            // ライト調整
            var light = FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                light.intensity = 1.5f;
            }

            // GameManager作成
            var gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Game.GameManager>();

            // StageManager作成
            var stageManager = new GameObject("StageManager");
            stageManager.AddComponent<StageManager>();

            // プレイヤー配置
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Player.prefab");
            if (playerPrefab != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(-6, 0, 0);
            }

            // 背景（シンプルなグリッド）
            CreateBackgroundGrid();

            // シーン保存
            string scenePath = "Assets/Scenes/DemoScene.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"Demo scene created: {scenePath}");
        }

        private static void CreateBackgroundGrid()
        {
            var bgParent = new GameObject("Background");

            // グリッド線を作成
            for (int i = -10; i <= 10; i++)
            {
                // 横線
                var hLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hLine.name = $"HLine_{i}";
                hLine.transform.SetParent(bgParent.transform);
                hLine.transform.position = new Vector3(0, -1, i * 2);
                hLine.transform.localScale = new Vector3(30, 0.02f, 0.05f);
                SetMaterialColor(hLine, new Color(0.1f, 0.2f, 0.3f));
                DestroyImmediate(hLine.GetComponent<Collider>());
            }

            for (int i = -15; i <= 15; i++)
            {
                // 縦線
                var vLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vLine.name = $"VLine_{i}";
                vLine.transform.SetParent(bgParent.transform);
                vLine.transform.position = new Vector3(i * 2, -1, 0);
                vLine.transform.localScale = new Vector3(0.05f, 0.02f, 25);
                SetMaterialColor(vLine, new Color(0.1f, 0.2f, 0.3f));
                DestroyImmediate(vLine.GetComponent<Collider>());
            }
        }
    }
}
