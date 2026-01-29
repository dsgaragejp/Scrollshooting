using UnityEngine;
using UnityEngine.Events;
using Game.Player;
using StageEditor.Runtime;

namespace Game
{
    /// <summary>
    /// ゲーム全体の管理
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Settings")]
        [SerializeField] private int initialLives = 3;

        [Header("Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameOver;
        public UnityEvent OnGameClear;
        public UnityEvent<int> OnScoreChanged;
        public UnityEvent<int> OnLivesChanged;

        private int currentScore;
        private int currentLives;
        private bool isGameRunning;

        public int Score => currentScore;
        public int Lives => currentLives;
        public bool IsGameRunning => isGameRunning;
        public PlayerController Player => player;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 自動的にプレイヤーとステージマネージャーを探す
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (stageManager == null)
            {
                stageManager = FindFirstObjectByType<StageManager>();
            }

            // イベント登録
            if (player != null)
            {
                player.OnDeath += OnPlayerDeath;
            }

            if (stageManager != null)
            {
                stageManager.OnStageClear.AddListener(OnStageClear);
            }

            StartGame();
        }

        public void StartGame()
        {
            currentScore = 0;
            currentLives = initialLives;
            isGameRunning = true;

            OnScoreChanged?.Invoke(currentScore);
            OnLivesChanged?.Invoke(currentLives);
            OnGameStart?.Invoke();

            Debug.Log("Game Started!");
        }

        public void AddScore(int points)
        {
            if (!isGameRunning) return;

            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);
        }

        private void OnPlayerDeath()
        {
            currentLives--;
            OnLivesChanged?.Invoke(currentLives);

            if (currentLives <= 0)
            {
                GameOver();
            }
            else
            {
                // リスポーン
                Invoke(nameof(RespawnPlayer), 1f);
            }
        }

        private void RespawnPlayer()
        {
            if (player != null)
            {
                Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : new Vector3(-6, 0, 0);
                player.transform.position = spawnPos;
                player.gameObject.SetActive(true);
            }
        }

        private void OnStageClear()
        {
            isGameRunning = false;
            OnGameClear?.Invoke();
            Debug.Log($"Stage Clear! Final Score: {currentScore}");
        }

        private void GameOver()
        {
            isGameRunning = false;
            OnGameOver?.Invoke();
            Debug.Log($"Game Over! Final Score: {currentScore}");
        }

        public void RestartGame()
        {
            // シーンをリロード
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
