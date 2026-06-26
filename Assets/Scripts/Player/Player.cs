using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputHandler))]
public class Player : MonoBehaviour
{
  private Rigidbody2D _rb;
  private InputHandler _input;
  private Vector2 _moveInput;
  private float _originalGravity;

  [Header("Matar o inimigo")]
  [SerializeField] private GameObject _headCheck;

  [Header("Configurações de Movimento")]
  [SerializeField] private float _maxSpeed = 10f;
  [SerializeField] private float _acceleration = 42f;
  [SerializeField] private float _desacceleration = 38f;

  [Header("Configurações de Dash")]
  [SerializeField] private float _dashForce = 52f;
  [SerializeField] private float _dashTimer;
  [SerializeField] private float _dashTime = 0.2f;

  [Tooltip("Visual e Direção")]
  [SerializeField] private bool _isFacingRight = true;
  [SerializeField] private bool _isDashing = false;

  [Header("Configurações de Pulo")]
  [SerializeField] private float _coyoteTime = 0.45f;
  [Tooltip("Debug")]
  [SerializeField] private float _coyoteTimer;
  [SerializeField] private int _maxJump = 2;
  [Tooltip("Debug")]
  [SerializeField] private int _jumpCounter;
  [SerializeField] private float _jumpForce = 12f;

  [Header("WallSlide & WallJump")]
  [SerializeField] private Transform _wallCheck;
  [SerializeField] private float _wallSlideSpeed = 2f;
  [SerializeField] private Vector2 _wallJumpForce = new(10f, 12f);
  [Tooltip("Debug")]
  [SerializeField] private bool _isTouchingWall = false;
  [Tooltip("Debub")]
  [SerializeField] private bool _isWallSliding = false;

  [Header("Configurações do Sistema de Mira e Projéteis")]
  [SerializeField] private TextMeshProUGUI _aimText;
  [SerializeField] private GameObject _bulletPrefab;
  [SerializeField] private Transform _bulletPos;
  [SerializeField] private float _bulletSpeed = 18f;
  [SerializeField] private int _maxAmmo = 4;
  [SerializeField] private int _currentAmmo;
  [SerializeField] private bool _isAiming = false;
  private Vector2 _aimDirection;

  [Header("Configurações do Chão")]
  [SerializeField] private Transform _groundCheck;
  [SerializeField] private float _groundCheckRadius = 0.2f;
  [SerializeField] private LayerMask _groundLayer;
  [SerializeField] private bool _isGrounded;

  private void Awake()
  {
    _rb = GetComponent<Rigidbody2D>();
    _input = GetComponent<InputHandler>();
    _originalGravity = _rb.gravityScale;

    _jumpCounter = 0;
    _currentAmmo = _maxAmmo;
  }

  private void OnEnable()
  {
    if (_input != null)
    {
      _input.OnMoveInputChanged += HandleMoveInput;
      _input.OnJumpPressed += HandleJump;
      _input.OnDashPressed += HandleDash;
      _input.OnAimStateChanged += HandleAimState;
      _input.OnFirePressed += HandleFire;
    }
  }

  private void OnDisable()
  {
    if (_input != null)
    {
      _input.OnMoveInputChanged -= HandleMoveInput;
      _input.OnJumpPressed -= HandleJump;
      _input.OnDashPressed -= HandleDash;
      _input.OnAimStateChanged -= HandleAimState;
      _input.OnFirePressed -= HandleFire;
    }
  }

  private void Update()
  {
    ProcessAiming();
  }

  private void FixedUpdate()
  {
    CheckSurroundings();
    ManageCoyoteTime();

    if (_isDashing)
    {
      ProcessDashContinuation();
      return;
    }
    ManageWallSlide();
    ApplyMovement();
  }

  private void HandleMoveInput(Vector2 inputDirection)
  {
    if (_isAiming) return;

    _moveInput = inputDirection;

    if (_moveInput.x > 0 && !_isFacingRight)
    {
      Flip();
    }
    else if (_moveInput.x < 0 && _isFacingRight)
    {
      Flip();
    }
  }

  private void ManageCoyoteTime()
  {
    if (_isGrounded && _rb.linearVelocityY <= 0.1f)
    {
      _coyoteTimer = _coyoteTime;
      _jumpCounter = 0;
      _isDashing = false;
      _currentAmmo = _maxAmmo;
    }
    else
    {
      _coyoteTimer -= Time.fixedDeltaTime;
    }
  }

  private void Flip()
  {
    _isFacingRight = !_isFacingRight;

    Vector3 currentScale = transform.localScale;
    currentScale.x *= -1;
    transform.localScale = currentScale;
  }

  private void ApplyMovement()
  {
    if (_isAiming)
    {
      _rb.linearVelocityX = 0f;
      return;
    }

    float targetSpeed = _moveInput.x * _maxSpeed;
    float acelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? _acceleration : _desacceleration;
    float newVelocityX = Mathf.MoveTowards(_rb.linearVelocityX, targetSpeed, acelRate * Time.fixedDeltaTime);
    _rb.linearVelocityX = newVelocityX;
  }

  private void CheckSurroundings()
  {
    _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
    _isTouchingWall = Physics2D.OverlapCircle(_wallCheck.position, _groundCheckRadius, _groundLayer);
  }

  private void HandleJump()
  {
    if (_isAiming || _isDashing) return;

    if (_isWallSliding)
    {
      _rb.linearVelocity = Vector2.zero;

      float pushDirection = _isFacingRight ? -1f : 1f;

      _rb.AddForce(new Vector2(_wallJumpForce.x * pushDirection, _wallJumpForce.y), ForceMode2D.Impulse);
      Flip();
      return;
    }

    if ((_coyoteTimer > 0f || _jumpCounter < _maxJump) && _isDashing == false)
    {
      _rb.linearVelocityY = 0;
      _rb.AddForceY(_jumpForce, ForceMode2D.Impulse);
      _coyoteTimer = 0f;
      _jumpCounter++;
    }
  }

  private void HandleDash()
  {
    if (!_isGrounded && _jumpCounter > 0 && !_isDashing && !_isAiming)
    {
      _isDashing = true;
      _dashTimer = _dashTime;

      _rb.gravityScale = 0f;

      float dashDirection = _isFacingRight ? 1f : -1f;
      _rb.linearVelocity = new Vector2(_dashForce * dashDirection, 0f);
    }
  }

  private void ProcessDashContinuation()
  {
    _dashTimer -= Time.fixedDeltaTime;

    float dashDirection = _isFacingRight ? 1f : -1f;
    _rb.linearVelocity = new Vector2(_dashForce * dashDirection, 0f);

    if (_dashTimer <= 0f)
    {
      _isDashing = false;
      _rb.gravityScale = _originalGravity;
      _rb.linearVelocityX = 0f;
    }
  }

  private void ManageWallSlide()
  {
    if (_isAiming)
    {
      _isWallSliding = false;
      return;
    }

    if (_isTouchingWall && !_isGrounded && _rb.linearVelocityY < 0f && _moveInput.x != 0)
    {
      _isWallSliding = true;

      _rb.linearVelocityY = Mathf.Max(_rb.linearVelocityY, -_wallSlideSpeed);
    }
    else
    {
      _isWallSliding = false;
    }
  }

  private void HandleAimState(bool isAiming)
  {
    _isAiming = isAiming;

    if (_aimText != null)
    {
      // Reseta a rotação ao cancelar a mira
      if (!isAiming)
      {
        _aimText.transform.localRotation = Quaternion.identity;
      }
    }
  }

  private void ProcessAiming()
  {
    if (!_isAiming) return;

    Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
    mouseWorldPos.z = 0f;

    Vector2 lookDirection = (mouseWorldPos - transform.position).normalized;

    if (lookDirection.x > 0 && !_isFacingRight) Flip();
    else if (lookDirection.x < 0 && _isFacingRight) Flip();

    float facingSign = _isFacingRight ? 1f : -1f;
    float angle = Mathf.Atan2(lookDirection.y, lookDirection.x * facingSign) * Mathf.Rad2Deg;

    // Guardará o ângulo em graus que o GameObject do TextMeshPro deve rotacionar no eixo Z
    float localZRotation = 0f;

    if (angle > 30f)
    {
      _aimDirection = new Vector2(facingSign, 1f).normalized;
      localZRotation = 45f; // Diagonal para cima
    }
    else if (angle < -30f)
    {
      _aimDirection = new Vector2(facingSign, -1f).normalized;
      localZRotation = -45f; // Diagonal para baixo
    }
    else
    {
      _aimDirection = new Vector2(facingSign, 0f).normalized;
      localZRotation = 0f; // Reto para frente
    }

    // ALTERADO: Aplica a rotação local baseada nos 3 pontos limitados
    if (_aimText != null)
    {
      _aimText.transform.localRotation = Quaternion.Euler(0f, 0f, localZRotation);
    }
  }

  private void HandleFire()
  {
    if (!_isAiming || _currentAmmo <= 0) return;

    _currentAmmo--;

    if (_bulletPrefab != null)
    {
      // ALTERADO: Agora pega a posição exata do seu objeto vazio customizado!
      // Se você esquecer de arrastar o objeto no Inspector, ele usa o centro do Player como precaução.
      Vector3 spawnPos = _bulletPos != null ? _bulletPos.position : transform.position;

      GameObject projectible = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);

      Rigidbody2D projRb = projectible.GetComponent<Rigidbody2D>();
      if (projRb != null)
      {
        projRb.linearVelocity = _aimDirection * _bulletSpeed;
      }
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    if (_groundCheck != null) Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);

    Gizmos.color = Color.blue;
    if (_wallCheck != null) Gizmos.DrawWireSphere(_wallCheck.position, _groundCheckRadius);
  }

  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Enemy"))
    {
      // CORRIGIDO: Removido o CompareTag duplicado de dentro da validação do objeto que quebrava a lógica.
      if (other.otherCollider.gameObject == _headCheck.CompareTag("KillEnemy"))
      {
        Destroy(other.gameObject);

        // Bônus de Game Feel: Dá um pequeno impulso para cima ao pisar na cabeça do inimigo
        _rb.linearVelocityY = 0f;
        _rb.AddForceY(_jumpForce * 0.7f, ForceMode2D.Impulse);
      }
      else
      {
        // Se tocou no inimigo por qualquer outro lado (corpo, lados), o Player morre
        Destroy(gameObject);
      }
    }
  }
}
