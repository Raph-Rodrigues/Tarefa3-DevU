using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputHandler))]
public class Player : MonoBehaviour
{
  private Rigidbody2D _rb;
  private InputHandler _input;
  private Vector2 _moveInput;

  [Header("Configurações de Movimento")]
  [SerializeField] private float _maxSpeed = 10f;
  [SerializeField] private float _acceleration = 42f;
  [SerializeField] private float _desacceleration = 38f;
  [Tooltip("Visual e Direção")]
  [SerializeField] private bool _isFacingRight = true;

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

  [Header("Configurações do Chão")]
  [SerializeField] private Transform _groundCheck;
  [SerializeField] private float _groundCheckRadius = 0.2f;
  [SerializeField] private LayerMask _groundLayer;
  [SerializeField] private bool _isGrounded;

  private void Awake()
  {
    _rb = GetComponent<Rigidbody2D>();
    _input = GetComponent<InputHandler>();

    _jumpCounter = 0;
  }

  private void OnEnable()
  {
    if (_input != null)
    {
      _input.OnMoveInputChanged += HandleMoveInput;
      _input.OnJumpPressed += HandleJump;
    }
  }

  private void OnDisable()
  {
    if (_input != null)
    {
      _input.OnMoveInputChanged -= HandleMoveInput;
      _input.OnJumpPressed -= HandleJump;
    }
  }

  private void FixedUpdate()
  {
    CheckSurroundings();
    ManageCoyoteTime();
    ManageWallSlide();
    ApplyMovement();
  }

  private void HandleMoveInput(Vector2 inputDirection)
  {
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
    if (_isWallSliding)
    {
      _rb.linearVelocity = Vector2.zero;

      float pushDirection = _isFacingRight ? -1f : 1f;

      _rb.AddForce(new Vector2(_wallJumpForce.x * pushDirection, _wallJumpForce.y), ForceMode2D.Impulse);
      Flip();
      return;
    }

    if (_coyoteTimer > 0f || _jumpCounter < _maxJump)
    {
      _rb.linearVelocityY = 0;
      _rb.AddForceY(_jumpForce, ForceMode2D.Impulse);
      _coyoteTimer = 0f;
      _jumpCounter++;
    }
  }

  private void ManageWallSlide()
  {
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

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    if (_groundCheck != null) Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);

    Gizmos.color = Color.blue;
    if (_wallCheck != null) Gizmos.DrawWireSphere(_wallCheck.position, _groundCheckRadius);
  }
}
