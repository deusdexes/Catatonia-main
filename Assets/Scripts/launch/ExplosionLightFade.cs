using UnityEngine;

public class ExplosionLightFade : MonoBehaviour
{
    public Light lightSource;
    public float maxIntensity = 8.0f;
    public float duration = 0.5f;
    private float time;

    void Start()
    {
        if (lightSource == null)
        {
            lightSource = GetComponent<Light>();
        }
    }

    void Update()
    {
        time += Time.deltaTime;
        lightSource.intensity = Mathf.Lerp(maxIntensity, 0, time / duration);
        if (time >= duration)
        {
            lightSource.enabled = false;
        }
    }
}