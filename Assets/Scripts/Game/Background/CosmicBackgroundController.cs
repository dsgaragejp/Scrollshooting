using UnityEngine;

namespace Game.Background
{
    /// <summary>
    /// XorDev風の宇宙背景をカメラ後方に表示するコントローラー
    /// </summary>
    [ExecuteInEditMode]
    public class CosmicBackgroundController : MonoBehaviour
    {
        [Header("Background Settings")]
        [SerializeField] private Material backgroundMaterial;
        [SerializeField] private float distanceFromCamera = 50f;
        [SerializeField] private float scale = 100f;

        [Header("Animation")]
        [SerializeField] private float animationSpeed = 1f;
        [SerializeField] private float colorIntensity = 1f;
        [SerializeField] private float fractalScale = 0.3f;

        [Header("Feedback Effect")]
        [SerializeField] private bool useFeedback = true;
        [SerializeField] private float feedbackStrength = 0.5f;
        [SerializeField] private float distortAmount = 0.04f;

        private MeshRenderer meshRenderer;
        private RenderTexture feedbackTexture;
        private RenderTexture previousFrame;
        private Camera backgroundCamera;

        private void OnEnable()
        {
            SetupBackgroundQuad();
            if (useFeedback)
            {
                SetupFeedbackTextures();
            }
        }

        private void OnDisable()
        {
            CleanupFeedbackTextures();
        }

        private void SetupBackgroundQuad()
        {
            // 既存のMeshRendererを取得、なければ作成
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                // QuadメッシュをGameObjectに追加
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = CreateQuadMesh();
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (backgroundMaterial != null)
            {
                meshRenderer.sharedMaterial = backgroundMaterial;
            }
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "BackgroundQuad";

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();

            return mesh;
        }

        private void SetupFeedbackTextures()
        {
            int width = Screen.width > 0 ? Screen.width : 1920;
            int height = Screen.height > 0 ? Screen.height : 1080;

            feedbackTexture = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBFloat);
            previousFrame = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBFloat);

            feedbackTexture.wrapMode = TextureWrapMode.Clamp;
            previousFrame.wrapMode = TextureWrapMode.Clamp;
        }

        private void CleanupFeedbackTextures()
        {
            if (feedbackTexture != null)
            {
                feedbackTexture.Release();
                DestroyImmediate(feedbackTexture);
            }
            if (previousFrame != null)
            {
                previousFrame.Release();
                DestroyImmediate(previousFrame);
            }
        }

        private void Update()
        {
            UpdatePosition();
            UpdateMaterialProperties();
        }

        private void UpdatePosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // カメラの後方に配置
            Vector3 cameraForward = mainCamera.transform.forward;
            transform.position = mainCamera.transform.position + cameraForward * distanceFromCamera;
            transform.rotation = Quaternion.LookRotation(-cameraForward);
            transform.localScale = Vector3.one * scale;
        }

        private void UpdateMaterialProperties()
        {
            if (backgroundMaterial == null) return;

            backgroundMaterial.SetFloat("_Speed", animationSpeed);
            backgroundMaterial.SetFloat("_Intensity", colorIntensity);
            backgroundMaterial.SetFloat("_FractalScale", fractalScale);
            backgroundMaterial.SetFloat("_FeedbackStrength", feedbackStrength);
            backgroundMaterial.SetFloat("_DistortAmount", distortAmount);

            if (useFeedback && previousFrame != null)
            {
                backgroundMaterial.SetTexture("_MainTex", previousFrame);
            }
        }

        /// <summary>
        /// エディタからマテリアルを設定
        /// </summary>
        public void SetMaterial(Material mat)
        {
            backgroundMaterial = mat;
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = mat;
            }
        }
    }
}
