using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float _moveSpeed = 2.8f;
    [SerializeField] private float _sprintSpeed = 5f;
    [SerializeField] private float _crouchMoveSpeed = 1.4f;
    [SerializeField] private float _acceleration = 12f;   // m/s² — tam hıza ~0.25 sn
    [SerializeField] private float _deceleration = 16f;   // durmak biraz daha hızlı
    [SerializeField] private CharacterController _characterController;
    private float _verticalVelocity;
    private float _gravity = -25f;
    private Vector3 _horizontalVelocity;

    [Header("Bakış")]
    [SerializeField] Camera _camera;
    private float _rotation = 0f;
    [SerializeField] private float _sensitivity = 0f;
    [SerializeField] private float _jumpHeight;

    [Header("Eğilme")]
    [SerializeField] private float _crouchHeight = 1f;
    private float _originalHeight;
    [SerializeField] private LayerMask _ceilingMask;
    [SerializeField] private float _crouchTransitionTime = 0.2f;   // eğilme / kalkma kaç saniye sürer

    [Header("Adım ve kamera sallanması")]
    [SerializeField] private float _walkStride = 0.7f;     // bir ayak sesi arası metre
    [SerializeField] private float _sprintStride = 0.95f;
    [SerializeField] private float _crouchStride = 0.65f;   // eğilince adım seyrek: ~0.46 sn'de bir
    [SerializeField] private bool _headBob = true;
    [SerializeField] private float _walkBobHeight = 0.03f;
    [SerializeField] private float _sprintBobHeight = 0.06f;
    [SerializeField] private float _bobSideways = 0.015f;

    // Ayak yere her değdiğinde. Footsteps buna abone olur.
    public event Action Stepped;

    private bool _wantsToStand = false;

    private float _targetHeight;
    private float _eyeHeight;      // eğilmeye göre kameranın kapsül içindeki yüksekliği
    private float _stepPhase;      // her adımda π ilerler; sallanma ve ses aynı ritmi kullanır
    private float _bobAmount;      // 0 = duruyor, 1 = tam sallanma

    private void Awake()
    {
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }
        _originalHeight = _characterController.height;
        _targetHeight = _originalHeight;
    }

    private void OnEnable()
    {
        InputService.Actions.Gameplay.Crouch.performed += OnPlayerCrouch;
        InputService.Actions.Gameplay.Crouch.canceled += OnPlayerCrouchCancelled;
    }

    private void OnDisable()
    {
        if (InputService.Actions == null) return;

        InputService.Actions.Gameplay.Crouch.performed -= OnPlayerCrouch;
        InputService.Actions.Gameplay.Crouch.canceled -= OnPlayerCrouchCancelled;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ApllyGravity();

        Look();

        ApplyCrouch();

        Move();

        UpdateStepAndCamera();
    }

    public bool IsCrouching()
    {
        return _targetHeight < _originalHeight || _characterController.height < _originalHeight - 0.01f;
    }

    public bool IsSprinting()
    {
        return !IsCrouching() && InputService.Actions.Gameplay.Sprint.IsPressed();
    }

    private void Move()
    {
        Vector2 inputVector = InputService.Actions.Gameplay.Move.ReadValue<Vector2>();

        float speed = _moveSpeed;
        if (IsCrouching()) speed = _crouchMoveSpeed;
        else if (IsSprinting()) speed = _sprintSpeed;

        // Hedef hıza bir anda değil, ivmeyle yaklaş: karaktere ağırlık verir.
        Vector3 wish = transform.right * inputVector.x + transform.forward * inputVector.y;
        wish = Vector3.ClampMagnitude(wish, 1f) * speed;
        float rate = wish.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, wish, rate * Time.deltaTime);

        Vector3 direction = _horizontalVelocity;
        direction.y = _verticalVelocity;
        _characterController.Move(direction * Time.deltaTime);
    }

    private void UpdateStepAndCamera()
    {
        // Gerçekten gidilen hız: duvara yürürken adım da sallanma da olmaz.
        Vector3 velocity = _characterController.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        bool walking = _characterController.isGrounded && speed > 0.1f;

        float stride = _walkStride;
        float bobHeight = _walkBobHeight;
        if (IsCrouching()) { stride = _crouchStride; bobHeight = _walkBobHeight * 0.6f; }
        else if (IsSprinting()) { stride = _sprintStride; bobHeight = _sprintBobHeight; }

        if (walking)
        {
            float previous = _stepPhase;
            _stepPhase += speed / stride * Mathf.PI * Time.deltaTime;
            // Faz her π'yi geçtiğinde bir ayak yere değmiştir.
            if (Mathf.Floor(_stepPhase / Mathf.PI) > Mathf.Floor(previous / Mathf.PI))
                Stepped?.Invoke();
        }

        // Durunca sallanma yumuşakça söner, yürürken hıza göre büyür.
        float targetBob = walking && _headBob ? Mathf.Clamp01(speed / _moveSpeed) : 0f;
        _bobAmount = Mathf.MoveTowards(_bobAmount, targetBob, Time.deltaTime * 4f);

        float up = Mathf.Abs(Mathf.Sin(_stepPhase)) * bobHeight * _bobAmount;   // adım ortasında en yüksek
        float side = Mathf.Sin(_stepPhase) * _bobSideways * _bobAmount;          // iki adımda bir sağ-sol
        _camera.transform.localPosition = new Vector3(side, _eyeHeight + up, 0f);
    }

    private void Look()
    {
        Vector2 rotationVector = InputService.Actions.Gameplay.Look.ReadValue<Vector2>();

        float mouseY = rotationVector.y;
        _rotation -= mouseY * _sensitivity;
        _rotation = Mathf.Clamp(_rotation, -80f, 80f);
        _camera.transform.localRotation = Quaternion.Euler(_rotation, 0f, 0f);

        float mouseX = rotationVector.x;
        transform.Rotate(Vector3.up * mouseX * _sensitivity, Space.World);
    }

    private void ApplyCrouch()
    {
        if (_wantsToStand && CanStandUp())
        {
            _targetHeight = _originalHeight;
            _wantsToStand = false;
        }

        // Sabit hızla git: Lerp hedefe yaklaştıkça yavaşlıyor, kalkma bitmiş görünüp
        // saniyelerce "eğilmiş" sayılıyordu. MoveTowards tam hedefte durur.
        float speed = (_originalHeight - _crouchHeight) / _crouchTransitionTime;
        float h = Mathf.MoveTowards(_characterController.height, _targetHeight, speed * Time.deltaTime);

        _characterController.height = h;
        _characterController.center = new Vector3(0f, (h - _originalHeight) / 2f, 0f);

        float eyeOffset = 0.15f;   // tepeden ne kadar aşağıda
        _eyeHeight = h / 2f - eyeOffset;
    }

    private void ApllyGravity()
    {
        if (_characterController.isGrounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = -1f;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }
    }

    private bool CanStandUp()
    {
        float radius = _characterController.radius * 0.95f;
        Vector3 origin = transform.position + Vector3.up * (_crouchHeight / 2f - _characterController.radius);

        float distance = _originalHeight - _crouchHeight;


        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, _ceilingMask);
    }

    private void OnPlayerCrouch(InputAction.CallbackContext ctx)
    {
        _targetHeight = _crouchHeight;
        _wantsToStand = false;
    }

    private void OnPlayerCrouchCancelled(InputAction.CallbackContext ctx)
    {
        _wantsToStand = true;
    }
}
