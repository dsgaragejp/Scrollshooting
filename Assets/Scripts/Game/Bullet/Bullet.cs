using UnityEngine;

namespace Game.Bullet
{
    /// <summary>
    /// 弾の基本クラス
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 1;
        [SerializeField] private Vector3 direction = Vector3.right;

        [Header("Visual")]
        [SerializeField] private bool rotateToDirection = true;

        private float spawnTime;

        public int Damage => damage;

        public void Initialize(Vector3 dir, float spd)
        {
            direction = dir.normalized;
            speed = spd;

            if (rotateToDirection && direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void Start()
        {
            spawnTime = Time.time;

            if (rotateToDirection && direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void Update()
        {
            // 移動
            transform.position += direction * speed * Time.deltaTime;

            // 寿命チェック
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }

            // 画面外チェック
            if (Mathf.Abs(transform.position.x) > 15f || Mathf.Abs(transform.position.y) > 10f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject);
        }

        protected virtual void HandleCollision(GameObject other)
        {
            // サブクラスでオーバーライド
        }
    }

    /// <summary>
    /// 自機弾
    /// </summary>
    public class PlayerBullet : Bullet
    {
        protected override void HandleCollision(GameObject other)
        {
            if (other.CompareTag("Enemy"))
            {
                var enemy = other.GetComponent<StageEditor.Runtime.EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(Damage);
                }
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 敵弾
    /// </summary>
    public class EnemyBullet : Bullet
    {
        protected override void HandleCollision(GameObject other)
        {
            if (other.CompareTag("Player"))
            {
                var player = other.GetComponent<Player.PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(Damage);
                }
                Destroy(gameObject);
            }
        }
    }
}
