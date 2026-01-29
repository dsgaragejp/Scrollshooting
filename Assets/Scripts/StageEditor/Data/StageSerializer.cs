using System;
using System.IO;
using UnityEngine;

namespace StageEditor.Data
{
    /// <summary>
    /// ステージデータのJSON保存/読込
    /// </summary>
    public static class StageSerializer
    {
        private const string STAGE_FILE_EXTENSION = ".stage.json";
        private const string ENEMY_FILE_EXTENSION = ".enemies.json";
        private const string FORMATION_FILE_EXTENSION = ".formations.json";
        private const string PATTERNS_FILE_EXTENSION = ".patterns.json";

        /// <summary>
        /// ステージデータを保存するデフォルトパス
        /// </summary>
        public static string DefaultSavePath => Path.Combine(Application.dataPath, "StageData");

        /// <summary>
        /// ステージをJSONファイルに保存
        /// </summary>
        public static bool SaveStage(Stage stage, string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(DefaultSavePath, stage.StageId + STAGE_FILE_EXTENSION);
                }

                // ディレクトリ作成
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 更新日時を記録
                stage.Meta.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // JSON変換
                string json = JsonUtility.ToJson(stage, true);
                File.WriteAllText(filePath, json);

                Debug.Log($"Stage saved: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save stage: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// JSONファイルからステージを読み込み
        /// </summary>
        public static Stage LoadStage(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"Stage file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                Stage stage = JsonUtility.FromJson<Stage>(json);

                Debug.Log($"Stage loaded: {filePath}");
                return stage;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load stage: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 利用可能なステージファイル一覧を取得
        /// </summary>
        public static string[] GetStageFiles(string directory = null)
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = DefaultSavePath;
            }

            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(directory, "*" + STAGE_FILE_EXTENSION);
        }

        /// <summary>
        /// 敵データベースを保存
        /// </summary>
        public static bool SaveEnemyDatabase(EnemyDatabase database, string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(DefaultSavePath, "enemies" + ENEMY_FILE_EXTENSION);
                }

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(database, true);
                File.WriteAllText(filePath, json);

                Debug.Log($"Enemy database saved: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save enemy database: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 敵データベースを読み込み
        /// </summary>
        public static EnemyDatabase LoadEnemyDatabase(string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(DefaultSavePath, "enemies" + ENEMY_FILE_EXTENSION);
                }

                if (!File.Exists(filePath))
                {
                    return new EnemyDatabase();
                }

                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<EnemyDatabase>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load enemy database: {e.Message}");
                return new EnemyDatabase();
            }
        }

        /// <summary>
        /// パターンデータベースを保存
        /// </summary>
        public static bool SavePatternDatabase(PatternDatabase database, string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(DefaultSavePath, "patterns" + PATTERNS_FILE_EXTENSION);
                }

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(database, true);
                File.WriteAllText(filePath, json);

                Debug.Log($"Pattern database saved: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save pattern database: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// パターンデータベースを読み込み
        /// </summary>
        public static PatternDatabase LoadPatternDatabase(string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(DefaultSavePath, "patterns" + PATTERNS_FILE_EXTENSION);
                }

                if (!File.Exists(filePath))
                {
                    return new PatternDatabase();
                }

                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<PatternDatabase>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load pattern database: {e.Message}");
                return new PatternDatabase();
            }
        }
    }

    /// <summary>
    /// 敵データベース
    /// </summary>
    [Serializable]
    public class EnemyDatabase
    {
        public SerializableDictionary<string, Enemy> Enemies = new SerializableDictionary<string, Enemy>();
        public SerializableDictionary<string, EnemyFormation> Formations = new SerializableDictionary<string, EnemyFormation>();
    }

    /// <summary>
    /// パターンデータベース
    /// </summary>
    [Serializable]
    public class PatternDatabase
    {
        public SerializableDictionary<string, MovePattern> MovePatterns = new SerializableDictionary<string, MovePattern>();
        public SerializableDictionary<string, BulletPattern> BulletPatterns = new SerializableDictionary<string, BulletPattern>();
    }
}
