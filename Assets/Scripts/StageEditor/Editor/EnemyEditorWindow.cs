using UnityEditor;
using UnityEngine;
using StageEditor.Data;

namespace StageEditor.Editor
{
    /// <summary>
    /// 敵エディタウィンドウ
    /// </summary>
    public class EnemyEditorWindow : EditorWindow
    {
        private Enemy currentEnemy;
        private EnemyDatabase database;
        private PatternDatabase patternDatabase;
        private Vector2 scrollPos;

        private System.Action<Enemy> onSave;

        public static void Open(Enemy enemy, EnemyDatabase database, PatternDatabase patternDatabase, System.Action<Enemy> onSave = null)
        {
            var window = GetWindow<EnemyEditorWindow>("Enemy Editor");
            window.currentEnemy = enemy;
            window.database = database;
            window.patternDatabase = patternDatabase;
            window.onSave = onSave;
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            if (currentEnemy == null)
            {
                EditorGUILayout.HelpBox("No enemy selected.", MessageType.Warning);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                EditorGUILayout.LabelField("Enemy Settings", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                // 基本情報
                currentEnemy.EnemyName = EditorGUILayout.TextField("Name", currentEnemy.EnemyName);
                currentEnemy.EnemyId = EditorGUILayout.TextField("ID", currentEnemy.EnemyId);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

                currentEnemy.Hp = EditorGUILayout.IntField("HP", currentEnemy.Hp);
                currentEnemy.Score = EditorGUILayout.IntField("Score", currentEnemy.Score);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);

                currentEnemy.PrefabPath = EditorGUILayout.TextField("Prefab Path", currentEnemy.PrefabPath);

                // Prefab選択ボタン
                if (GUILayout.Button("Select Prefab"))
                {
                    string path = EditorUtility.OpenFilePanel("Select Enemy Prefab", "Assets", "prefab");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Assetsからの相対パスに変換
                        if (path.StartsWith(Application.dataPath))
                        {
                            path = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                        currentEnemy.PrefabPath = path;
                    }
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Patterns", EditorStyles.boldLabel);

                // 移動パターン選択
                DrawPatternSelect("Move Pattern", ref currentEnemy.MovePatternId, patternDatabase.MovePatterns);

                // 弾幕パターン選択
                DrawBulletPatternSelect("Bullet Pattern", ref currentEnemy.BulletPatternId, patternDatabase.BulletPatterns);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Item Drop", EditorStyles.boldLabel);

                currentEnemy.ItemDropId = EditorGUILayout.TextField("Item ID", currentEnemy.ItemDropId);
                currentEnemy.ItemDropRate = EditorGUILayout.Slider("Drop Rate", currentEnemy.ItemDropRate, 0f, 1f);

                EditorGUILayout.Space(20);

                // プレビュー（Prefabがあれば）
                if (!string.IsNullOrEmpty(currentEnemy.PrefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentEnemy.PrefabPath);
                    if (prefab != null)
                    {
                        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                        var previewTexture = AssetPreview.GetAssetPreview(prefab);
                        if (previewTexture != null)
                        {
                            GUILayout.Label(previewTexture, GUILayout.Width(128), GUILayout.Height(128));
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // ボタン
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Save", GUILayout.Height(30)))
                {
                    onSave?.Invoke(currentEnemy);
                    Close();
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(30)))
                {
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPatternSelect(string label, ref string patternId, SerializableDictionary<string, MovePattern> patterns)
        {
            var names = new System.Collections.Generic.List<string> { "(None)" };
            var ids = new System.Collections.Generic.List<string> { "" };

            for (int i = 0; i < patterns.Count; i++)
            {
                ids.Add(patterns.Keys[i]);
                names.Add(patterns.Values[i].PatternName);
            }

            int selectedIndex = ids.IndexOf(patternId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, names.ToArray());
            patternId = ids[selectedIndex];
        }

        private void DrawBulletPatternSelect(string label, ref string patternId, SerializableDictionary<string, BulletPattern> patterns)
        {
            var names = new System.Collections.Generic.List<string> { "(None)" };
            var ids = new System.Collections.Generic.List<string> { "" };

            for (int i = 0; i < patterns.Count; i++)
            {
                ids.Add(patterns.Keys[i]);
                names.Add(patterns.Values[i].PatternName);
            }

            int selectedIndex = ids.IndexOf(patternId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, names.ToArray());
            patternId = ids[selectedIndex];
        }
    }
}
