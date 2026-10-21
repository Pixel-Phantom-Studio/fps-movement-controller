using UnityEngine;

// PlayerController'ın adım olayında bir ayak sesi çalar; ritim kamera sallanmasıyla aynıdır.
// Klip dizisi boşsa sessiz kalır.
public class Footsteps : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private float _minPitch = 0.9f;
    [SerializeField] private float _maxPitch = 1.1f;

    [Header("Duruma göre ses")]
    [SerializeField] private float _crouchVolume = 0.35f;
    [SerializeField] private float _walkVolume = 0.8f;
    [SerializeField] private float _sprintVolume = 1f;
    [SerializeField] private float _crouchPitchDrop = 0.1f;   // eğilince daha pes, daha yumuşak

    private int _lastClipIndex = -1;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<PlayerController>();
        if (_audio == null) _audio = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _player.Stepped += PlayStep;
    }

    private void OnDisable()
    {
        _player.Stepped -= PlayStep;
    }

    private void PlayStep()
    {
        if (_audio == null || _clips == null || _clips.Length == 0) return;

        // Rastgele klip seç, ama bir öncekiyle aynı olmasın.
        int index = Random.Range(0, _clips.Length);
        if (_clips.Length > 1 && index == _lastClipIndex)
            index = (index + 1) % _clips.Length;
        _lastClipIndex = index;

        if (_clips[index] == null) return;

        float volume = _walkVolume;
        float pitch = Random.Range(_minPitch, _maxPitch);
        if (_player.IsCrouching()) { volume = _crouchVolume; pitch -= _crouchPitchDrop; }
        else if (_player.IsSprinting()) volume = _sprintVolume;

        _audio.pitch = pitch;
        _audio.PlayOneShot(_clips[index], volume);
    }
}
