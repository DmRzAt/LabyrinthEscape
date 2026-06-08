using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]
public class FootstepAudio : MonoBehaviour
{
    [Tooltip("Continuous walking recordings. A random loop is selected each time movement starts.")]
    public AudioClip[] movementLoops;
    public int variations = 4;
    [Range(0f, 1f)] public float volume = 0.45f;
    [Range(0.1f, 10f)] public float fadeSpeed = 4f;
    [Tooltip("Seconds between audible steps in the source loop.")]
    [Min(0.1f)] public float sourceStepInterval = 0.63f;

    PlayerController _player;
    Rigidbody _body;
    AudioSource _loopSource;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _body = GetComponent<Rigidbody>();

        var oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.volume = 1f;

        _player.footstepSource = oneShotSource;
        _player.footstepVolume = volume;
        _player.jumpClip = ProceduralSfx.Jump(1);
        _player.landClip = ProceduralSfx.Land(1);

        if (movementLoops != null && movementLoops.Length > 0)
        {
            _player.footstepClips = System.Array.Empty<AudioClip>();
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = 0f;
            _loopSource.volume = 0f;
            return;
        }

        var clips = new AudioClip[Mathf.Max(1, variations)];
        for (int i = 0; i < clips.Length; i++) clips[i] = ProceduralSfx.Footstep(1000 + i * 37);
        _player.footstepClips = clips;
    }

    void Update()
    {
        if (_loopSource == null) return;

        Vector3 velocity = _body.linearVelocity;
        velocity.y = 0f;
        bool shouldPlay = _player.IsGrounded && velocity.sqrMagnitude > 0.25f;
        float targetVolume = shouldPlay ? volume : 0f;
        _loopSource.volume = Mathf.MoveTowards(_loopSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

        if (shouldPlay)
        {
            float targetStepRate = velocity.magnitude / Mathf.Max(0.1f, _player.CurrentStepStride);
            float sourceStepRate = 1f / Mathf.Max(0.1f, sourceStepInterval);
            _loopSource.pitch = Mathf.Clamp(targetStepRate / sourceStepRate, 0.35f, 1.5f);
        }

        if (shouldPlay && !_loopSource.isPlaying)
        {
            _loopSource.clip = movementLoops[Random.Range(0, movementLoops.Length)];
            _loopSource.Play();
        }
        else if (!shouldPlay && _loopSource.isPlaying && _loopSource.volume <= 0.001f)
        {
            _loopSource.Stop();
        }
    }
}
