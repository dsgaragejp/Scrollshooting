using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StageEditor.Data;

namespace StageEditor.Editor
{
    /// <summary>
    /// ステージエディタ メインウィンドウ
    /// </summary>
    public class StageEditorWindow : EditorWindow
    {
        // 現在編集中のデータ
        private Stage currentStage;
        private string currentTimelineId;
        private string selectedEventId;
        private EnemyDatabase enemyDatabase;
        private PatternDatabase patternDatabase;

        // UI状態
        private Vector2 timelineScrollPos;
        private Vector2 eventListScrollPos;
        private Vector2 propertyScrollPos;
        private float timelineZoom = 10f;  // 1秒あたりのピクセル数
        private float targetTimelineZoom = 10f;  // ズームのターゲット値
        private float timelineOffset = 0f;
        private float targetTimelineOffset = 0f;  // オフセットのターゲット値
        private bool isDraggingEvent = false;
        private string draggingEventId;
        private float currentCursorTime = 0f;  // カーソル位置（秒）
        private Rect lastTimelineRect;

        // 慣性スクロール用
        private float scrollVelocity = 0f;
        private double lastUpdateTime;
        private bool isPanning = false;
        private Vector2 panStartPos;
        private float panStartOffset;

        // タッチパッド用スムーズスクロール
        private Vector2 accumulatedScroll = Vector2.zero;
        private float scrollMomentum = 0f;
        private float lastScrollTime = 0f;

        // スムーズアニメーション用（タッチパッド最適化）
        private const float SMOOTH_SPEED = 8f;           // より滑らかな補間
        private const float SCROLL_DECELERATION = 3f;    // ゆるやかな減速
        private const float MIN_VELOCITY = 0.05f;        // より小さな閾値
        private const float SCROLL_SENSITIVITY = 0.8f;   // タッチパッド感度
        private const float MOMENTUM_TRANSFER = 0.92f;   // 慣性の持続率

        // タブ
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Timeline", "Events", "Enemies", "Patterns", "Settings" };

        // スタイル
        private GUIStyle timelineEventStyle;
        private GUIStyle selectedEventStyle;
        private GUIStyle headerStyle;
        private bool stylesInitialized = false;

        // 定数
        private const float TIMELINE_HEIGHT = 200f;
        private const float EVENT_HEIGHT = 24f;
        private const float PROPERTY_PANEL_WIDTH = 300f;

        [MenuItem("Tools/Stage Editor %#e")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageEditorWindow>("Stage Editor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabases();
            if (currentStage == null)
            {
                NewStage();
            }
            lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SaveDatabases();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Min((float)(currentTime - lastUpdateTime), 0.05f); // 最大50msでキャップ
            lastUpdateTime = currentTime;

            bool needsRepaint = false;

            // 慣性スクロール処理（タッチパッド最適化）
            if (Mathf.Abs(scrollVelocity) > MIN_VELOCITY)
            {
                // スムーズな慣性移動
                float movement = scrollVelocity * deltaTime * 50f;
                targetTimelineOffset += movement;
                targetTimelineOffset = Mathf.Max(0, targetTimelineOffset);

                // 自然な減速カーブ（macOS風）
                scrollVelocity *= Mathf.Pow(MOMENTUM_TRANSFER, deltaTime * 60f);

                needsRepaint = true;
            }
            else
            {
                scrollVelocity = 0f;
            }

            // スムーズなオフセット補間（イージング改善）
            float offsetDiff = targetTimelineOffset - timelineOffset;
            if (Mathf.Abs(offsetDiff) > 0.1f)
            {
                // SmoothDamp風の動き
                float smoothFactor = 1f - Mathf.Exp(-SMOOTH_SPEED * deltaTime);
                timelineOffset += offsetDiff * smoothFactor;
                needsRepaint = true;
            }
            else if (Mathf.Abs(offsetDiff) > 0.001f)
            {
                timelineOffset = targetTimelineOffset;
                needsRepaint = true;
            }

            // スムーズなズーム補間
            float zoomDiff = targetTimelineZoom - timelineZoom;
            if (Mathf.Abs(zoomDiff) > 0.01f)
            {
                float smoothFactor = 1f - Mathf.Exp(-SMOOTH_SPEED * deltaTime);
                timelineZoom += zoomDiff * smoothFactor;
                needsRepaint = true;
            }
            else if (Mathf.Abs(zoomDiff) > 0.001f)
            {
                timelineZoom = targetTimelineZoom;
                needsRepaint = true;
            }

            if (needsRepaint)
            {
                Repaint();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            timelineEventStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.8f)) }
            };

            selectedEventStyle = new GUIStyle(timelineEventStyle)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.8f, 0.5f, 0.2f, 0.9f)) }
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            InitStyles();

            // ツールバー
            DrawToolbar();

            EditorGUILayout.Space(5);

            // タブ
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            // メインコンテンツ
            switch (selectedTab)
            {
                case 0:
                    DrawTimelineTab();
                    // Timelineタブの場合はプロパティパネルを下に表示
                    EditorGUILayout.Space(10);
                    DrawPropertyPanel();
                    break;
                case 1: DrawEventsTab(); break;
                case 2: DrawEnemiesTab(); break;
                case 3: DrawPatternsTab(); break;
                case 4: DrawSettingsTab(); break;
            }
        }

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("New Stage", "Create a new stage? Unsaved changes will be lost.", "Yes", "No"))
                    {
                        NewStage();
                    }
                }

                if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    OpenStage();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    SaveStage();
                }

                if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveStageAs();
                }

                GUILayout.FlexibleSpace();

                // ステージ名表示
                if (currentStage != null)
                {
                    GUILayout.Label($"Stage: {currentStage.StageName}", EditorStyles.toolbarButton);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Test Play", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    TestPlay();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Timeline Tab

        private void DrawTimelineTab()
        {
            if (currentStage == null) return;

            // タイムライン選択
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Timeline:", GUILayout.Width(60));

                List<string> timelineNames = new List<string>();
                List<string> timelineIds = new List<string>();
                int selectedIndex = 0;

                for (int i = 0; i < currentStage.Timelines.Count; i++)
                {
                    var timeline = currentStage.Timelines.Values[i];
                    timelineIds.Add(currentStage.Timelines.Keys[i]);
                    timelineNames.Add(timeline.TimelineName);
                    if (currentStage.Timelines.Keys[i] == currentTimelineId)
                    {
                        selectedIndex = i;
                    }
                }

                if (timelineNames.Count > 0)
                {
                    int newIndex = EditorGUILayout.Popup(selectedIndex, timelineNames.ToArray(), GUILayout.Width(150));
                    if (newIndex < timelineIds.Count)
                    {
                        currentTimelineId = timelineIds[newIndex];
                    }
                }

                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    AddNewTimeline();
                }

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    if (currentStage.Timelines.Count > 1)
                    {
                        RemoveCurrentTimeline();
                    }
                }

                GUILayout.FlexibleSpace();

                // ズームスライダー
                EditorGUILayout.LabelField("Zoom:", GUILayout.Width(40));
                float newZoom = EditorGUILayout.Slider(targetTimelineZoom, 5f, 100f, GUILayout.Width(150));
                if (Mathf.Abs(newZoom - targetTimelineZoom) > 0.01f)
                {
                    targetTimelineZoom = newZoom;
                }

                // 操作ヒント
                GUILayout.Label("| 2本指:スクロール  ⌘/Shift+スクロール:ズーム  横スワイプ:パン", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // タイムライン表示
            DrawTimelineView();

            EditorGUILayout.Space(10);

            // イベント追加ボタン
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button($"Add Event at {currentCursorTime:F1}s", GUILayout.Height(30)))
                {
                    AddEventToTimelineAtCursor();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTriangle(Vector2 position, float size, Color color)
        {
            // 三角形マーカーを描画
            Handles.color = color;
            Vector3[] points = new Vector3[]
            {
                new Vector3(position.x - size/2, position.y - size, 0),
                new Vector3(position.x + size/2, position.y - size, 0),
                new Vector3(position.x, position.y, 0)
            };
            Handles.DrawAAConvexPolygon(points);
        }

        private void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawHeart(Rect rect, Color color)
        {
            // ハートマークを描画
            Handles.color = color;
            float cx = rect.center.x;
            float cy = rect.center.y;
            float size = rect.width * 0.5f;

            // 簡易ハート形状
            Vector3[] heartPoints = new Vector3[12];
            heartPoints[0] = new Vector3(cx, cy + size * 0.3f, 0);
            heartPoints[1] = new Vector3(cx - size * 0.5f, cy - size * 0.2f, 0);
            heartPoints[2] = new Vector3(cx - size * 0.8f, cy - size * 0.5f, 0);
            heartPoints[3] = new Vector3(cx - size * 0.8f, cy - size * 0.8f, 0);
            heartPoints[4] = new Vector3(cx - size * 0.5f, cy - size, 0);
            heartPoints[5] = new Vector3(cx, cy - size * 0.7f, 0);
            heartPoints[6] = new Vector3(cx + size * 0.5f, cy - size, 0);
            heartPoints[7] = new Vector3(cx + size * 0.8f, cy - size * 0.8f, 0);
            heartPoints[8] = new Vector3(cx + size * 0.8f, cy - size * 0.5f, 0);
            heartPoints[9] = new Vector3(cx + size * 0.5f, cy - size * 0.2f, 0);
            heartPoints[10] = new Vector3(cx, cy + size * 0.3f, 0);
            heartPoints[11] = heartPoints[0];

            Handles.DrawAAConvexPolygon(heartPoints);
        }

        private void DrawEventIcon(Rect rect, StageEvent evt)
        {
            // イベントタイプに応じたアイコンを描画
            Texture2D iconTexture = null;

            // Prefabからアイコンを取得
            if (!string.IsNullOrEmpty(evt.PrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(evt.PrefabPath);
                if (prefab != null)
                {
                    iconTexture = AssetPreview.GetAssetPreview(prefab);
                }
            }

            // ターゲットIDから敵やFormationのPrefabを取得
            if (iconTexture == null && !string.IsNullOrEmpty(evt.TargetId))
            {
                if (evt.EventType == StageEventType.SpawnSingleEnemy && enemyDatabase.Enemies.ContainsKey(evt.TargetId))
                {
                    var enemy = enemyDatabase.Enemies[evt.TargetId];
                    if (!string.IsNullOrEmpty(enemy.PrefabPath))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemy.PrefabPath);
                        if (prefab != null)
                        {
                            iconTexture = AssetPreview.GetAssetPreview(prefab);
                        }
                    }
                }
            }

            if (iconTexture != null)
            {
                GUI.DrawTexture(rect, iconTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                // デフォルトアイコン（ビルトインアイコンを使用）
                string iconName = GetBuiltinIconName(evt.EventType);
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon != null && icon.image != null)
                {
                    GUI.DrawTexture(rect, icon.image, ScaleMode.ScaleToFit);
                }
                else
                {
                    // フォールバック: テキストアイコン
                    DrawTextIcon(rect, evt.EventType);
                }
            }
        }

        private string GetBuiltinIconName(StageEventType type)
        {
            return type switch
            {
                StageEventType.SpawnSingleEnemy => "d_NavMeshAgent Icon",
                StageEventType.SpawnFormationEnemies => "d_PreMatQuad",
                StageEventType.SpawnBoss => "d_SceneViewFx",
                StageEventType.SetBackground => "d_RenderTexture Icon",
                StageEventType.SetScrollSpeed => "d_SpeedTreeWind Icon",
                StageEventType.NextTimeline => "d_Animation Icon",
                StageEventType.StageClear => "d_Favorite Icon",
                StageEventType.PlaySound => "d_AudioSource Icon",
                StageEventType.PlayBGM => "d_AudioClip Icon",
                StageEventType.ShowMessage => "d_TextAsset Icon",
                StageEventType.CameraEffect => "d_Camera Icon",
                _ => "d_Prefab Icon"
            };
        }

        private void DrawTextIcon(Rect rect, StageEventType type)
        {
            Color bgColor = GetEventTypeColor(type);
            bgColor.a = 1f;
            EditorGUI.DrawRect(rect, bgColor);

            string symbol = type switch
            {
                StageEventType.SpawnSingleEnemy => "E",
                StageEventType.SpawnFormationEnemies => "F",
                StageEventType.SpawnBoss => "B",
                StageEventType.SetBackground => "BG",
                StageEventType.SetScrollSpeed => "SP",
                StageEventType.NextTimeline => ">",
                StageEventType.StageClear => "*",
                StageEventType.PlaySound => "SE",
                StageEventType.PlayBGM => "M",
                StageEventType.ShowMessage => "T",
                StageEventType.CameraEffect => "C",
                _ => "?"
            };

            GUIStyle centeredStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            GUI.Label(rect, symbol, centeredStyle);
        }

        private string GetTargetDisplayName(StageEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TargetId)) return null;

            switch (evt.EventType)
            {
                case StageEventType.SpawnSingleEnemy:
                    if (enemyDatabase.Enemies.ContainsKey(evt.TargetId))
                        return enemyDatabase.Enemies[evt.TargetId].EnemyName;
                    break;
                case StageEventType.SpawnFormationEnemies:
                    if (enemyDatabase.Formations.ContainsKey(evt.TargetId))
                        return enemyDatabase.Formations[evt.TargetId].FormationName;
                    break;
                case StageEventType.SpawnBoss:
                    return $"Boss: {evt.TargetId}";
                case StageEventType.NextTimeline:
                    if (currentStage.Timelines.ContainsKey(evt.TargetId))
                        return $"-> {currentStage.Timelines[evt.TargetId].TimelineName}";
                    break;
            }
            return null;
        }

        private void DrawTimelineView()
        {
            if (string.IsNullOrEmpty(currentTimelineId) || !currentStage.Timelines.ContainsKey(currentTimelineId))
            {
                EditorGUILayout.HelpBox("No timeline selected.", MessageType.Info);
                return;
            }

            var timeline = currentStage.Timelines[currentTimelineId];

            // タイムライン背景
            Rect timelineRect = GUILayoutUtility.GetRect(position.width - 40, TIMELINE_HEIGHT);
            lastTimelineRect = timelineRect;
            EditorGUI.DrawRect(timelineRect, new Color(0.15f, 0.15f, 0.15f));

            // 時間目盛り
            DrawTimeRuler(timelineRect);

            // イベントサイズ
            const float eventWidth = 90f;
            const float eventHeight = 50f;

            // 重なり回避のためのスロット計算
            var ySlots = CalculateEventYSlots(timeline, eventWidth);

            // イベント表示
            foreach (var timelineEvent in timeline.Events)
            {
                if (!currentStage.Events.ContainsKey(timelineEvent.EventId)) continue;

                var evt = currentStage.Events[timelineEvent.EventId];
                float xPos = timelineRect.x + (timelineEvent.Time * timelineZoom) - timelineOffset;

                if (xPos < timelineRect.x - 100 || xPos > timelineRect.xMax) continue;

                // スロットに基づいてY位置を決定
                int slot = ySlots.ContainsKey(timelineEvent.EventId) ? ySlots[timelineEvent.EventId] : 0;
                float yOffset = slot * (eventHeight + 5); // 5pxの余白

                Rect eventRect = new Rect(xPos, timelineRect.y + 25 + yOffset, eventWidth, eventHeight);

                // 背景描画
                bool isSelected = (timelineEvent.EventId == selectedEventId);
                Color bgColor = isSelected ? new Color(0.9f, 0.6f, 0.2f, 0.9f) : GetEventTypeColor(evt.EventType);
                EditorGUI.DrawRect(eventRect, bgColor);

                // 枠線
                if (isSelected)
                {
                    DrawRectOutline(eventRect, Color.white, 2);
                }

                // アイコン描画
                Rect iconRect = new Rect(eventRect.x + 2, eventRect.y + 2, 24, 24);
                DrawEventIcon(iconRect, evt);

                // イベント名
                Rect labelRect = new Rect(eventRect.x + 28, eventRect.y + 2, eventRect.width - 32, 14);
                GUI.Label(labelRect, evt.EventName, EditorStyles.miniLabel);

                // ターゲット名（敵や編隊の名前）
                string targetName = GetTargetDisplayName(evt);
                if (!string.IsNullOrEmpty(targetName))
                {
                    Rect targetRect = new Rect(eventRect.x + 2, eventRect.y + 28, eventRect.width - 4, 14);
                    GUI.Label(targetRect, targetName, EditorStyles.miniLabel);
                }

                // 時間表示
                Rect timeRect = new Rect(eventRect.x + 2, eventRect.y + eventHeight - 14, eventRect.width - 4, 12);
                GUI.Label(timeRect, $"{timelineEvent.Time:F1}s", EditorStyles.miniLabel);

                // お気に入りマーク（右上）
                if (evt.IsFavorite)
                {
                    Rect heartRect = new Rect(eventRect.xMax - 16, eventRect.y + 2, 14, 14);
                    DrawHeart(heartRect, new Color(1f, 0.3f, 0.4f));
                }

                // クリック処理
                if (Event.current.type == EventType.MouseDown && eventRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        selectedEventId = timelineEvent.EventId;
                        currentCursorTime = timelineEvent.Time;
                        Event.current.Use();
                    }
                }

                // ドラッグ処理
                if (Event.current.type == EventType.MouseDrag && eventRect.Contains(Event.current.mousePosition))
                {
                    isDraggingEvent = true;
                    draggingEventId = timelineEvent.EventId;
                }
            }

            // 赤いカーソルライン描画
            float cursorX = timelineRect.x + (currentCursorTime * timelineZoom) - timelineOffset;
            if (cursorX >= timelineRect.x && cursorX <= timelineRect.xMax)
            {
                EditorGUI.DrawRect(new Rect(cursorX - 1, timelineRect.y, 2, timelineRect.height), Color.red);
                // カーソル上部に三角形マーカー
                DrawTriangle(new Vector2(cursorX, timelineRect.y), 8, Color.red);
            }

            // カーソル時間表示
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Cursor: {currentCursorTime:F2}s", GUILayout.Width(100));
            currentCursorTime = EditorGUILayout.Slider(currentCursorTime, 0f, 300f);
            EditorGUILayout.EndHorizontal();

            // タイムライン上でクリックしてカーソル移動
            if (Event.current.type == EventType.MouseDown && timelineRect.Contains(Event.current.mousePosition))
            {
                // イベント上でなければカーソルを移動
                bool clickedOnEvent = false;
                foreach (var timelineEvent in timeline.Events)
                {
                    if (!currentStage.Events.ContainsKey(timelineEvent.EventId)) continue;
                    float xPos = timelineRect.x + (timelineEvent.Time * timelineZoom) - timelineOffset;
                    int slot = ySlots.ContainsKey(timelineEvent.EventId) ? ySlots[timelineEvent.EventId] : 0;
                    float yOffset = slot * (eventHeight + 5);
                    Rect eventRect = new Rect(xPos, timelineRect.y + 25 + yOffset, eventWidth, eventHeight);
                    if (eventRect.Contains(Event.current.mousePosition))
                    {
                        clickedOnEvent = true;
                        break;
                    }
                }

                if (!clickedOnEvent)
                {
                    currentCursorTime = (Event.current.mousePosition.x - timelineRect.x + timelineOffset) / timelineZoom;
                    currentCursorTime = Mathf.Max(0, currentCursorTime);
                    selectedEventId = null;  // 選択解除
                    Repaint();
                }
            }

            // ドラッグ中のイベント時間更新
            if (isDraggingEvent && Event.current.type == EventType.MouseDrag)
            {
                var timelineEvent = timeline.Events.Find(e => e.EventId == draggingEventId);
                if (timelineEvent != null)
                {
                    float newTime = (Event.current.mousePosition.x - timelineRect.x + timelineOffset) / timelineZoom;
                    timelineEvent.Time = Mathf.Max(0, newTime);
                    currentCursorTime = timelineEvent.Time;  // カーソルも追従
                    Repaint();
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (isDraggingEvent)
                {
                    timeline.SortEvents();
                }
                isDraggingEvent = false;
                draggingEventId = null;

                // パン終了時に慣性を適用
                if (isPanning)
                {
                    // ドラッグ中の速度を慣性として引き継ぐ
                    // scrollVelocityは既にドラッグ中に計算済み
                    isPanning = false;
                }
            }

            // 中ボタンまたは右ボタンでパン開始
            if (Event.current.type == EventType.MouseDown &&
                (Event.current.button == 2 || Event.current.button == 1) &&
                timelineRect.Contains(Event.current.mousePosition))
            {
                isPanning = true;
                panStartPos = Event.current.mousePosition;
                panStartOffset = timelineOffset; // 現在値を使用（よりレスポンシブ）
                scrollVelocity = 0f;  // 慣性をリセット
                Event.current.Use();
            }

            // パン中のドラッグ処理
            if (isPanning && Event.current.type == EventType.MouseDrag)
            {
                Vector2 currentPos = Event.current.mousePosition;
                float delta = panStartPos.x - currentPos.x;

                // 直接オフセットを更新（ラグなし）
                timelineOffset = Mathf.Max(0, panStartOffset + delta);
                targetTimelineOffset = timelineOffset;

                // ドラッグ速度から慣性を計算
                scrollVelocity = delta * 0.02f;

                Event.current.Use();
                Repaint();
            }

            // スクロールホイール/タッチパッドで慣性スクロール
            if (Event.current.type == EventType.ScrollWheel && timelineRect.Contains(Event.current.mousePosition))
            {
                Vector2 scrollDelta = Event.current.delta;
                float currentScrollTime = (float)EditorApplication.timeSinceStartup;

                // Shift, Alt, または Command(⌘) キーでズーム
                if (Event.current.shift || Event.current.alt || Event.current.command)
                {
                    // マウス位置を中心にズーム
                    float mouseTime = (Event.current.mousePosition.x - timelineRect.x + timelineOffset) / timelineZoom;

                    // タッチパッド向けの細かいズーム
                    float zoomDelta = scrollDelta.y * 0.3f * SCROLL_SENSITIVITY;
                    targetTimelineZoom -= zoomDelta;
                    targetTimelineZoom = Mathf.Clamp(targetTimelineZoom, 5f, 100f);

                    // ズーム後もマウス位置が同じ時間を指すようにオフセット調整
                    targetTimelineOffset = mouseTime * targetTimelineZoom - (Event.current.mousePosition.x - timelineRect.x);
                    targetTimelineOffset = Mathf.Max(0, targetTimelineOffset);
                }
                else
                {
                    // タッチパッド向けスムーズスクロール
                    // 連続したスクロールイベントを検出して慣性を蓄積
                    float timeSinceLastScroll = currentScrollTime - lastScrollTime;

                    if (timeSinceLastScroll < 0.1f)
                    {
                        // 連続スクロール中 - 速度を蓄積
                        scrollVelocity += scrollDelta.y * SCROLL_SENSITIVITY;
                    }
                    else
                    {
                        // 新しいスクロール開始
                        scrollVelocity = scrollDelta.y * SCROLL_SENSITIVITY;
                    }

                    // 速度の上限
                    scrollVelocity = Mathf.Clamp(scrollVelocity, -30f, 30f);

                    // 水平スクロールもサポート（タッチパッドの横スワイプ）
                    if (Mathf.Abs(scrollDelta.x) > Mathf.Abs(scrollDelta.y) * 0.5f)
                    {
                        targetTimelineOffset += scrollDelta.x * SCROLL_SENSITIVITY * 5f;
                        targetTimelineOffset = Mathf.Max(0, targetTimelineOffset);
                    }
                }

                lastScrollTime = currentScrollTime;
                Event.current.Use();
            }
        }

        private void DrawTimeRuler(Rect timelineRect)
        {
            float startTime = timelineOffset / timelineZoom;
            float endTime = (timelineOffset + timelineRect.width) / timelineZoom;

            for (float t = Mathf.Floor(startTime); t <= endTime; t += 1f)
            {
                float xPos = timelineRect.x + (t * timelineZoom) - timelineOffset;

                // 秒ごとの線
                EditorGUI.DrawRect(new Rect(xPos, timelineRect.y, 1, timelineRect.height), new Color(0.3f, 0.3f, 0.3f));

                // 時間ラベル（5秒ごと）
                if (t % 5 == 0)
                {
                    EditorGUI.DrawRect(new Rect(xPos, timelineRect.y, 2, timelineRect.height), new Color(0.5f, 0.5f, 0.5f));
                    GUI.Label(new Rect(xPos + 2, timelineRect.y, 40, 20), $"{t:0}s");
                }
            }
        }

        /// <summary>
        /// 重なりを避けるためのY位置スロットを計算
        /// </summary>
        private Dictionary<string, int> CalculateEventYSlots(StageTimeline timeline, float eventWidth)
        {
            var slots = new Dictionary<string, int>();
            var sortedEvents = timeline.Events.OrderBy(e => e.Time).ToList();

            // 各スロットの終了X位置を追跡
            var slotEndPositions = new List<float>();

            foreach (var timelineEvent in sortedEvents)
            {
                float xPos = timelineEvent.Time * timelineZoom;

                // 空いているスロットを探す
                int assignedSlot = -1;
                for (int i = 0; i < slotEndPositions.Count; i++)
                {
                    if (xPos >= slotEndPositions[i])
                    {
                        assignedSlot = i;
                        break;
                    }
                }

                // 空きスロットがなければ新しいスロットを作成
                if (assignedSlot == -1)
                {
                    assignedSlot = slotEndPositions.Count;
                    slotEndPositions.Add(0);
                }

                // スロットを割り当て
                slots[timelineEvent.EventId] = assignedSlot;
                slotEndPositions[assignedSlot] = xPos + eventWidth + 5; // 5pxの余白
            }

            return slots;
        }

        private Color GetEventTypeColor(StageEventType type)
        {
            return type switch
            {
                StageEventType.SpawnSingleEnemy => new Color(0.3f, 0.6f, 0.9f, 0.8f),
                StageEventType.SpawnFormationEnemies => new Color(0.3f, 0.8f, 0.5f, 0.8f),
                StageEventType.SpawnBoss => new Color(0.9f, 0.3f, 0.3f, 0.8f),
                StageEventType.SetBackground => new Color(0.6f, 0.4f, 0.8f, 0.8f),
                StageEventType.SetScrollSpeed => new Color(0.8f, 0.6f, 0.3f, 0.8f),
                StageEventType.NextTimeline => new Color(0.5f, 0.5f, 0.5f, 0.8f),
                StageEventType.StageClear => new Color(0.9f, 0.9f, 0.3f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.8f)
            };
        }

        #endregion

        #region Events Tab

        private void DrawEventsTab()
        {
            if (currentStage == null) return;

            EditorGUILayout.LabelField("Event Definitions", headerStyle);
            EditorGUILayout.Space(5);

            // イベント追加
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Add Event", GUILayout.Height(25)))
                {
                    ShowAddEventMenu();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // イベント一覧
            eventListScrollPos = EditorGUILayout.BeginScrollView(eventListScrollPos);
            {
                for (int i = 0; i < currentStage.Events.Count; i++)
                {
                    string eventId = currentStage.Events.Keys[i];
                    var evt = currentStage.Events.Values[i];

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        // 選択状態の背景色
                        if (eventId == selectedEventId)
                        {
                            EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), new Color(0.3f, 0.5f, 0.8f, 0.3f));
                        }

                        // イベントタイプアイコン
                        GUILayout.Label(GetEventTypeIcon(evt.EventType), GUILayout.Width(20));

                        // イベント名
                        if (GUILayout.Button(evt.EventName, EditorStyles.label, GUILayout.ExpandWidth(true)))
                        {
                            selectedEventId = eventId;
                        }

                        // イベントタイプ
                        GUILayout.Label(evt.EventType.ToString(), EditorStyles.miniLabel, GUILayout.Width(120));

                        // 削除ボタン
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Event", $"Delete event '{evt.EventName}'?", "Yes", "No"))
                            {
                                RemoveEvent(eventId);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private string GetEventTypeIcon(StageEventType type)
        {
            return type switch
            {
                StageEventType.SpawnSingleEnemy => "E",
                StageEventType.SpawnFormationEnemies => "F",
                StageEventType.SpawnBoss => "B",
                StageEventType.SetBackground => "BG",
                StageEventType.SetScrollSpeed => "SP",
                StageEventType.NextTimeline => ">",
                StageEventType.StageClear => "*",
                _ => "?"
            };
        }

        private void ShowAddEventMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (StageEventType type in System.Enum.GetValues(typeof(StageEventType)))
            {
                StageEventType capturedType = type;
                menu.AddItem(new GUIContent(type.ToString()), false, () => AddNewEvent(capturedType));
            }

            menu.ShowAsContext();
        }

        #endregion

        #region Enemies Tab

        private void DrawEnemiesTab()
        {
            EditorGUILayout.LabelField("Enemy Database", headerStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Add Enemy", GUILayout.Height(25)))
                {
                    AddNewEnemy();
                }
                if (GUILayout.Button("+ Add Formation", GUILayout.Height(25)))
                {
                    AddNewFormation();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 敵一覧
            EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
            for (int i = 0; i < enemyDatabase.Enemies.Count; i++)
            {
                var enemy = enemyDatabase.Enemies.Values[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.Label(enemy.EnemyName, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"HP: {enemy.Hp}", GUILayout.Width(60));
                    GUILayout.Label($"Score: {enemy.Score}", GUILayout.Width(80));
                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
                        // TODO: 敵エディタを開く
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // 編隊一覧
            EditorGUILayout.LabelField("Formations", EditorStyles.boldLabel);
            for (int i = 0; i < enemyDatabase.Formations.Count; i++)
            {
                var formation = enemyDatabase.Formations.Values[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.Label(formation.FormationName, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"Members: {formation.Members.Count}", GUILayout.Width(80));
                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
                        // TODO: 編隊エディタを開く
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Patterns Tab

        private void DrawPatternsTab()
        {
            EditorGUILayout.LabelField("Pattern Database", headerStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Add Move Pattern", GUILayout.Height(25)))
                {
                    AddNewMovePattern();
                }
                if (GUILayout.Button("+ Add Bullet Pattern", GUILayout.Height(25)))
                {
                    AddNewBulletPattern();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 移動パターン一覧
            EditorGUILayout.LabelField("Move Patterns", EditorStyles.boldLabel);
            for (int i = 0; i < patternDatabase.MovePatterns.Count; i++)
            {
                var pattern = patternDatabase.MovePatterns.Values[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.Label(pattern.PatternName, GUILayout.ExpandWidth(true));
                    GUILayout.Label(pattern.Type.ToString(), GUILayout.Width(80));
                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
                        // TODO: パターンエディタを開く
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // 弾幕パターン一覧
            EditorGUILayout.LabelField("Bullet Patterns", EditorStyles.boldLabel);
            for (int i = 0; i < patternDatabase.BulletPatterns.Count; i++)
            {
                var pattern = patternDatabase.BulletPatterns.Values[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.Label(pattern.PatternName, GUILayout.ExpandWidth(true));
                    GUILayout.Label(pattern.Type.ToString(), GUILayout.Width(80));
                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
                        // TODO: パターンエディタを開く
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab()
        {
            if (currentStage == null) return;

            EditorGUILayout.LabelField("Stage Settings", headerStyle);
            EditorGUILayout.Space(10);

            // ステージ情報
            currentStage.StageId = EditorGUILayout.TextField("Stage ID", currentStage.StageId);
            currentStage.StageName = EditorGUILayout.TextField("Stage Name", currentStage.StageName);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Meta Information", EditorStyles.boldLabel);

            currentStage.Meta.Author = EditorGUILayout.TextField("Author", currentStage.Meta.Author);
            currentStage.Meta.Difficulty = EditorGUILayout.IntSlider("Difficulty", currentStage.Meta.Difficulty, 1, 10);
            currentStage.Meta.Description = EditorGUILayout.TextArea(currentStage.Meta.Description, GUILayout.Height(60));
            currentStage.Meta.Version = EditorGUILayout.TextField("Version", currentStage.Meta.Version);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Created: {currentStage.Meta.CreatedAt}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Updated: {currentStage.Meta.UpdatedAt}", EditorStyles.miniLabel);
        }

        #endregion

        #region Property Panel

        private void DrawPropertyPanel()
        {
            EditorGUILayout.LabelField("Properties", headerStyle);
            EditorGUILayout.Space(5);

            propertyScrollPos = EditorGUILayout.BeginScrollView(propertyScrollPos);
            {
                if (!string.IsNullOrEmpty(selectedEventId) && currentStage.Events.ContainsKey(selectedEventId))
                {
                    DrawEventProperties(currentStage.Events[selectedEventId]);
                }
                else
                {
                    EditorGUILayout.HelpBox("Select an event to edit its properties.", MessageType.Info);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawEventProperties(StageEvent evt)
        {
            EditorGUILayout.LabelField("Event Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            evt.EventName = EditorGUILayout.TextField("Name", evt.EventName);
            evt.EventType = (StageEventType)EditorGUILayout.EnumPopup("Type", evt.EventType);

            // お気に入り設定
            EditorGUILayout.BeginHorizontal();
            evt.IsFavorite = EditorGUILayout.Toggle("Favorite", evt.IsFavorite);
            if (evt.IsFavorite)
            {
                GUILayout.Label("♥", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.3f, 0.4f) }, fontSize = 14 });
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // カスタムPrefabパス（アイコン表示用）
            EditorGUILayout.BeginHorizontal();
            evt.PrefabPath = EditorGUILayout.TextField("Prefab Path", evt.PrefabPath);
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                string path = EditorUtility.OpenFilePanel("Select Prefab", "Assets", "prefab");
                if (!string.IsNullOrEmpty(path))
                {
                    // Assetsからの相対パスに変換
                    if (path.StartsWith(Application.dataPath))
                    {
                        evt.PrefabPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Prefabプレビュー表示
            if (!string.IsNullOrEmpty(evt.PrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(evt.PrefabPath);
                if (prefab != null)
                {
                    var preview = AssetPreview.GetAssetPreview(prefab);
                    if (preview != null)
                    {
                        Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                        GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                    }
                }
            }

            EditorGUILayout.Space(10);

            // タイプ別プロパティ
            switch (evt.EventType)
            {
                case StageEventType.SpawnSingleEnemy:
                case StageEventType.SpawnFormationEnemies:
                    DrawSpawnEventProperties(evt);
                    break;

                case StageEventType.SpawnBoss:
                    DrawBossEventProperties(evt);
                    break;

                case StageEventType.SetScrollSpeed:
                    evt.ScrollSpeed = EditorGUILayout.FloatField("Scroll Speed", evt.ScrollSpeed);
                    break;

                case StageEventType.SetBackground:
                    evt.TargetId = EditorGUILayout.TextField("Background ID", evt.TargetId);
                    break;

                case StageEventType.NextTimeline:
                    DrawTimelineSelectProperty(evt);
                    break;

                case StageEventType.PlaySound:
                case StageEventType.PlayBGM:
                    evt.SoundId = EditorGUILayout.TextField("Sound ID", evt.SoundId);
                    evt.Volume = EditorGUILayout.Slider("Volume", evt.Volume, 0f, 1f);
                    break;

                case StageEventType.ShowMessage:
                    evt.Message = EditorGUILayout.TextArea(evt.Message, GUILayout.Height(60));
                    evt.MessageDuration = EditorGUILayout.FloatField("Duration", evt.MessageDuration);
                    break;
            }

            EditorGUILayout.Space(10);

            // 破壊時イベント設定
            if (evt.EventType == StageEventType.SpawnSingleEnemy ||
                evt.EventType == StageEventType.SpawnFormationEnemies ||
                evt.EventType == StageEventType.SpawnBoss)
            {
                EditorGUILayout.LabelField("On Destroy Event", EditorStyles.boldLabel);
                evt.HasOnDestroyEvent = EditorGUILayout.Toggle("Has On Destroy Event", evt.HasOnDestroyEvent);

                if (evt.HasOnDestroyEvent)
                {
                    evt.OnDestroyEventType = (StageEventType)EditorGUILayout.EnumPopup("Event Type", evt.OnDestroyEventType);
                    evt.OnDestroyEventId = EditorGUILayout.TextField("Event ID", evt.OnDestroyEventId);
                    evt.OnDestroyEventCondition = EditorGUILayout.FloatField("Time Condition", evt.OnDestroyEventCondition);
                }
            }
        }

        private void DrawSpawnEventProperties(StageEvent evt)
        {
            EditorGUILayout.LabelField("Spawn Properties", EditorStyles.boldLabel);

            // 敵/編隊選択
            if (evt.EventType == StageEventType.SpawnSingleEnemy)
            {
                DrawEnemySelectProperty(evt);
            }
            else
            {
                DrawFormationSelectProperty(evt);
            }

            evt.SpawnPosition = EditorGUILayout.Vector2Field("Spawn Position", evt.SpawnPosition);
        }

        private void DrawBossEventProperties(StageEvent evt)
        {
            EditorGUILayout.LabelField("Boss Properties", EditorStyles.boldLabel);
            evt.TargetId = EditorGUILayout.TextField("Boss ID", evt.TargetId);
            evt.SpawnPosition = EditorGUILayout.Vector2Field("Spawn Position", evt.SpawnPosition);
        }

        private void DrawEnemySelectProperty(StageEvent evt)
        {
            List<string> names = new List<string> { "(None)" };
            List<string> ids = new List<string> { "" };

            for (int i = 0; i < enemyDatabase.Enemies.Count; i++)
            {
                ids.Add(enemyDatabase.Enemies.Keys[i]);
                names.Add(enemyDatabase.Enemies.Values[i].EnemyName);
            }

            int selectedIndex = ids.IndexOf(evt.TargetId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Enemy", selectedIndex, names.ToArray());
            evt.TargetId = ids[selectedIndex];
        }

        private void DrawFormationSelectProperty(StageEvent evt)
        {
            List<string> names = new List<string> { "(None)" };
            List<string> ids = new List<string> { "" };

            for (int i = 0; i < enemyDatabase.Formations.Count; i++)
            {
                ids.Add(enemyDatabase.Formations.Keys[i]);
                names.Add(enemyDatabase.Formations.Values[i].FormationName);
            }

            int selectedIndex = ids.IndexOf(evt.TargetId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Formation", selectedIndex, names.ToArray());
            evt.TargetId = ids[selectedIndex];
        }

        private void DrawTimelineSelectProperty(StageEvent evt)
        {
            List<string> names = new List<string> { "(None)" };
            List<string> ids = new List<string> { "" };

            for (int i = 0; i < currentStage.Timelines.Count; i++)
            {
                ids.Add(currentStage.Timelines.Keys[i]);
                names.Add(currentStage.Timelines.Values[i].TimelineName);
            }

            int selectedIndex = ids.IndexOf(evt.TargetId);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup("Next Timeline", selectedIndex, names.ToArray());
            evt.TargetId = ids[selectedIndex];
        }

        #endregion

        #region Data Operations

        private void NewStage()
        {
            currentStage = new Stage();
            var initialTimeline = new StageTimeline { TimelineName = "Main" };
            currentStage.Timelines.Add(initialTimeline.TimelineId, initialTimeline);
            currentTimelineId = initialTimeline.TimelineId;
            selectedEventId = null;
        }

        private void OpenStage()
        {
            string path = EditorUtility.OpenFilePanel("Open Stage", StageSerializer.DefaultSavePath, "stage.json");
            if (!string.IsNullOrEmpty(path))
            {
                var stage = StageSerializer.LoadStage(path);
                if (stage != null)
                {
                    currentStage = stage;
                    if (currentStage.Timelines.Count > 0)
                    {
                        currentTimelineId = currentStage.Timelines.Keys[0];
                    }
                    selectedEventId = null;
                }
            }
        }

        private void SaveStage()
        {
            if (currentStage != null)
            {
                StageSerializer.SaveStage(currentStage);
                AssetDatabase.Refresh();
            }
        }

        private void SaveStageAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Stage As", StageSerializer.DefaultSavePath,
                currentStage?.StageName ?? "stage", "stage.json");
            if (!string.IsNullOrEmpty(path))
            {
                StageSerializer.SaveStage(currentStage, path);
                AssetDatabase.Refresh();
            }
        }

        private void LoadDatabases()
        {
            enemyDatabase = StageSerializer.LoadEnemyDatabase();
            patternDatabase = StageSerializer.LoadPatternDatabase();
        }

        private void SaveDatabases()
        {
            StageSerializer.SaveEnemyDatabase(enemyDatabase);
            StageSerializer.SavePatternDatabase(patternDatabase);
        }

        private void AddNewTimeline()
        {
            var timeline = new StageTimeline();
            currentStage.Timelines.Add(timeline.TimelineId, timeline);
            currentTimelineId = timeline.TimelineId;
        }

        private void RemoveCurrentTimeline()
        {
            if (!string.IsNullOrEmpty(currentTimelineId))
            {
                currentStage.Timelines.Remove(currentTimelineId);
                if (currentStage.Timelines.Count > 0)
                {
                    currentTimelineId = currentStage.Timelines.Keys[0];
                }
                else
                {
                    currentTimelineId = null;
                }
            }
        }

        private void AddNewEvent(StageEventType type)
        {
            var evt = new StageEvent(type);
            currentStage.Events.Add(evt.EventId, evt);
            selectedEventId = evt.EventId;
        }

        private void AddEventToTimeline()
        {
            AddEventToTimelineAtCursor();
        }

        private void AddEventToTimelineAtCursor()
        {
            if (string.IsNullOrEmpty(currentTimelineId)) return;

            // 新しいイベントを作成
            var evt = new StageEvent(StageEventType.SpawnSingleEnemy);
            currentStage.Events.Add(evt.EventId, evt);

            // タイムラインに追加（カーソル位置に）
            var timeline = currentStage.Timelines[currentTimelineId];
            var timelineEvent = new TimelineEvent(currentCursorTime, evt.EventId);
            timeline.Events.Add(timelineEvent);
            timeline.SortEvents();

            selectedEventId = evt.EventId;
        }

        private void RemoveEvent(string eventId)
        {
            // タイムラインからも削除
            foreach (var kvp in currentStage.Timelines.GetEnumerator())
            {
                kvp.Value.Events.RemoveAll(e => e.EventId == eventId);
            }

            currentStage.Events.Remove(eventId);

            if (selectedEventId == eventId)
            {
                selectedEventId = null;
            }
        }

        private void AddNewEnemy()
        {
            var enemy = new Enemy();
            enemyDatabase.Enemies.Add(enemy.EnemyId, enemy);
        }

        private void AddNewFormation()
        {
            var formation = new EnemyFormation();
            enemyDatabase.Formations.Add(formation.FormationId, formation);
        }

        private void AddNewMovePattern()
        {
            var pattern = new MovePattern();
            patternDatabase.MovePatterns.Add(pattern.PatternId, pattern);
        }

        private void AddNewBulletPattern()
        {
            var pattern = new BulletPattern();
            patternDatabase.BulletPatterns.Add(pattern.PatternId, pattern);
        }

        private void TestPlay()
        {
            // 保存してからプレイモードに入る
            SaveStage();
            EditorApplication.isPlaying = true;
        }

        #endregion
    }
}
