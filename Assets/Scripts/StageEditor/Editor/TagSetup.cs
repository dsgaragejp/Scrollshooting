using UnityEngine;
using UnityEditor;

namespace StageEditor.Editor
{
    /// <summary>
    /// ゲームに必要なタグを自動設定
    /// </summary>
    public static class TagSetup
    {
        private static readonly string[] RequiredTags = new string[]
        {
            "Player",
            "Enemy",
            "PlayerBullet",
            "EnemyBullet",
            "Item"
        };

        [MenuItem("Tools/Setup Game Tags")]
        public static void SetupTags()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            foreach (string tag in RequiredTags)
            {
                if (!HasTag(tagsProp, tag))
                {
                    AddTag(tagsProp, tag);
                    Debug.Log($"Tag added: {tag}");
                }
                else
                {
                    Debug.Log($"Tag already exists: {tag}");
                }
            }

            tagManager.ApplyModifiedProperties();
            Debug.Log("Tag setup complete!");
        }

        private static bool HasTag(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddTag(SerializedProperty tagsProp, string tag)
        {
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        }
    }
}
