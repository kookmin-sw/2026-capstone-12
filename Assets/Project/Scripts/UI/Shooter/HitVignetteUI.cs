using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class HitVignetteUI : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float maxAlpha = 0.7f;
    [SerializeField] private int textureSize = 256;

    private RawImage rawImage;
    private float flashTimer = 0f;
    private bool isFlashing = false;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.texture = GenerateVignetteTexture();
        rawImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnEnable()
    {
        ShooterHealthNet.OnShooterHit += TriggerFlash;
    }

    private void OnDisable()
    {
        ShooterHealthNet.OnShooterHit -= TriggerFlash;
    }

    private void Update()
    {
        if (!isFlashing) return;

        flashTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(flashTimer / flashDuration) * maxAlpha;
        rawImage.color = new Color(1f, 1f, 1f, alpha);

        if (flashTimer <= 0f)
        {
            isFlashing = false;
            rawImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void TriggerFlash()
    {
        flashTimer = flashDuration;
        isFlashing = true;
    }

    private Texture2D GenerateVignetteTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = textureSize * 0.5f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Transparent in center, red at screen edges
                float alpha = Mathf.Clamp01((dist - 0.45f) / 0.55f);
                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 0f, 0f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}
