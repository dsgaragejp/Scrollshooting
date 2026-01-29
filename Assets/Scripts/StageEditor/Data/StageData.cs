using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageEditor.Data
{
    /// <summary>
    /// ステージ全体のデータ構造
    /// </summary>
    [Serializable]
    public class Stage
    {
        public string StageId;
        public string StageName;
        public StageMeta Meta;
        public SerializableDictionary<string, StageTimeline> Timelines = new SerializableDictionary<string, StageTimeline>();
        public SerializableDictionary<string, StageEvent> Events = new SerializableDictionary<string, StageEvent>();

        public Stage()
        {
            StageId = Guid.NewGuid().ToString("N").Substring(0, 8);
            StageName = "New Stage";
            Meta = new StageMeta();
            Timelines = new SerializableDictionary<string, StageTimeline>();
            Events = new SerializableDictionary<string, StageEvent>();
        }
    }

    /// <summary>
    /// ステージメタ情報
    /// </summary>
    [Serializable]
    public class StageMeta
    {
        public string Author = "";
        public int Difficulty = 1;
        public string Description = "";
        public string Version = "1.0.0";
        public string CreatedAt;
        public string UpdatedAt;

        public StageMeta()
        {
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UpdatedAt = CreatedAt;
        }
    }

    /// <summary>
    /// タイムライン（開幕～中ボス、中ボス撃破～ボスなどで別Timelineにできる）
    /// </summary>
    [Serializable]
    public class StageTimeline
    {
        public string TimelineId;
        public string TimelineName;
        public float ScrollSpeed = 1.0f;
        public string BackgroundId;
        public List<TimelineEvent> Events = new List<TimelineEvent>();

        public StageTimeline()
        {
            TimelineId = Guid.NewGuid().ToString("N").Substring(0, 8);
            TimelineName = "New Timeline";
        }

        /// <summary>
        /// イベントを時間順にソート
        /// </summary>
        public void SortEvents()
        {
            Events.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
    }

    /// <summary>
    /// 時間に紐づいた実行イベント定義
    /// </summary>
    [Serializable]
    public class TimelineEvent
    {
        public float Time;           // 実行するタイムライン上の時間（秒）
        public string EventId;       // 実行するイベントのID

        public TimelineEvent()
        {
            Time = 0f;
            EventId = "";
        }

        public TimelineEvent(float time, string eventId)
        {
            Time = time;
            EventId = eventId;
        }
    }

    /// <summary>
    /// イベントタイプ
    /// </summary>
    public enum StageEventType
    {
        None,                    // なにもしない（OnDestroy用）
        SetBackground,           // 背景の設定、ルート変更など
        SetScrollSpeed,          // 背景スクロール速度の調節
        SpawnSingleEnemy,        // 単体敵の出現
        SpawnFormationEnemies,   // 敵編隊の出現
        SpawnBoss,               // ボス（体力ゲージを表示などUIをボスモードにする切り替え）
        NextTimeline,            // 次のタイムラインへ移行する
        StageClear,              // ステージクリア
        PlaySound,               // SE再生
        PlayBGM,                 // BGM変更
        ShowMessage,             // メッセージ表示
        CameraEffect,            // カメラエフェクト
    }

    /// <summary>
    /// 実際に実行されるイベントの定義
    /// </summary>
    [Serializable]
    public class StageEvent
    {
        public string EventId;
        public string EventName;
        public StageEventType EventType;
        public bool IsFavorite;              // お気に入りフラグ

        // 共通パラメータ
        public string TargetId;              // 敵や編隊、移行先タイムラインなどのID
        public string PrefabPath;            // 関連するPrefabのパス（アイコン表示用）
        public Vector2 SpawnPosition;        // 出現座標

        // スクロール速度用
        public float ScrollSpeed;

        // 破壊時イベント
        public bool HasOnDestroyEvent;
        public StageEventType OnDestroyEventType;
        public string OnDestroyEventId;
        public float OnDestroyEventCondition; // Timeline時間がこの値を超えていたら破壊イベントを実行しない

        // サウンド用
        public string SoundId;
        public float Volume = 1.0f;

        // メッセージ用
        public string Message;
        public float MessageDuration = 2.0f;

        public StageEvent()
        {
            EventId = Guid.NewGuid().ToString("N").Substring(0, 8);
            EventName = "New Event";
            EventType = StageEventType.None;
            SpawnPosition = Vector2.zero;
            IsFavorite = false;
        }

        public StageEvent(StageEventType type) : this()
        {
            EventType = type;
            EventName = type.ToString();
        }
    }

    /// <summary>
    /// 敵編隊の定義
    /// </summary>
    [Serializable]
    public class EnemyFormation
    {
        public string FormationId;
        public string FormationName;
        public List<FormationMember> Members = new List<FormationMember>();
        public string MovePatternId;

        public EnemyFormation()
        {
            FormationId = Guid.NewGuid().ToString("N").Substring(0, 8);
            FormationName = "New Formation";
        }
    }

    /// <summary>
    /// 編隊メンバー
    /// </summary>
    [Serializable]
    public class FormationMember
    {
        public string EnemyId;
        public Vector2 LocalPosition;    // 編隊内の相対座標
        public float SpawnDelay;         // 出現遅延（秒）
    }

    /// <summary>
    /// 敵定義
    /// </summary>
    [Serializable]
    public class Enemy
    {
        public string EnemyId;
        public string EnemyName;
        public string PrefabPath;
        public int Hp = 1;
        public int Score = 100;
        public string MovePatternId;
        public string BulletPatternId;
        public string ItemDropId;
        public float ItemDropRate = 0.1f;

        public Enemy()
        {
            EnemyId = Guid.NewGuid().ToString("N").Substring(0, 8);
            EnemyName = "New Enemy";
        }
    }

    /// <summary>
    /// 移動パターン定義
    /// </summary>
    [Serializable]
    public class MovePattern
    {
        public string PatternId;
        public string PatternName;
        public MovePatternType Type;
        public float Speed = 3.0f;
        public List<Vector2> Waypoints = new List<Vector2>();
        public bool Loop;

        public MovePattern()
        {
            PatternId = Guid.NewGuid().ToString("N").Substring(0, 8);
            PatternName = "New Pattern";
        }
    }

    public enum MovePatternType
    {
        Straight,        // 直進
        Waypoint,        // ウェイポイント追従
        Sin,             // サイン波
        Circle,          // 円運動
        Target,          // プレイヤー追尾
        Custom,          // カスタム（スクリプト指定）
    }

    /// <summary>
    /// 弾幕パターン定義
    /// </summary>
    [Serializable]
    public class BulletPattern
    {
        public string PatternId;
        public string PatternName;
        public BulletPatternType Type;
        public int BulletCount = 1;
        public float BulletSpeed = 5.0f;
        public float FireRate = 1.0f;       // 発射間隔（秒）
        public float SpreadAngle = 30.0f;   // 扇形の角度
        public bool AimAtPlayer;

        public BulletPattern()
        {
            PatternId = Guid.NewGuid().ToString("N").Substring(0, 8);
            PatternName = "New Bullet Pattern";
        }
    }

    public enum BulletPatternType
    {
        Single,          // 単発
        Spread,          // 扇形
        Circle,          // 全方位
        Aimed,           // 自機狙い
        Random,          // ランダム
        Custom,          // カスタム
    }

    /// <summary>
    /// JSON用のシリアライズ可能なDictionary
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey> Keys = new List<TKey>();
        public List<TValue> Values = new List<TValue>();

        public TValue this[TKey key]
        {
            get
            {
                int index = Keys.IndexOf(key);
                return index >= 0 ? Values[index] : default;
            }
            set
            {
                int index = Keys.IndexOf(key);
                if (index >= 0)
                {
                    Values[index] = value;
                }
                else
                {
                    Keys.Add(key);
                    Values.Add(value);
                }
            }
        }

        public bool ContainsKey(TKey key) => Keys.Contains(key);

        public void Add(TKey key, TValue value)
        {
            if (!ContainsKey(key))
            {
                Keys.Add(key);
                Values.Add(value);
            }
        }

        public bool Remove(TKey key)
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
            {
                Keys.RemoveAt(index);
                Values.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Keys.Clear();
            Values.Clear();
        }

        public int Count => Keys.Count;

        public IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                yield return new KeyValuePair<TKey, TValue>(Keys[i], Values[i]);
            }
        }
    }
}
