namespace Babodayo.Player.StateMachine
{
    public sealed class PlayerStateMachine
    {
        public PlayerState Current { get; private set; }
        public string Name => Current?.Name ?? "Uninitialized";
        public void Change(PlayerState next) { Current?.Exit(); Current=next; Current?.Enter(); }
        public void Tick() => Current?.Tick();
    }
}
