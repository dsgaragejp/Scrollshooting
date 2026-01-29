using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StageEditor.Data;

namespace StageEditor.Runtime
{
    /// <summary>
    /// 敵の動作を制御するコンポーネント
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private int currentHp;
        [SerializeField] private int score;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // データ
        private Enemy enemyData;
        private MovePattern movePattern;
        private BulletPattern bulletPattern;
        private PatternDatabase patternDatabase;
        private StageManager stageManager;

        // 移動
        private Vector3 startPosition;
        private float moveTime;
        private int waypointIndex;

        // 射撃
        private float lastFireTime;

        // イベント
        public event Action OnDestroyed;
        public event Action<int> OnDamaged;

        public int CurrentHp => currentHp;
        public int Score => score;

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(Enemy data, PatternDatabase patterns, StageManager manager)
        {
            enemyData = data;
            patternDatabase = patterns;
            stageManager = manager;

            currentHp = data.Hp;
            score = data.Score;
            startPosition = transform.position;

            // 移動パターン取得
            if (!string.IsNullOrEmpty(data.MovePatternId) && patterns.MovePatterns.ContainsKey(data.MovePatternId))
            {
                movePattern = patterns.MovePatterns[data.MovePatternId];
            }

            // 弾幕パターン取得
            if (!string.IsNullOrEmpty(data.BulletPatternId) && patterns.BulletPatterns.ContainsKey(data.BulletPatternId))
            {
                bulletPattern = patterns.BulletPatterns[data.BulletPatternId];
            }

            if (debugMode)
            {
                Debug.Log($"Enemy initialized: {data.EnemyName}, HP: {currentHp}");
            }
        }

        private void Update()
        {
            UpdateMovement();
            UpdateFiring();
            CheckOutOfBounds();
        }

        #region Movement

        private void UpdateMovement()
        {
            if (movePattern == null)
            {
                // デフォルト：左に直進
                transform.position += Vector3.left * 3f * Time.deltaTime;
                return;
            }

            moveTime += Time.deltaTime;

            switch (movePattern.Type)
            {
                case MovePatternType.Straight:
                    MoveStraight();
                    break;

                case MovePatternType.Sin:
                    MoveSin();
                    break;

                case MovePatternType.Circle:
                    MoveCircle();
                    break;

                case MovePatternType.Waypoint:
                    MoveWaypoint();
                    break;

                case MovePatternType.Target:
                    MoveTowardsPlayer();
                    break;
            }
        }

        private void MoveStraight()
        {
            transform.position += Vector3.left * movePattern.Speed * Time.deltaTime;
        }

        private void MoveSin()
        {
            float x = startPosition.x - moveTime * movePattern.Speed;
            float y = startPosition.y + Mathf.Sin(moveTime * 3f) * 2f;
            transform.position = new Vector3(x, y, 0);
        }

        private void MoveCircle()
        {
            float angle = moveTime * movePattern.Speed;
            float radius = 2f;
            float x = startPosition.x + Mathf.Cos(angle) * radius - moveTime * 0.5f;
            float y = startPosition.y + Mathf.Sin(angle) * radius;
            transform.position = new Vector3(x, y, 0);
        }

        private void MoveWaypoint()
        {
            if (movePattern.Waypoints.Count == 0) return;

            Vector3 target = startPosition + (Vector3)movePattern.Waypoints[waypointIndex];
            Vector3 direction = (target - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target);

            if (distance < 0.1f)
            {
                waypointIndex++;
                if (waypointIndex >= movePattern.Waypoints.Count)
                {
                    if (movePattern.Loop)
                    {
                        waypointIndex = 0;
                    }
                    else
                    {
                        waypointIndex = movePattern.Waypoints.Count - 1;
                    }
                }
            }
            else
            {
                transform.position += direction * movePattern.Speed * Time.deltaTime;
            }
        }

        private void MoveTowardsPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 direction = (player.transform.position - transform.position).normalized;
                transform.position += direction * movePattern.Speed * Time.deltaTime;
            }
            else
            {
                // プレイヤーがいない場合は左へ
                transform.position += Vector3.left * movePattern.Speed * Time.deltaTime;
            }
        }

        #endregion

        #region Firing

        private void UpdateFiring()
        {
            if (bulletPattern == null) return;

            if (Time.time - lastFireTime >= bulletPattern.FireRate)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }

        private void Fire()
        {
            switch (bulletPattern.Type)
            {
                case BulletPatternType.Single:
                    FireSingle();
                    break;

                case BulletPatternType.Spread:
                    FireSpread();
                    break;

                case BulletPatternType.Circle:
                    FireCircle();
                    break;

                case BulletPatternType.Aimed:
                    FireAimed();
                    break;

                case BulletPatternType.Random:
                    FireRandom();
                    break;
            }
        }

        private void FireSingle()
        {
            Vector2 direction = bulletPattern.AimAtPlayer ? GetPlayerDirection() : Vector2.left;
            SpawnBullet(direction);
        }

        private void FireSpread()
        {
            float spreadRad = bulletPattern.SpreadAngle * Mathf.Deg2Rad;
            float baseAngle = bulletPattern.AimAtPlayer ? GetPlayerAngle() : Mathf.PI;

            float startAngle = baseAngle - spreadRad / 2;
            float angleStep = bulletPattern.BulletCount > 1 ? spreadRad / (bulletPattern.BulletCount - 1) : 0;

            for (int i = 0; i < bulletPattern.BulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnBullet(direction);
            }
        }

        private void FireCircle()
        {
            for (int i = 0; i < bulletPattern.BulletCount; i++)
            {
                float angle = (Mathf.PI * 2 / bulletPattern.BulletCount) * i;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnBullet(direction);
            }
        }

        private void FireAimed()
        {
            Vector2 direction = GetPlayerDirection();
            SpawnBullet(direction);
        }

        private void FireRandom()
        {
            for (int i = 0; i < bulletPattern.BulletCount; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnBullet(direction);
            }
        }

        private void SpawnBullet(Vector2 direction)
        {
            // 弾のPrefabを生成（簡易版）
            var bullet = new GameObject("EnemyBullet");
            bullet.transform.position = transform.position;
            bullet.tag = "EnemyBullet";

            var sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBulletSprite();
            sr.color = Color.yellow;

            var collider = bullet.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;
            collider.isTrigger = true;

            var bulletController = bullet.AddComponent<BulletController>();
            bulletController.Initialize(direction, bulletPattern.BulletSpeed);

            // 親コンテナに配置
            if (stageManager != null)
            {
                bullet.transform.SetParent(stageManager.transform.Find("BulletContainer"));
            }
        }

        private Sprite CreateBulletSprite()
        {
            var tex = new Texture2D(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(4, 4));
                    tex.SetPixel(x, y, dist < 4 ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        }

        private Vector2 GetPlayerDirection()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                return (player.transform.position - transform.position).normalized;
            }
            return Vector2.left;
        }

        private float GetPlayerAngle()
        {
            Vector2 dir = GetPlayerDirection();
            return Mathf.Atan2(dir.y, dir.x);
        }

        #endregion

        #region Damage & Death

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHp -= damage;
            OnDamaged?.Invoke(damage);

            if (debugMode)
            {
                Debug.Log($"{enemyData?.EnemyName} took {damage} damage. HP: {currentHp}");
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (debugMode)
            {
                Debug.Log($"{enemyData?.EnemyName} destroyed! Score: {score}");
            }

            // スコア加算（GameManagerに通知）
            // TODO: GameManager.Instance.AddScore(score);

            // アイテムドロップ
            if (enemyData != null && !string.IsNullOrEmpty(enemyData.ItemDropId))
            {
                if (UnityEngine.Random.value <= enemyData.ItemDropRate)
                {
                    // TODO: アイテム生成
                }
            }

            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void CheckOutOfBounds()
        {
            // 画面外に出たら破棄
            if (transform.position.x < -15f || transform.position.x > 15f ||
                transform.position.y < -10f || transform.position.y > 10f)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject, other.tag);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other.gameObject, other.tag);
        }

        private void HandleCollision(GameObject obj, string tag)
        {
            // プレイヤーの弾との衝突
            if (tag == "PlayerBullet")
            {
                // ダメージ量を弾から取得
                var bullet = obj.GetComponent<Game.Bullet.Bullet>();
                int damage = bullet != null ? bullet.Damage : 1;

                TakeDamage(damage);
                Destroy(obj);

                // スコア加算
                if (currentHp <= 0 && Game.GameManager.Instance != null)
                {
                    Game.GameManager.Instance.AddScore(score);
                }
            }
        }
    }

    /// <summary>
    /// 弾の移動制御
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;

        public void Initialize(Vector2 dir, float spd)
        {
            direction = dir.normalized;
            speed = spd;
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            // 画面外で破棄
            if (Mathf.Abs(transform.position.x) > 15f || Mathf.Abs(transform.position.y) > 10f)
            {
                Destroy(gameObject);
            }
        }
    }
}
