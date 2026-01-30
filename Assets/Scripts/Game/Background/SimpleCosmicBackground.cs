using UnityEngine;

namespace Game.Background
{
    /// <summary>
    /// シンプルな宇宙背景 - カメラの後方に大きなQuadを配置
    /// XorDev風シェーダーを背景として表示
    /// </summary>
    [ExecuteInEditMode]
    public class SimpleCosmicBackground : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material cosmicMaterial;
        [SerializeField] private Camera targetCamera;

        [Header("Positioning")]
        [SerializeField] private float distance = 100f;
        [SerializeField] private float size = 200f;

        [Header("Shader Parameters")]
        [Range(0.1f, 5f)]
        [SerializeField] private float speed = 1f;
        [Range(0.1f, 3f)]
        [SerializeField] private float intensity = 1f;
        [Range(0.1f, 1f)]
        [SerializeField] private float fractalScale = 0.3f;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            propertyBlock = new MaterialPropertyBlock();

            // MeshFilterとMeshRendererを確認・作成
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            // Quadメッシュを設定
            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = CreateQuadMesh();
            }

            // マテリアルを設定
            if (cosmicMaterial != null && meshRenderer.sharedMaterial != cosmicMaterial)
            {
                meshRenderer.sharedMaterial = cosmicMaterial;
            }

            // シャドウを無効化
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CosmicBackgroundQuad";

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
            mesh.RecalculateBounds();

            return mesh;
        }

        private void LateUpdate()
        {
            UpdateTransform();
            UpdateShaderProperties();
        }

        private void UpdateTransform()
        {
            if (targetCamera == null) return;

            // カメラの前方に配置（遠くに）
            Vector3 cameraPos = targetCamera.transform.position;
            Vector3 cameraForward = targetCamera.transform.forward;

            transform.position = cameraPos + cameraForward * distance;
            transform.rotation = Quaternion.LookRotation(cameraForward);
            transform.localScale = new Vector3(size, size, 1f);
        }

        private void UpdateShaderProperties()
        {
            if (meshRenderer == null || cosmicMaterial == null) return;

            meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat("_Speed", speed);
            propertyBlock.SetFloat("_Intensity", intensity);
            propertyBlock.SetFloat("_FractalScale", fractalScale);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// マテリアルを設定
        /// </summary>
        public void SetMaterial(Material mat)
        {
            cosmicMaterial = mat;
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// カメラを設定
        /// </summary>
        public void SetCamera(Camera cam)
        {
            targetCamera = cam;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (meshRenderer != null && cosmicMaterial != null)
            {
                meshRenderer.sharedMaterial = cosmicMaterial;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (targetCamera == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size, size, 0.1f));
        }
#endif
    }
}
