using UnityEngine;

namespace Game.Background
{
    /// <summary>
    /// 背景スクロール制御
    /// </summary>
    public class ScrollingBackground : MonoBehaviour
    {
        [Header("Scroll Settings")]
        [SerializeField] private float scrollSpeed = 2f;
        [SerializeField] private float resetPositionX = -30f;
        [SerializeField] private float startPositionX = 30f;

        [Header("References")]
        [SerializeField] private Transform[] backgroundLayers;

        private void Update()
        {
            // 各レイヤーをスクロール
            if (backgroundLayers != null && backgroundLayers.Length > 0)
            {
                foreach (var layer in backgroundLayers)
                {
                    if (layer == null) continue;

                    layer.position += Vector3.left * scrollSpeed * Time.deltaTime;

                    // リセット位置に達したらループ
                    if (layer.position.x <= resetPositionX)
                    {
                        layer.position = new Vector3(startPositionX, layer.position.y, layer.position.z);
                    }
                }
            }
            else
            {
                // 自身をスクロール
                transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

                if (transform.position.x <= resetPositionX)
                {
                    transform.position = new Vector3(startPositionX, transform.position.y, transform.position.z);
                }
            }
        }

        public void SetScrollSpeed(float speed)
        {
            scrollSpeed = speed;
        }
    }

    /// <summary>
    /// パララックス（視差）スクロール
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float parallaxFactor = 0.5f;
        [SerializeField] private bool infiniteHorizontal = true;
        [SerializeField] private float textureWidth = 20f;

        private Transform cameraTransform;
        private Vector3 lastCameraPosition;
        private float startX;

        private void Start()
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                lastCameraPosition = cameraTransform.position;
            }
            startX = transform.position.x;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
            transform.position += new Vector3(deltaMovement.x * parallaxFactor, 0, 0);
            lastCameraPosition = cameraTransform.position;

            // 無限スクロール
            if (infiniteHorizontal)
            {
                float relativeX = transform.position.x - startX;
                if (Mathf.Abs(relativeX) >= textureWidth)
                {
                    float offset = relativeX > 0 ? -textureWidth : textureWidth;
                    transform.position += new Vector3(offset, 0, 0);
                    startX = transform.position.x;
                }
            }
        }
    }
}
