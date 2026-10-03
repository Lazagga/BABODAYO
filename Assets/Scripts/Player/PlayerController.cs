using System;
using Babodayo.Core;
using Babodayo.Input;
using UnityEngine;
using Babodayo.Player.StateMachine;

namespace Babodayo.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Scene dependencies")]
        [SerializeField, Tooltip("The shared scene combat clock. Assign explicitly.")]
        private CombatClock clock = null;
        [SerializeField, Tooltip("This player's exclusive input reader. Assign explicitly.")]
        private PlayerInputReader inputReader = null;
        [SerializeField, Tooltip("Motor that owns player physics and movement tuning.")]
        public PlayerMotor Motor;
        [Tooltip("Cursor facing and visual orientation.")] public PlayerFacing FacingControl;
        public PlayerStateMachine States { get; private set; }
        public PlayerLocomotion Locomotion { get; private set; }
        public int Facing { get; set; } = 1;
        public float MoveInput => CurrentInput.Horizontal;
        public CombatCommand LastCommand { get; private set; }
        public event Action<CombatCommand,int> CommandExecuted;
        public Func<bool> CombatAction;
        public Func<bool> IsFrozen;
        [Header("Designer: input tuning")]
        [SerializeField, Range(20, 30), Tooltip("Number of recent combat frames retained for command recognition.")]
        private int historyFrames = 30;
        [SerializeField, Tooltip("Control-pattern recognition settings; unrelated to attack move data.")]
        private CommandParserSettings parserSettings = new CommandParserSettings();
        [SerializeField, Tooltip("Command lifetimes in combat frames.")]
        private CommandBufferSettings bufferSettings = new CommandBufferSettings();

        private InputHistory history;
        private CommandParser parser;
        public CommandBuffer Commands { get; private set; }
        public InputFrame CurrentInput { get; private set; }
        public int CurrentFrame => clock != null ? clock.CurrentFrame : 0;
        // Future state machine ticks here and consumes Commands; it never reads Input System.
        public event Action<int> StateTick;

        private void Awake()
        {
            history = new InputHistory(Mathf.Clamp(Mathf.Max(historyFrames, parserSettings.DoubleTapFrames + 2), 20, 30));
            parser = new CommandParser(parserSettings);
            Commands = new CommandBuffer(bufferSettings);
            States = new PlayerStateMachine();
            Locomotion = new PlayerLocomotion(this);
            States.Change(new GroundIdleState(this));
        }
        private void OnEnable()
        {
            if (clock == null || inputReader == null)
            {
                Debug.LogError("Assign CombatClock and PlayerInputReader to PlayerController.", this);
                enabled = false;
                return;
            }
            history.Clear(); parser.Reset(); Commands.Clear(); inputReader.ClearPending();
            CurrentInput = default;
            // Seed neutral so the first sampled direction can be the first tap.
            history.Add(new InputFrame(clock.CurrentFrame, 0, 0,
                InputButtons.None, InputButtons.None, InputButtons.None));
            clock.InputTick += PrepareInput;
            clock.SimulationTick += TickState;
        }
        private void OnDisable()
        {
            if (clock != null)
            {
                clock.InputTick -= PrepareInput;
                clock.SimulationTick -= TickState;
            }
            Commands?.Clear();
        }
        private void PrepareInput(int frame)
        {
            CurrentInput = inputReader.Capture(frame);
            history.Add(CurrentInput);
            if(IsFrozen?.Invoke() ?? false) Commands.DeferExpiration(1);
            Commands.Expire(frame);
            parser.Parse(history, Commands);
        }
        private void TickState(int frame)
        {
            if (Motor == null || Motor.Data == null) return;
            if (IsFrozen != null && IsFrozen()) return;
            Locomotion.Sample();
            if (!(States.Current is GroundDashState)) UpdateFacing(!(States.Current is AttackState));
            States.Tick();
            FacingControl?.Present(Facing);
            StateTick?.Invoke(frame);
        }
        public void UpdateFacing(bool moving)
        {
            if (FacingControl != null) Facing = FacingControl.Resolve(CurrentInput, transform.position, Facing, moving);
            else if (moving && MoveInput != 0) Facing = MoveInput < 0 ? -1 : 1;
        }
        public bool Consume(CombatCommand command)
        {
            if (!Commands.TryConsume(command,CurrentFrame,out _)) return false;
            LastCommand=command; CommandExecuted?.Invoke(command,CurrentFrame); return true;
        }
        public bool TryActions() => Locomotion.TryJump() || Locomotion.TryDash() || (CombatAction?.Invoke() ?? false);
        public void ReturnToLocomotion()
        {
            States.Change(Motor.Grounded ? (MoveInput==0 ? (PlayerState)new GroundIdleState(this) : new GroundMoveState(this)) :
                Motor.Velocity.y>0 ? (PlayerState)new JumpState(this) : new FallState(this));
        }
        public void Configure(CombatClock value,PlayerInputReader reader,PlayerMotor motor)
        { clock=value; inputReader=reader; Motor=motor; }
    }
}
