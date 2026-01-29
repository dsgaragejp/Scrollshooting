using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// 自機コントローラー
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private Vector2 moveBounds = new Vector2(8f, 4f);

        [Header("Shooting")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.1f;

        [Header("Stats")]
        [SerializeField] private int maxHp = 3;

        private int currentHp;
        private float nextFireTime;
        private Vector2 moveInput;
        private bool isFiring;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;

        public System.Action<int, int> OnHpChanged;
        public System.Action OnDeath;

        private void Start()
        {
            currentHp = maxHp;

            if (firePoint == null)
            {
                firePoint = transform;
            }

            // 弾Prefabがなければリソースから読み込み
            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/PlayerBullet");
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
        }

        private void HandleMovement()
        {
            // Input Systemから入力を取得
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                moveInput = Vector2.zero;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveInput.x -= 1;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveInput.x += 1;
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) moveInput.y += 1;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) moveInput.y -= 1;

                isFiring = keyboard.spaceKey.isPressed || keyboard.zKey.isPressed;
            }

            // 移動
            Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
            transform.position += movement;

            // 画面内に制限
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -moveBounds.x, moveBounds.x);
            pos.y = Mathf.Clamp(pos.y, -moveBounds.y, moveBounds.y);
            transform.position = pos;
        }

        private void HandleShooting()
        {
            if (isFiring && Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
        }

        private void Fire()
        {
            if (bulletPrefab == null) return;

            Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        }

        public void TakeDamage(int damage = 1)
        {
            currentHp -= damage;
            OnHpChanged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("EnemyBullet") || other.CompareTag("Enemy"))
            {
                TakeDamage(1);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("EnemyBullet") || other.CompareTag("Enemy"))
            {
                TakeDamage(1);
            }
        }
    }
}
