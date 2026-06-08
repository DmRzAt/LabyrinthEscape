using UnityEngine;

public class DungeonAmbience : MonoBehaviour
{
    [Header("Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.18f;

    [Header("Ambience")]
    public Vector2 intervalRange = new Vector2(5f, 13f);
    [Range(0f, 1f)] public float volume = 0.5f;

    AudioSource _src, _musicSource;
    AudioClip[] _drips, _creaks, _rumbles;
    float _timer;

    void Start()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;

        if (backgroundMusic != null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = musicVolume;
            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }

        _drips   = new[] { ProceduralSfx.Drip(1), ProceduralSfx.Drip(2), ProceduralSfx.Drip(3) };
        _creaks  = new[] { ProceduralSfx.Creak(1), ProceduralSfx.Creak(2) };
        _rumbles = new[] { ProceduralSfx.Rumble(1), ProceduralSfx.Rumble(2) };

        _timer = Random.Range(intervalRange.x, intervalRange.y);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = Random.Range(intervalRange.x, intervalRange.y);

        float r = Random.value;
        AudioClip clip;
        float v;
        if (r < 0.55f)      { clip = Pick(_drips);   v = volume * Random.Range(0.5f, 0.9f); }
        else if (r < 0.85f) { clip = Pick(_creaks);  v = volume * Random.Range(0.4f, 0.7f); }
        else                { clip = Pick(_rumbles); v = volume * Random.Range(0.6f, 1f); }

        _src.pitch = Random.Range(0.92f, 1.08f);
        _src.PlayOneShot(clip, v);
    }

    AudioClip Pick(AudioClip[] arr) => arr[Random.Range(0, arr.Length)];
}
