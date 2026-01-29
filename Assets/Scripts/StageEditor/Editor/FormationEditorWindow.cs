using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StageEditor.Data;

namespace StageEditor.Editor
{
    /// <summary>
    /// 敵編隊エディタウィンドウ
    /// </summary>
    public class FormationEditorWindow : EditorWindow
    {
        private EnemyFormation currentFormation;
        private EnemyDatabase database;
        private PatternDatabase patternDatabase;
        private Vector2 scrollPos;
        private Vector2 previewScrollPos;

        private System.Action<EnemyFormation> onSave;

        // プレビュー設定
        private float previewScale = 20f;
        private Vector2 previewOffset = new Vector2(150, 150);

        public static void Open(EnemyFormation formation, EnemyDatabase database, PatternDatabase patternDatabase, System.Action<EnemyFormation> onSave = null)
        {
            var window = GetWindow<FormationEditorWindow>("Formation Editor");
            window.currentFormation = formation;
            window.database = database;
            window.patternDatabase = patternDatabase;
            window.onSave = onSave;
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            if (currentFormation == null)
            {
                EditorGUILayout.HelpBox("No formation selected.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                // 左側：設定パネル
                EditorGUILayout.BeginVertical(GUILayout.Width(280));
                {
                    DrawSettingsPanel();
                }
                EditorGUILayout.EndVertical();

                // 右側：プレビュー
                EditorGUILayout.BeginVertical();
                {
                    DrawPreviewPanel();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ボタン
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Save", GUILayout.Height(30)))
                {
                    onSave?.Invoke(currentFormation);
                    Close();
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(30)))
                {
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsPanel()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                EditorGUILayout.LabelField("Formation Settings", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                currentFormation.FormationName = EditorGUILayout.TextField("Name", currentFormation.FormationName);
                currentFormation.FormationId = EditorGUILayout.TextField("ID", currentFormation.FormationId);

                EditorGUILayout.Space(10);

                // 移動パターン選択
                DrawPatternSelect("Move Pattern", ref currentFormation.MovePatternId);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Members", EditorStyles.boldLabel);

                // メンバー追加ボタン
                if (GUILayout.Button("+ Add Member"))
                {
                    currentFormation.Members.Add(new FormationMember());
                }

                EditorGUILayout.Space(5);

                // メンバー一覧
                for (int i = 0; i < currentFormation.Members.Count; i++)
                {
                    var member = currentFormation.Members[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"Member {i + 1}", EditorStyles.boldLabel);
                            if (GUILayout.Button("X", GUILayout.Width(20)))
                            {
                                currentFormation.Members.RemoveAt(i);
                                i--;
                                continue;
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        // 敵選択
                        DrawEnemySelect(ref member.EnemyId);

                        // 相対位置
                        member.LocalPosition = EditorGUILayout.Vector2Field("Position", member.LocalPosition);

                        // 出現遅延
                        member.SpawnDelay = EditorGUILayout.FloatField("Spawn Delay", member.SpawnDelay);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.Space(10);

                // プリセット編隊
                EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("V Formation"))
                    {
                        ApplyPreset_V();
                    }
                    if (GUILayout.Button("Line"))
                    {
                        ApplyPreset_Line();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Circle"))
                    {
                        ApplyPreset_Circle();
                    }
                    if (GUILayout.Button("Grid"))
                    {
                        ApplyPreset_Grid();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            // ズームスライダー
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Scale:", GUILayout.Width(40));
                previewScale = EditorGUILayout.Slider(previewScale, 5f, 50f);
            }
            EditorGUILayout.EndHorizontal();

            // プレビューエリア
            Rect previewRect = GUILayoutUtility.GetRect(300, 300);
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.15f));

            // グリッド描画
            DrawGrid(previewRect);

            // 中心点
            Vector2 center = new Vector2(previewRect.x + previewRect.width / 2, previewRect.y + previewRect.height / 2);

            // 中心マーカー
            EditorGUI.DrawRect(new Rect(center.x - 2, center.y - 2, 4, 4), Color.white);

            // メンバー描画
            for (int i = 0; i < currentFormation.Members.Count; i++)
            {
                var member = currentFormation.Members[i];
                Vector2 pos = center + member.LocalPosition * previewScale;

                // 敵の色
                Color color = GetEnemyColor(member.EnemyId, i);
                EditorGUI.DrawRect(new Rect(pos.x - 8, pos.y - 8, 16, 16), color);

                // インデックス表示
                GUI.Label(new Rect(pos.x - 5, pos.y - 7, 20, 20), (i + 1).ToString());
            }
        }

        private void DrawGrid(Rect rect)
        {
            Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            float gridSize = previewScale;

            // 縦線
            for (float x = rect.x; x < rect.xMax; x += gridSize)
            {
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), gridColor);
            }

            // 横線
            for (float y = rect.y; y < rect.yMax; y += gridSize)
            {
                EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1), gridColor);
            }
        }

        private Color GetEnemyColor(string enemyId, int index)
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                return new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            // インデックスに基づいて色を変える
            float hue = (index * 0.15f) % 1f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        private void DrawPatternSelect(string label, ref string patternId)
        {
            var names = new List<string> { "(None)" };
            var ids = new List<string> { "" };

            for (int i = 0; i < patternDatabase.MovePatterns.Count; i++)
            {
                ids.Add(patternDatabase.MovePatterns.Keys[i]);
                names.Add(patternDatabase.MovePatterns.Values[i].PatternName);
            }

            int selectedIndex = ids.IndexOf(patternId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, names.ToArray());
            patternId = ids[selectedIndex];
        }

        private void DrawEnemySelect(ref string enemyId)
        {
            var names = new List<string> { "(None)" };
            var ids = new List<string> { "" };

            for (int i = 0; i < database.Enemies.Count; i++)
            {
                ids.Add(database.Enemies.Keys[i]);
                names.Add(database.Enemies.Values[i].EnemyName);
            }

            int selectedIndex = ids.IndexOf(enemyId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Enemy", selectedIndex, names.ToArray());
            enemyId = ids[selectedIndex];
        }

        #region Presets

        private void ApplyPreset_V()
        {
            currentFormation.Members.Clear();

            // V字編隊（5機）
            currentFormation.Members.Add(new FormationMember { LocalPosition = new Vector2(0, 0), SpawnDelay = 0f });
            currentFormation.Members.Add(new FormationMember { LocalPosition = new Vector2(-1, -1), SpawnDelay = 0.1f });
            currentFormation.Members.Add(new FormationMember { LocalPosition = new Vector2(1, -1), SpawnDelay = 0.1f });
            currentFormation.Members.Add(new FormationMember { LocalPosition = new Vector2(-2, -2), SpawnDelay = 0.2f });
            currentFormation.Members.Add(new FormationMember { LocalPosition = new Vector2(2, -2), SpawnDelay = 0.2f });
        }

        private void ApplyPreset_Line()
        {
            currentFormation.Members.Clear();

            // 横一列（5機）
            for (int i = 0; i < 5; i++)
            {
                currentFormation.Members.Add(new FormationMember
                {
                    LocalPosition = new Vector2(0, i - 2),
                    SpawnDelay = i * 0.1f
                });
            }
        }

        private void ApplyPreset_Circle()
        {
            currentFormation.Members.Clear();

            // 円形配置（8機）
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2 / 8;
                currentFormation.Members.Add(new FormationMember
                {
                    LocalPosition = new Vector2(Mathf.Cos(angle) * 2, Mathf.Sin(angle) * 2),
                    SpawnDelay = i * 0.05f
                });
            }
        }

        private void ApplyPreset_Grid()
        {
            currentFormation.Members.Clear();

            // 3x3グリッド
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    currentFormation.Members.Add(new FormationMember
                    {
                        LocalPosition = new Vector2(x - 1, y - 1),
                        SpawnDelay = (y * 3 + x) * 0.05f
                    });
                }
            }
        }

        #endregion
    }
}
