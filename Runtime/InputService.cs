using UnityEngine;

// Girdi servisi: Unity New Input System (PlayerInput) eylemlerini yönetir.
// Sahneye obje olarak konmasa bile Actions çağrıldığında otomatik ayağa kalkar (lazy init).
[DefaultExecutionOrder(-100)]
public class InputService : MonoBehaviour
{
    private static PlayerInput _actions;
    public static PlayerInput Actions
    {
        get
        {
            if (_actions == null)
            {
                _actions = new PlayerInput();
                _actions.Gameplay.Enable();
            }
            return _actions;
        }
        private set => _actions = value;
    }

    private static InputService _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (_actions == null)
        {
            _actions = new PlayerInput();
            _actions.Gameplay.Enable();
        }

        // DontDestroyOnLoad yalnızca kök objelerde çalışır. Sahnede düzen için
        // bir grup altında durabilsin diye çalışma anında köke çıkarılıyor.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // Yalnızca sahibi temizler. Kopya kendini yok ederken buraya girerse
        // hayatta kalan örneğin statiğini siler ve tüm girdi kopar.
        if (_instance != this) return;

        _actions?.Dispose();
        _actions = null;
        _instance = null;
    }
}
