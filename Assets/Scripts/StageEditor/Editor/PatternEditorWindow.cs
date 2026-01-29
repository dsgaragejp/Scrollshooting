using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StageEditor.Data;

namespace StageEditor.Editor
{
    /// <summary>
    /// 移動パターン/弾幕パターン エディタウィンドウ
    /// </summary>
    public class PatternEditorWindow : EditorWindow
    {
        private enum EditorMode
        {
            MovePattern,
            BulletPattern
        }

        private EditorMode mode;
        private MovePattern currentMovePattern;
        private BulletPattern currentBulletPattern;
        private Vector2 scrollPos;

        private System.Action onSave;

        // プレビュー
        private float previewTime = 0f;
        private bool isPreviewPlaying = false;
        private double lastPreviewTime;
        private float previewScale = 30f;

        public static void OpenMovePattern(MovePattern pattern, System.Action onSave = null)
        {
            var window = GetWindow<PatternEditorWindow>("Pattern Editor");
            window.mode = EditorMode.MovePattern;
            window.currentMovePattern = pattern;
            window.currentBulletPattern = null;
            window.onSave = onSave;
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        public static void OpenBulletPattern(BulletPattern pattern, System.Action onSave = null)
        {
            var window = GetWindow<PatternEditorWindow>("Pattern Editor");
            window.mode = EditorMode.BulletPattern;
            window.currentMovePattern = null;
            window.currentBulletPattern = pattern;
            window.onSave = onSave;
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void Update()
        {
            if (isPreviewPlaying)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                previewTime += (float)(currentTime - lastPreviewTime);
                lastPreviewTime = currentTime;

                if (previewTime > 5f)
                {
                    previewTime = 0f;
                }

                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                // 左側：設定
                EditorGUILayout.BeginVertical(GUILayout.Width(250));
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
                    onSave?.Invoke();
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
                if (mode == EditorMode.MovePattern)
                {
                    DrawMovePatternSettings();
                }
                else
                {
                    DrawBulletPatternSettings();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawMovePatternSettings()
        {
            if (currentMovePattern == null) return;

            EditorGUILayout.LabelField("Move Pattern Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            currentMovePattern.PatternName = EditorGUILayout.TextField("Name", currentMovePattern.PatternName);
            currentMovePattern.PatternId = EditorGUILayout.TextField("ID", currentMovePattern.PatternId);

            EditorGUILayout.Space(10);

            currentMovePattern.Type = (MovePatternType)EditorGUILayout.EnumPopup("Type", currentMovePattern.Type);
            currentMovePattern.Speed = EditorGUILayout.FloatField("Speed", currentMovePattern.Speed);
            currentMovePattern.Loop = EditorGUILayout.Toggle("Loop", currentMovePattern.Loop);

            EditorGUILayout.Space(10);

            // ウェイポイント設定
            if (currentMovePattern.Type == MovePatternType.Waypoint)
            {
                EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);

                if (GUILayout.Button("+ Add Waypoint"))
                {
                    currentMovePattern.Waypoints.Add(Vector2.zero);
                }

                for (int i = 0; i < currentMovePattern.Waypoints.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        currentMovePattern.Waypoints[i] = EditorGUILayout.Vector2Field($"Point {i + 1}", currentMovePattern.Waypoints[i]);
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            currentMovePattern.Waypoints.RemoveAt(i);
                            i--;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // サイン波設定
            if (currentMovePattern.Type == MovePatternType.Sin)
            {
                EditorGUILayout.LabelField("Sin Wave Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Sin wave: Y = Amplitude * sin(Frequency * X)", MessageType.Info);
            }

            // 円運動設定
            if (currentMovePattern.Type == MovePatternType.Circle)
            {
                EditorGUILayout.LabelField("Circle Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Circular motion around spawn point", MessageType.Info);
            }
        }

        private void DrawBulletPatternSettings()
        {
            if (currentBulletPattern == null) return;

            EditorGUILayout.LabelField("Bullet Pattern Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            currentBulletPattern.PatternName = EditorGUILayout.TextField("Name", currentBulletPattern.PatternName);
            currentBulletPattern.PatternId = EditorGUILayout.TextField("ID", currentBulletPattern.PatternId);

            EditorGUILayout.Space(10);

            currentBulletPattern.Type = (BulletPatternType)EditorGUILayout.EnumPopup("Type", currentBulletPattern.Type);
            currentBulletPattern.BulletCount = EditorGUILayout.IntField("Bullet Count", currentBulletPattern.BulletCount);
            currentBulletPattern.BulletSpeed = EditorGUILayout.FloatField("Bullet Speed", currentBulletPattern.BulletSpeed);
            currentBulletPattern.FireRate = EditorGUILayout.FloatField("Fire Rate (sec)", currentBulletPattern.FireRate);
            currentBulletPattern.AimAtPlayer = EditorGUILayout.Toggle("Aim at Player", currentBulletPattern.AimAtPlayer);

            // 扇形設定
            if (currentBulletPattern.Type == BulletPatternType.Spread)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Spread Settings", EditorStyles.boldLabel);
                currentBulletPattern.SpreadAngle = EditorGUILayout.Slider("Spread Angle", currentBulletPattern.SpreadAngle, 0f, 360f);
            }
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            // コントロール
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(isPreviewPlaying ? "Stop" : "Play", GUILayout.Width(60)))
                {
                    isPreviewPlaying = !isPreviewPlaying;
                    if (isPreviewPlaying)
                    {
                        lastPreviewTime = EditorApplication.timeSinceStartup;
                    }
                }

                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    previewTime = 0f;
                }

                EditorGUILayout.LabelField($"Time: {previewTime:F2}s", GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // スケール
            previewScale = EditorGUILayout.Slider("Scale", previewScale, 10f, 100f);

            // プレビューエリア
            Rect previewRect = GUILayoutUtility.GetRect(200, 300);
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.15f));

            Vector2 center = new Vector2(previewRect.x + previewRect.width / 2, previewRect.y + previewRect.height / 2);

            // グリッド
            DrawGrid(previewRect);

            if (mode == EditorMode.MovePattern)
            {
                DrawMovePatternPreview(previewRect, center);
            }
            else
            {
                DrawBulletPatternPreview(previewRect, center);
            }
        }

        private void DrawGrid(Rect rect)
        {
            Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            // 縦線・横線
            for (float x = rect.x; x < rect.xMax; x += previewScale)
            {
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), gridColor);
            }
            for (float y = rect.y; y < rect.yMax; y += previewScale)
            {
                EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1), gridColor);
            }
        }

        private void DrawMovePatternPreview(Rect previewRect, Vector2 center)
        {
            if (currentMovePattern == null) return;

            Vector2 pos = center;

            switch (currentMovePattern.Type)
            {
                case MovePatternType.Straight:
                    // 左に直進
                    pos = center + new Vector2(-previewTime * currentMovePattern.Speed * previewScale, 0);
                    break;

                case MovePatternType.Sin:
                    // サイン波
                    float x = -previewTime * currentMovePattern.Speed;
                    float y = Mathf.Sin(previewTime * 3f) * 2f;
                    pos = center + new Vector2(x, y) * previewScale;
                    break;

                case MovePatternType.Circle:
                    // 円運動
                    float angle = previewTime * currentMovePattern.Speed;
                    pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * previewScale * 2;
                    break;

                case MovePatternType.Waypoint:
                    // ウェイポイント追従
                    pos = GetWaypointPosition(center);
                    // ウェイポイントを線で表示
                    DrawWaypointPath(center);
                    break;

                case MovePatternType.Target:
                    // プレイヤー追尾（中心に向かう）
                    Vector2 target = center + new Vector2(-100, 0);
                    Vector2 dir = (target - center).normalized;
                    pos = center + dir * previewTime * currentMovePattern.Speed * previewScale * 0.5f;
                    break;
            }

            // 敵を描画
            EditorGUI.DrawRect(new Rect(pos.x - 10, pos.y - 10, 20, 20), new Color(0.3f, 0.7f, 0.9f));

            // 進行方向を示す線
            Handles.color = Color.yellow;
            Handles.DrawLine(pos, pos + new Vector2(-20, 0));
        }

        private Vector2 GetWaypointPosition(Vector2 center)
        {
            if (currentMovePattern.Waypoints.Count == 0)
                return center;

            float totalDist = 0f;
            List<float> distances = new List<float>();
            distances.Add(0f);

            for (int i = 1; i < currentMovePattern.Waypoints.Count; i++)
            {
                float dist = Vector2.Distance(currentMovePattern.Waypoints[i - 1], currentMovePattern.Waypoints[i]);
                totalDist += dist;
                distances.Add(totalDist);
            }

            if (totalDist == 0) return center + currentMovePattern.Waypoints[0] * previewScale;

            float travelDist = (previewTime * currentMovePattern.Speed) % totalDist;

            for (int i = 1; i < distances.Count; i++)
            {
                if (travelDist <= distances[i])
                {
                    float segmentProgress = (travelDist - distances[i - 1]) / (distances[i] - distances[i - 1]);
                    Vector2 pos = Vector2.Lerp(currentMovePattern.Waypoints[i - 1], currentMovePattern.Waypoints[i], segmentProgress);
                    return center + pos * previewScale;
                }
            }

            return center + currentMovePattern.Waypoints[currentMovePattern.Waypoints.Count - 1] * previewScale;
        }

        private void DrawWaypointPath(Vector2 center)
        {
            if (currentMovePattern.Waypoints.Count < 2) return;

            Handles.color = new Color(0.5f, 0.5f, 0.8f, 0.5f);

            for (int i = 1; i < currentMovePattern.Waypoints.Count; i++)
            {
                Vector2 from = center + currentMovePattern.Waypoints[i - 1] * previewScale;
                Vector2 to = center + currentMovePattern.Waypoints[i] * previewScale;
                Handles.DrawLine(from, to);

                // ウェイポイントマーカー
                EditorGUI.DrawRect(new Rect(from.x - 3, from.y - 3, 6, 6), Color.white);
            }

            // 最後のポイント
            Vector2 last = center + currentMovePattern.Waypoints[currentMovePattern.Waypoints.Count - 1] * previewScale;
            EditorGUI.DrawRect(new Rect(last.x - 3, last.y - 3, 6, 6), Color.white);
        }

        private void DrawBulletPatternPreview(Rect previewRect, Vector2 center)
        {
            if (currentBulletPattern == null) return;

            // 発射元（敵）
            EditorGUI.DrawRect(new Rect(center.x - 10, center.y - 10, 20, 20), new Color(0.9f, 0.3f, 0.3f));

            // 弾幕表示
            int bulletCount = currentBulletPattern.BulletCount;
            float spreadAngle = currentBulletPattern.SpreadAngle * Mathf.Deg2Rad;
            float baseAngle = Mathf.PI; // 左向き

            List<Vector2> bulletPositions = new List<Vector2>();

            switch (currentBulletPattern.Type)
            {
                case BulletPatternType.Single:
                    bulletPositions.Add(GetBulletPosition(center, baseAngle, previewTime));
                    break;

                case BulletPatternType.Spread:
                    float startAngle = baseAngle - spreadAngle / 2;
                    float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0;

                    for (int i = 0; i < bulletCount; i++)
                    {
                        float angle = startAngle + angleStep * i;
                        bulletPositions.Add(GetBulletPosition(center, angle, previewTime));
                    }
                    break;

                case BulletPatternType.Circle:
                    for (int i = 0; i < bulletCount; i++)
                    {
                        float angle = (Mathf.PI * 2 / bulletCount) * i;
                        bulletPositions.Add(GetBulletPosition(center, angle, previewTime));
                    }
                    break;

                case BulletPatternType.Aimed:
                    // プレイヤー方向（左下）
                    Vector2 playerDir = new Vector2(-1, 0.5f).normalized;
                    float aimedAngle = Mathf.Atan2(playerDir.y, playerDir.x);
                    bulletPositions.Add(GetBulletPosition(center, aimedAngle, previewTime));
                    break;

                case BulletPatternType.Random:
                    for (int i = 0; i < bulletCount; i++)
                    {
                        float randAngle = baseAngle + (Mathf.PerlinNoise(i * 0.5f, previewTime) - 0.5f) * spreadAngle;
                        bulletPositions.Add(GetBulletPosition(center, randAngle, previewTime));
                    }
                    break;
            }

            // 弾を描画
            foreach (var pos in bulletPositions)
            {
                if (previewRect.Contains(pos))
                {
                    EditorGUI.DrawRect(new Rect(pos.x - 4, pos.y - 4, 8, 8), Color.yellow);
                }
            }
        }

        private Vector2 GetBulletPosition(Vector2 origin, float angle, float time)
        {
            float dist = time * currentBulletPattern.BulletSpeed * previewScale;
            return origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        }
    }
}
