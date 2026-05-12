using UnityEngine;

public class SpawnCoreOrb : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject aliveVisual;
    [SerializeField] private GameObject brokenVisual;
    [SerializeField] private ParticleSystem breakEffect;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatFrequency = 1.5f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private bool floatWhenBroken = false;

    private Vector3 initialLocalPosition;
    private bool isBroken;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (isBroken && !floatWhenBroken)
            return;

        float offsetY = Mathf.Sin(Time.time * floatFrequency + phaseOffset) * floatAmplitude;
        transform.localPosition = initialLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    public void SetAlive(bool instant = false)
    {
        isBroken = false;

        if (aliveVisual != null)
            aliveVisual.SetActive(true);

        if (brokenVisual != null)
            brokenVisual.SetActive(false);

        if (instant)
            transform.localPosition = initialLocalPosition;
    }

    public void SetBroken(bool playEffect = true)
    {
        if (isBroken)
            return;

        isBroken = true;

        if (aliveVisual != null)
            aliveVisual.SetActive(false);

        if (brokenVisual != null)
            brokenVisual.SetActive(true);

        if (playEffect && breakEffect != null)
            breakEffect.Play();

        if (!floatWhenBroken)
            transform.localPosition = initialLocalPosition;
    }
}
