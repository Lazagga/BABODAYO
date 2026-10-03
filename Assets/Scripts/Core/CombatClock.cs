using System;
using UnityEngine;

namespace Babodayo.Core
{
    [DisallowMultipleComponent]
    public sealed class CombatClock : MonoBehaviour
    {
        public const int FramesPerSecond = 60;
        public const float FrameDuration = 1f / FramesPerSecond;
        public int CurrentFrame { get; private set; }
        // Input for every actor is prepared before any actor simulation runs.
        public event Action<int> InputTick;
        public event Action<int> SimulationTick;
        public event Action<int> CompletedTick;
        private SimulationMode2D previousSimulation;

        private void Awake()
        {
            previousSimulation=Physics2D.simulationMode;
            Physics2D.simulationMode=SimulationMode2D.Script;
            if (Mathf.Abs(Time.fixedDeltaTime - FrameDuration) > 0.00001f)
            {
                Debug.LogError("CombatClock requires Fixed Timestep = 1/60 seconds.", this);
                enabled = false;
            }
        }

        private void FixedUpdate() => Step();
        public void Step()
        {
            CurrentFrame++;
            InputTick?.Invoke(CurrentFrame);
            SimulationTick?.Invoke(CurrentFrame);
            Physics2D.SyncTransforms();
            Physics2D.Simulate(FrameDuration);
            CompletedTick?.Invoke(CurrentFrame);
        }
        private void OnDestroy() { Physics2D.simulationMode=previousSimulation; }
    }
}
