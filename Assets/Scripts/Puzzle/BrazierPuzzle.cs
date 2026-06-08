using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrazierPuzzle : MonoBehaviour
{
    [Header("Braziers (array order is the brazier index)")]
    [SerializeField] private Brazier[] _braziers;

    [Header("Rules")]
    [SerializeField, Range(1, 8)] private int _startLength = 3;
    [SerializeField, Range(1, 12)] private int _winLength = 5;
    [SerializeField] private bool _resetOnMistake = true;

    [Header("Show timing")]
    [SerializeField] private float _startDelay = 1f;
    [SerializeField] private float _flashOn = 0.5f;
    [SerializeField] private float _flashGap = 0.25f;

    [Header("Start")]
    [SerializeField] private bool _startOnTriggerEnter = true;
    [SerializeField] private bool _autoStart = false;
    [Tooltip("If set, the puzzle starts only after this wave room's enemies (incl. boss) are all dead.")]
    [SerializeField] private WaveRoomPuzzle _startAfterWaveRoom;
    [Tooltip("Keep the braziers hidden until the puzzle starts (so they aren't visible during the fight).")]
    [SerializeField] private bool _hideBraziersUntilStart = true;
    [Tooltip("Smooth ignite of each brazier when the puzzle starts.")]
    [SerializeField] private float _revealDuration = 0.5f;
    [SerializeField] private float _revealStagger = 0.18f;

    [Header("Replay")]
    [Tooltip("Optional lever the player can pull during their turn to re-watch the current sequence.")]
    [SerializeField] private PuzzleLever _replayLever;

    [Header("Flame colours (per brazier, by index)")]
    [SerializeField] private Color[] _palette =
    {
        new Color(1f, 0.25f, 0.18f),
        new Color(0.35f, 1f, 0.30f),
        new Color(0.30f, 0.6f, 1f),
        new Color(1f, 0.82f, 0.28f),
    };

    [Header("Reward")]
    [SerializeField] private Door[] _doorsToOpen;
    [SerializeField] private GameObject _rewardChest;
    [SerializeField] private GameObject _keyPrefab;
    [SerializeField] private Transform _keySpawnPoint;
    [SerializeField] private bool _debugLogs = true;

    readonly List<int> _sequence = new List<int>();
    readonly List<int> _valid = new List<int>();
    int _inputIndex;
    bool _started, _completed, _playerTurn, _showing;
    System.Random _rng;

    AudioSource _sfx;
    AudioClip _wrongClip, _winClip;

    void Awake()
    {
        _rng = new System.Random();
        for (int i = 0; i < _braziers.Length; i++)
        {
            Brazier b = _braziers[i];
            if (b == null) continue;
            b.SetIndex(i);
            b.SetInteractable(false);
            if (_palette != null && _palette.Length > 0) b.SetFlameColor(_palette[i % _palette.Length]);
            b.SetLit(false);
            b.Activated += OnBrazierActivated;
            _valid.Add(i);
        }
        if (_rewardChest != null) _rewardChest.SetActive(false);
        if (_winLength < _startLength) _winLength = _startLength;

        if (_hideBraziersUntilStart)
            foreach (Brazier b in _braziers) if (b != null) b.gameObject.SetActive(false);

        if (_startAfterWaveRoom != null) _startAfterWaveRoom.WavesCleared += StartPuzzle;
        if (_replayLever != null) { _replayLever.SetEnabled(true); _replayLever.StateChanged += OnReplayLever; }

        ValidateConfig();
    }

    void ValidateConfig()
    {
        bool gatesProgress = _doorsToOpen != null && _doorsToOpen.Length > 0;
        bool canStart = _startOnTriggerEnter || _autoStart || _startAfterWaveRoom != null;

        if (gatesProgress && !canStart)
            Debug.LogError("[BrazierPuzzle] Gates a door but has no start source (trigger / autoStart / waveRoom) — the door could stay locked forever.", this);
        if (gatesProgress && _valid.Count == 0)
            Debug.LogError("[BrazierPuzzle] Gates a door but has no usable braziers assigned.", this);
        if (_braziers != null && (_palette == null || _palette.Length < _braziers.Length))
            Debug.LogWarning("[BrazierPuzzle] Fewer palette colours than braziers — some will share a colour.", this);
    }

    void OnDestroy()
    {
        if (_startAfterWaveRoom != null) _startAfterWaveRoom.WavesCleared -= StartPuzzle;
        if (_replayLever != null) _replayLever.StateChanged -= OnReplayLever;
        if (_braziers == null) return;
        foreach (Brazier b in _braziers) if (b != null) b.Activated -= OnBrazierActivated;
    }

    void OnReplayLever(PuzzleLever lever, bool isOn)
    {
        if (!isOn) return;
        ReplaySequence();
        lever.SetOn(false, false);
    }

    public void ReplaySequence()
    {
        if (!_started || _completed || _showing || !_playerTurn) return;
        _playerTurn = false;
        StartCoroutine(RunRound());
    }

    void Start()
    {
        if (_autoStart) StartPuzzle();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_startOnTriggerEnter || _started || !other.CompareTag(Tags.Player)) return;
        StartPuzzle();
    }

    public void StartPuzzle()
    {
        if (_started) return;

        if (_valid.Count == 0)
        {
            Debug.LogError("[BrazierPuzzle] No usable braziers — opening the door so the player isn't trapped.", this);
            _started = true;
            _completed = true;
            GrantReward();
            return;
        }

        _started = true;
        _sequence.Clear();
        for (int i = 0; i < _startLength; i++) AppendStep();
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        if (_hideBraziersUntilStart)
        {
            for (int i = 0; i < _braziers.Length; i++)
            {
                if (_braziers[i] != null) _braziers[i].Reveal(_revealDuration);
                yield return new WaitForSeconds(_revealStagger);
            }
            yield return new WaitForSeconds(_revealDuration);
        }
        else
        {
            foreach (Brazier b in _braziers) if (b != null) b.gameObject.SetActive(true);
        }

        yield return RunRound();
    }

    void AppendStep()
    {
        if (_valid.Count == 0) return;

        List<int> pool = new List<int>();
        foreach (int v in _valid) if (!_sequence.Contains(v)) pool.Add(v);
        if (pool.Count == 0) pool.AddRange(_valid);

        if (_sequence.Count > 0 && pool.Count > 1) pool.Remove(_sequence[_sequence.Count - 1]);

        _sequence.Add(pool[_rng.Next(pool.Count)]);
    }

    IEnumerator RunRound()
    {
        yield return ShowSequence();
        BeginPlayerTurn();
    }

    IEnumerator ShowSequence()
    {
        _playerTurn = false;
        _showing = true;
        SetAllInteractable(false);
        SetAllLit(false);
        Toast("Watch the sequence...");
        if (_debugLogs) Debug.Log($"[BrazierPuzzle] Showing sequence of {_sequence.Count}.", this);

        yield return new WaitForSeconds(_startDelay);

        for (int i = 0; i < _sequence.Count; i++)
        {
            Brazier b = _braziers[_sequence[i]];
            if (b != null) b.Flash(_flashOn);
            yield return new WaitForSeconds(_flashOn + _flashGap);
        }

        _showing = false;
    }

    void BeginPlayerTurn()
    {
        _inputIndex = 0;
        _playerTurn = true;
        SetAllInteractable(true);
        Toast(_replayLever != null ? "Repeat it!  (pull the lever to watch again)" : "Repeat it!");
    }

    void OnBrazierActivated(Brazier b)
    {
        if (!_playerTurn || _completed || b == null) return;

        int expected = _sequence[_inputIndex];
        if (b.Index != expected)
        {
            Mistake();
            return;
        }

        b.Flash(_flashOn * 0.6f);
        _inputIndex++;

        if (_inputIndex < _sequence.Count) return;

        _playerTurn = false;
        SetAllInteractable(false);

        if (_sequence.Count >= _winLength) Win();
        else StartCoroutine(NextRoundAfter(0.7f));
    }

    IEnumerator NextRoundAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        AppendStep();
        yield return RunRound();
    }

    void Mistake()
    {
        _playerTurn = false;
        SetAllInteractable(false);
        PlayWrong();
        if (_debugLogs) Debug.Log("[BrazierPuzzle] Wrong input — resetting.", this);
        StartCoroutine(MistakeRoutine());
    }

    IEnumerator MistakeRoutine()
    {
        Toast("Wrong!");
        foreach (Brazier b in _braziers) if (b != null) b.Flash(0.15f, false);
        yield return new WaitForSeconds(0.9f);

        if (_resetOnMistake)
        {
            _sequence.Clear();
            for (int i = 0; i < _startLength; i++) AppendStep();
        }
        yield return RunRound();
    }

    void Win()
    {
        _completed = true;
        if (_debugLogs) Debug.Log("[BrazierPuzzle] Solved! Firing reward.", this);

        foreach (Brazier b in _braziers)
        {
            if (b == null) continue;
            b.CancelFlash();
            b.SetInteractable(false);
            b.SetLit(true);
        }
        PlayWin();
        GrantReward();
    }

    void GrantReward()
    {
        if (_rewardChest != null)
        {
            _rewardChest.SetActive(true);
            var reveal = _rewardChest.GetComponent<RewardReveal>();
            if (reveal != null) reveal.Reveal();
        }
        else if (_keyPrefab != null && _keySpawnPoint != null)
        {
            Instantiate(_keyPrefab, _keySpawnPoint.position, _keySpawnPoint.rotation).SetActive(true);
        }

        if (_doorsToOpen == null) return;
        foreach (Door d in _doorsToOpen)
        {
            if (d == null) continue;
            d.SetLocked(false);
            d.Open();
        }
    }

    void SetAllInteractable(bool on)
    {
        foreach (Brazier b in _braziers) if (b != null) b.SetInteractable(on);
    }

    void SetAllLit(bool lit)
    {
        foreach (Brazier b in _braziers) if (b != null) b.SetLit(lit);
    }

    static void Toast(string text) => PickupFeedback.ShowMessage(text);

    void EnsureSfx()
    {
        if (_sfx != null) return;
        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
        _wrongClip = ProceduralSfx.Chime(300f, 150f, 0.30f, 0.55f);
        _winClip = ProceduralSfx.Chime(523f, 1046f, 0.5f, 0.65f);
    }

    void PlayWrong()
    {
        EnsureSfx();
        _sfx.pitch = 1f;
        _sfx.PlayOneShot(_wrongClip);
        CameraShake.Shake(0.18f, 0.14f);
        CameraShake.Punch(new Vector3(0f, 0f, 6f));
        PostFXPunch.Punch(0.55f);
    }

    void PlayWin()
    {
        EnsureSfx();
        _sfx.pitch = 1f;
        _sfx.PlayOneShot(_winClip);
    }
}
