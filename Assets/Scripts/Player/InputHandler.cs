using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
  private PlayerControls _controls;

  public event Action<Vector2> OnMoveInputChanged;
  public event Action OnJumpPressed;
  public event Action OnDashPressed;
  public event Action<bool> OnAimStateChanged;
  public event Action OnFirePressed;

  private void Awake()
  {
    _controls = new PlayerControls();

    _controls.Player.Move.performed += ctx => SendMoveInput(ctx.ReadValue<Vector2>());
    _controls.Player.Move.canceled += ctx => SendMoveInput(Vector2.zero);

    _controls.Player.Jump.performed += ctx => TriggerJump();
    _controls.Player.Sprint.performed += ctx => TriggerDash();

    _controls.Player.Look.performed += ctx => OnAimStateChanged?.Invoke(true);
    _controls.Player.Look.canceled += ctx => OnAimStateChanged?.Invoke(false);
    _controls.Player.Attack.performed += ctx => OnFirePressed?.Invoke();
  }

  private void OnEnable()
  {
    _controls.Player.Enable();
  }

  private void OnDisable()
  {
    _controls.Player.Disable();
  }

  private void SendMoveInput(Vector2 inputDirection)
  {
    OnMoveInputChanged?.Invoke(inputDirection);
  }

  private void TriggerJump()
  {
    OnJumpPressed?.Invoke();
  }

  private void TriggerDash()
  {
    OnDashPressed?.Invoke();
  }
}
