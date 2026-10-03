using UnityEngine;
using UnityEngine.InputSystem;

namespace Babodayo.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Designer: bindings (button actions must have no Hold/Tap interactions)")]
        [SerializeField, Tooltip("Continuous movement. Default: WASD.")]
        private InputAction move = CreateMoveAction();
        [SerializeField, Tooltip("Primary attack button. Default: left mouse button.")]
        private InputAction lmb = new InputAction("LMB", InputActionType.Button, "<Mouse>/leftButton");
        [SerializeField, Tooltip("Shoot button. Default: right mouse button.")]
        private InputAction rmb = new InputAction("RMB", InputActionType.Button, "<Mouse>/rightButton");
        [SerializeField, Tooltip("Fixed-height jump button. Default: Space.")]
        private InputAction jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        [SerializeField, Tooltip("Attack modifier. Default: left Shift.")]
        private InputAction shift = new InputAction("Shift", InputActionType.Button, "<Keyboard>/leftShift");
        [SerializeField, Range(0.1f, 0.9f), Tooltip("Axis threshold used to quantize direction to -1 / 0 / +1.")]
        private float directionThreshold = 0.5f;

        [SerializeField, Tooltip("Screen-space cursor for horizontal facing.")]
        private InputAction pointer = new InputAction("Pointer", InputActionType.Value, "<Mouse>/position");
        private InputButtons pressed, released;
        private int lmbHorizontal, lmbVertical;
        private bool lmbShiftHeld;
        private bool hasFocus = true;

        private static InputAction CreateMoveAction()
        {
            var action = new InputAction("Move", InputActionType.Value);
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            return action;
        }

        private void OnEnable()
        {
            lmb.performed += OnLmb; lmb.canceled += OnLmb;
            rmb.performed += OnRmb; rmb.canceled += OnRmb;
            jump.performed += OnJump; jump.canceled += OnJump;
            shift.performed += OnShift; shift.canceled += OnShift;
            move.Enable(); lmb.Enable(); rmb.Enable(); jump.Enable(); shift.Enable(); pointer.Enable();
            ClearPending();
        }
        private void OnDisable()
        {
            lmb.performed -= OnLmb; lmb.canceled -= OnLmb;
            rmb.performed -= OnRmb; rmb.canceled -= OnRmb;
            jump.performed -= OnJump; jump.canceled -= OnJump;
            shift.performed -= OnShift; shift.canceled -= OnShift;
            move.Disable(); lmb.Disable(); rmb.Disable(); jump.Disable(); shift.Disable(); pointer.Disable();
            ClearPending();
        }
        private void OnDestroy()
        {
            move.Dispose(); lmb.Dispose(); rmb.Dispose(); jump.Dispose(); shift.Dispose(); pointer.Dispose();
        }
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            ClearPending();
        }
        private void OnLmb(InputAction.CallbackContext context)
        {
            if (hasFocus && context.performed && (pressed & InputButtons.LMB) == 0)
            {
                Vector2 direction = move.ReadValue<Vector2>();
                lmbHorizontal = Quantize(direction.x); lmbVertical = Quantize(direction.y);
                lmbShiftHeld = shift.IsPressed();
            }
            Record(context, InputButtons.LMB);
        }
        private void OnRmb(InputAction.CallbackContext context) => Record(context, InputButtons.RMB);
        private void OnJump(InputAction.CallbackContext context) => Record(context, InputButtons.Jump);
        private void OnShift(InputAction.CallbackContext context) => Record(context, InputButtons.Shift);
        private void Record(InputAction.CallbackContext context, InputButtons button)
        {
            if (!hasFocus) return;
            if (context.performed) pressed |= button;
            else released |= button;
        }

        // Called only by the owning controller, once per combat tick.
        public InputFrame Capture(int frame)
        {
            if (!isActiveAndEnabled || !hasFocus)
                return new InputFrame(frame, 0, 0, InputButtons.None, InputButtons.None, InputButtons.None);
            Vector2 direction = move.ReadValue<Vector2>();
            InputButtons held = InputButtons.None;
            if (lmb.IsPressed()) held |= InputButtons.LMB;
            if (rmb.IsPressed()) held |= InputButtons.RMB;
            if (jump.IsPressed()) held |= InputButtons.Jump;
            if (shift.IsPressed()) held |= InputButtons.Shift;
            var result = new InputFrame(frame, Quantize(direction.x), Quantize(direction.y), pressed, held, released,
                lmbHorizontal, lmbVertical, lmbShiftHeld, pointer.ReadValue<Vector2>(), pointer.controls.Count > 0);
            ClearPending();
            return result;
        }
        public void ClearPending() { pressed = InputButtons.None; released = InputButtons.None; }
        private int Quantize(float axis) => axis >= directionThreshold ? 1 : axis <= -directionThreshold ? -1 : 0;
    }
}
