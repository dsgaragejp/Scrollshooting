using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using StageEditor.Data;

namespace StageEditor.Runtime
{
    /// <summary>
    /// ステージ実行マネージャー
    /// タイムラインに従ってイベントを実行する
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string stageFilePath;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool debugMode = false;

        [Header("References")]
        [SerializeField] private Transform enemyContainer;
        [SerializeField] private Transform bulletContainer;

        [Header("Events")]
        public UnityEvent OnStageStart;
        public UnityEvent OnStageClear;
        public UnityEvent<string> OnTimelineChanged;
        public UnityEvent<StageEvent> OnEventExecuted;

        // ステージデータ
        private Stage currentStage;
        private EnemyDatabase enemyDatabase;
        private PatternDatabase patternDatabase;

        // 実行状態
        private string currentTimelineId;
        private StageTimeline currentTimeline;
        private float timelineTime;
        private int currentEventIndex;
        private bool isRunning;
        private bool isPaused;

        // 敵管理
        private Dictionary<string, List<GameObject>> activeEnemies = new Dictionary<string, List<GameObject>>();
        private Dictionary<string, EnemyController> enemyControllers = new Dictionary<string, EnemyController>();

        public Stage CurrentStage => currentStage;
        public float CurrentTime => timelineTime;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (enemyContainer == null)
            {
                var go = new GameObject("EnemyContainer");
                go.transform.SetParent(transform);
                enemyContainer = go.transform;
            }

            if (bulletContainer == null)
            {
                var go = new GameObject("BulletContainer");
                go.transform.SetParent(transform);
                bulletContainer = go.transform;
            }
        }

        private void Start()
        {
            LoadDatabases();

            if (autoStart && !string.IsNullOrEmpty(stageFilePath))
            {
                LoadAndStartStage(stageFilePath);
            }
        }

        private void Update()
        {
            if (!isRunning || isPaused) return;

            UpdateTimeline();
        }

        #region Stage Loading

        /// <summary>
        /// データベースを読み込み
        /// </summary>
        private void LoadDatabases()
        {
            enemyDatabase = StageSerializer.LoadEnemyDatabase();
            patternDatabase = StageSerializer.LoadPatternDatabase();
        }

        /// <summary>
        /// ステージを読み込んで開始
        /// </summary>
        public void LoadAndStartStage(string filePath)
        {
            currentStage = StageSerializer.LoadStage(filePath);
            if (currentStage == null)
            {
                Debug.LogError($"Failed to load stage: {filePath}");
                return;
            }

            StartStage();
        }

        /// <summary>
        /// ステージデータを直接設定して開始
        /// </summary>
        public void SetAndStartStage(Stage stage)
        {
            currentStage = stage;
            StartStage();
        }

        /// <summary>
        /// ステージを開始
        /// </summary>
        public void StartStage()
        {
            if (currentStage == null || currentStage.Timelines.Count == 0)
            {
                Debug.LogError("No valid stage data");
                return;
            }

            // 最初のタイムラインを開始
            currentTimelineId = currentStage.Timelines.Keys[0];
            StartTimeline(currentTimelineId);

            isRunning = true;
            isPaused = false;

            OnStageStart?.Invoke();

            if (debugMode)
            {
                Debug.Log($"Stage started: {currentStage.StageName}");
            }
        }

        /// <summary>
        /// 指定タイムラインを開始
        /// </summary>
        private void StartTimeline(string timelineId)
        {
            if (!currentStage.Timelines.ContainsKey(timelineId))
            {
                Debug.LogError($"Timeline not found: {timelineId}");
                return;
            }

            currentTimelineId = timelineId;
            currentTimeline = currentStage.Timelines[timelineId];
            currentTimeline.SortEvents();

            timelineTime = 0f;
            currentEventIndex = 0;

            OnTimelineChanged?.Invoke(currentTimeline.TimelineName);

            if (debugMode)
            {
                Debug.Log($"Timeline started: {currentTimeline.TimelineName}");
            }
        }

        #endregion

        #region Timeline Execution

        /// <summary>
        /// タイムライン更新
        /// </summary>
        private void UpdateTimeline()
        {
            if (currentTimeline == null) return;

            timelineTime += Time.deltaTime;

            // イベント実行チェック
            while (currentEventIndex < currentTimeline.Events.Count)
            {
                var timelineEvent = currentTimeline.Events[currentEventIndex];

                if (timelineEvent.Time <= timelineTime)
                {
                    ExecuteEvent(timelineEvent.EventId);
                    currentEventIndex++;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// イベントを実行
        /// </summary>
        private void ExecuteEvent(string eventId)
        {
            if (!currentStage.Events.ContainsKey(eventId))
            {
                Debug.LogWarning($"Event not found: {eventId}");
                return;
            }

            var evt = currentStage.Events[eventId];

            if (debugMode)
            {
                Debug.Log($"[{timelineTime:F2}s] Executing event: {evt.EventName} ({evt.EventType})");
            }

            switch (evt.EventType)
            {
                case StageEventType.SpawnSingleEnemy:
                    SpawnEnemy(evt);
                    break;

                case StageEventType.SpawnFormationEnemies:
                    SpawnFormation(evt);
                    break;

                case StageEventType.SpawnBoss:
                    SpawnBoss(evt);
                    break;

                case StageEventType.SetBackground:
                    SetBackground(evt);
                    break;

                case StageEventType.SetScrollSpeed:
                    SetScrollSpeed(evt);
                    break;

                case StageEventType.NextTimeline:
                    StartTimeline(evt.TargetId);
                    break;

                case StageEventType.StageClear:
                    StageClear();
                    break;

                case StageEventType.PlaySound:
                    PlaySound(evt);
                    break;

                case StageEventType.PlayBGM:
                    PlayBGM(evt);
                    break;

                case StageEventType.ShowMessage:
                    ShowMessage(evt);
                    break;

                case StageEventType.CameraEffect:
                    CameraEffect(evt);
                    break;
            }

            OnEventExecuted?.Invoke(evt);
        }

        #endregion

        #region Event Implementations

        private void SpawnEnemy(StageEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TargetId)) return;

            if (!enemyDatabase.Enemies.ContainsKey(evt.TargetId))
            {
                Debug.LogWarning($"Enemy not found in database: {evt.TargetId}");
                return;
            }

            var enemyData = enemyDatabase.Enemies[evt.TargetId];
            SpawnEnemyInstance(enemyData, evt.SpawnPosition, evt);
        }

        private void SpawnFormation(StageEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TargetId)) return;

            if (!enemyDatabase.Formations.ContainsKey(evt.TargetId))
            {
                Debug.LogWarning($"Formation not found in database: {evt.TargetId}");
                return;
            }

            var formation = enemyDatabase.Formations[evt.TargetId];
            StartCoroutine(SpawnFormationCoroutine(formation, evt));
        }

        private IEnumerator SpawnFormationCoroutine(EnemyFormation formation, StageEvent evt)
        {
            List<GameObject> formationEnemies = new List<GameObject>();

            foreach (var member in formation.Members)
            {
                if (member.SpawnDelay > 0)
                {
                    yield return new WaitForSeconds(member.SpawnDelay);
                }

                if (!enemyDatabase.Enemies.ContainsKey(member.EnemyId)) continue;

                var enemyData = enemyDatabase.Enemies[member.EnemyId];
                Vector2 spawnPos = evt.SpawnPosition + member.LocalPosition;

                var enemy = SpawnEnemyInstance(enemyData, spawnPos, evt);
                if (enemy != null)
                {
                    formationEnemies.Add(enemy);
                }
            }

            // 編隊破壊イベントの監視
            if (evt.HasOnDestroyEvent && formationEnemies.Count > 0)
            {
                StartCoroutine(WatchFormationDestroyed(formationEnemies, evt));
            }
        }

        private IEnumerator WatchFormationDestroyed(List<GameObject> enemies, StageEvent evt)
        {
            while (true)
            {
                enemies.RemoveAll(e => e == null);

                if (enemies.Count == 0)
                {
                    // 時間条件チェック
                    if (evt.OnDestroyEventCondition <= 0 || timelineTime <= evt.OnDestroyEventCondition)
                    {
                        ExecuteOnDestroyEvent(evt);
                    }
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private GameObject SpawnEnemyInstance(Enemy enemyData, Vector2 position, StageEvent evt)
        {
            GameObject prefab = null;

            // Prefabを読み込み
            if (!string.IsNullOrEmpty(enemyData.PrefabPath))
            {
                prefab = Resources.Load<GameObject>(enemyData.PrefabPath);
            }

            if (prefab == null)
            {
                // デフォルトの敵を生成（仮）
                prefab = CreateDefaultEnemyPrefab();
            }

            var enemyGO = Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity, enemyContainer);
            enemyGO.name = $"{enemyData.EnemyName}_{System.Guid.NewGuid().ToString("N").Substring(0, 4)}";

            // EnemyControllerの設定
            var controller = enemyGO.GetComponent<EnemyController>();
            if (controller == null)
            {
                controller = enemyGO.AddComponent<EnemyController>();
            }

            controller.Initialize(enemyData, patternDatabase, this);

            // 単体敵の破壊イベント監視
            if (evt.HasOnDestroyEvent)
            {
                controller.OnDestroyed += () =>
                {
                    if (evt.OnDestroyEventCondition <= 0 || timelineTime <= evt.OnDestroyEventCondition)
                    {
                        ExecuteOnDestroyEvent(evt);
                    }
                };
            }

            return enemyGO;
        }

        private GameObject CreateDefaultEnemyPrefab()
        {
            // 仮のPrefab生成
            var go = new GameObject("DefaultEnemy");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDefaultSprite();
            sr.color = Color.red;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        private Sprite CreateDefaultSprite()
        {
            var tex = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        private void SpawnBoss(StageEvent evt)
        {
            // ボス出現（UI変更などを含む）
            if (debugMode)
            {
                Debug.Log($"Boss spawn: {evt.TargetId}");
            }

            SpawnEnemy(evt);

            // TODO: ボスUI表示
        }

        private void ExecuteOnDestroyEvent(StageEvent parentEvent)
        {
            if (parentEvent.OnDestroyEventType == StageEventType.NextTimeline)
            {
                StartTimeline(parentEvent.OnDestroyEventId);
            }
            else if (parentEvent.OnDestroyEventType == StageEventType.StageClear)
            {
                StageClear();
            }
            // 他のイベントタイプも必要に応じて追加
        }

        private void SetBackground(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Set background: {evt.TargetId}");
            }
            // TODO: 背景変更の実装
        }

        private void SetScrollSpeed(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Set scroll speed: {evt.ScrollSpeed}");
            }
            // TODO: スクロール速度変更の実装
        }

        private void PlaySound(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Play sound: {evt.SoundId}");
            }
            // TODO: SE再生の実装
        }

        private void PlayBGM(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Play BGM: {evt.SoundId}");
            }
            // TODO: BGM変更の実装
        }

        private void ShowMessage(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Show message: {evt.Message}");
            }
            // TODO: メッセージ表示の実装
        }

        private void CameraEffect(StageEvent evt)
        {
            if (debugMode)
            {
                Debug.Log($"Camera effect: {evt.TargetId}");
            }
            // TODO: カメラエフェクトの実装
        }

        private void StageClear()
        {
            isRunning = false;
            OnStageClear?.Invoke();

            if (debugMode)
            {
                Debug.Log("Stage Clear!");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ステージを一時停止
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// ステージを再開
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// ステージを停止
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            isPaused = false;

            // 全ての敵を破棄
            foreach (Transform child in enemyContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 指定時間にジャンプ（デバッグ用）
        /// </summary>
        public void JumpToTime(float time)
        {
            timelineTime = time;

            // イベントインデックスを更新
            currentEventIndex = 0;
            for (int i = 0; i < currentTimeline.Events.Count; i++)
            {
                if (currentTimeline.Events[i].Time <= time)
                {
                    currentEventIndex = i + 1;
                }
                else
                {
                    break;
                }
            }
        }

        #endregion
    }
}
