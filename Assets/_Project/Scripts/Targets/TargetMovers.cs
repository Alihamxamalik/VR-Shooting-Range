using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>Where a target is along its rail. A plain value, so a pooled target carries it without allocating.</summary>
    public struct MotionState
    {
        public float RailLength;
        /// <summary>Metres from the start of the rail.</summary>
        public float Position;
        /// <summary>+1 toward the rail's end, -1 back toward its start.</summary>
        public float Direction;
        public float Elapsed;
        /// <summary>Seconds spent standing still because the panel speed is zero.</summary>
        public float Stalled;
        /// <summary>Seconds left in the current strafe burst or pause.</summary>
        public float PhaseTimer;
        public bool Paused;
        public int Passes;
        /// <summary>Seconds a standing target stays up.</summary>
        public float Exposure;
        public Rng Rng;
    }

    /// <summary>
    /// One movement pattern. Movers are stateless and shared: all per-target state lives in
    /// <see cref="MotionState"/>, and the speed comes in every tick so a panel change takes effect at once.
    /// </summary>
    public interface ITargetMover
    {
        void Begin(ref MotionState state);

        /// <summary>Advances the target. Returns false once it has run its course and should leave.</summary>
        bool Tick(ref MotionState state, float speed, float deltaTime);
    }

    public static class TargetMovers
    {
        /// <summary>Below this speed a moving pattern is treated as standing still.</summary>
        public const float MinMovingSpeed = 0.05f;
        public const float StaticLifetime = 5f;
        public const float StalledLifetime = 5f;
        public const float StrafeLifetime = 8f;
        public const int PingPongPasses = 3;

        static readonly ITargetMover s_Static = new StaticMover();
        static readonly ITargetMover s_Linear = new LinearMover();
        static readonly ITargetMover s_PingPong = new PingPongMover();
        static readonly ITargetMover s_Strafe = new StrafeMover();
        static readonly ITargetMover s_PopUp = new PopUpMover();

        public static ITargetMover For(MovementPattern pattern)
        {
            switch (pattern)
            {
                case MovementPattern.LeftToRight: return s_Linear;
                case MovementPattern.PingPong: return s_PingPong;
                case MovementPattern.Strafe: return s_Strafe;
                case MovementPattern.PopUp: return s_PopUp;
                default: return s_Static;
            }
        }

        /// <summary>
        /// The pattern to actually use: a travelling pattern at zero speed becomes Static, so targets spread
        /// along the rail instead of stacking at its start.
        /// </summary>
        public static MovementPattern Effective(MovementPattern pattern, float speed)
        {
            var travels = pattern == MovementPattern.LeftToRight || pattern == MovementPattern.PingPong || pattern == MovementPattern.Strafe;
            return travels && speed < MinMovingSpeed ? MovementPattern.Static : pattern;
        }

        static bool Stall(ref MotionState state, float deltaTime)
        {
            state.Stalled += deltaTime;
            return state.Stalled < StalledLifetime;
        }

        /// <summary>Stands at a random spot on the rail until its exposure runs out.</summary>
        sealed class StaticMover : ITargetMover
        {
            public void Begin(ref MotionState state)
            {
                state.Position = state.Rng.Range(0.1f, 0.9f) * state.RailLength;
                if (state.Exposure <= 0f)
                    state.Exposure = StaticLifetime;
            }

            public bool Tick(ref MotionState state, float speed, float deltaTime)
            {
                state.Elapsed += deltaTime;
                return state.Elapsed < state.Exposure;
            }
        }

        /// <summary>Crosses the rail once, start to end.</summary>
        sealed class LinearMover : ITargetMover
        {
            public void Begin(ref MotionState state)
            {
                state.Position = 0f;
                state.Direction = 1f;
            }

            public bool Tick(ref MotionState state, float speed, float deltaTime)
            {
                state.Elapsed += deltaTime;
                if (speed < MinMovingSpeed)
                    return Stall(ref state, deltaTime);

                state.Position += speed * deltaTime;
                return state.Position < state.RailLength;
            }
        }

        /// <summary>Runs back and forth between the rail ends for a fixed number of passes.</summary>
        sealed class PingPongMover : ITargetMover
        {
            public void Begin(ref MotionState state)
            {
                state.Position = 0f;
                state.Direction = 1f;
                state.Passes = 0;
            }

            public bool Tick(ref MotionState state, float speed, float deltaTime)
            {
                state.Elapsed += deltaTime;
                if (speed < MinMovingSpeed)
                    return Stall(ref state, deltaTime);

                state.Position += state.Direction * speed * deltaTime;
                if (state.Position >= state.RailLength)
                {
                    state.Position = Mathf.Max(0f, 2f * state.RailLength - state.Position);
                    state.Direction = -1f;
                    state.Passes++;
                }
                else if (state.Position <= 0f)
                {
                    state.Position = Mathf.Min(state.RailLength, -state.Position);
                    state.Direction = 1f;
                    state.Passes++;
                }

                return state.Passes < PingPongPasses;
            }
        }

        /// <summary>Moves in short bursts with random pauses and changes of direction, like someone dodging.</summary>
        sealed class StrafeMover : ITargetMover
        {
            const float k_MinBurst = 0.4f;
            const float k_MaxBurst = 1.2f;
            const float k_MinPause = 0.2f;
            const float k_MaxPause = 0.5f;
            const float k_PauseChance = 0.35f;

            public void Begin(ref MotionState state)
            {
                state.Position = state.Rng.Range(0.2f, 0.8f) * state.RailLength;
                state.Direction = state.Rng.Sign();
                state.PhaseTimer = state.Rng.Range(k_MinBurst, k_MaxBurst);
                state.Paused = false;
            }

            public bool Tick(ref MotionState state, float speed, float deltaTime)
            {
                state.Elapsed += deltaTime;
                if (speed < MinMovingSpeed)
                    return Stall(ref state, deltaTime);

                state.PhaseTimer -= deltaTime;
                if (state.PhaseTimer <= 0f)
                {
                    state.Paused = !state.Paused && state.Rng.NextFloat() < k_PauseChance;
                    if (state.Paused)
                    {
                        state.PhaseTimer = state.Rng.Range(k_MinPause, k_MaxPause);
                    }
                    else
                    {
                        state.Direction = state.Rng.Sign();
                        state.PhaseTimer = state.Rng.Range(k_MinBurst, k_MaxBurst);
                    }
                }

                if (!state.Paused)
                {
                    state.Position += state.Direction * speed * deltaTime;
                    if (state.Position >= state.RailLength)
                    {
                        state.Position = state.RailLength;
                        state.Direction = -1f;
                    }
                    else if (state.Position <= 0f)
                    {
                        state.Position = 0f;
                        state.Direction = 1f;
                    }
                }

                return state.Elapsed < StrafeLifetime;
            }
        }

        /// <summary>Holds a random spot for the exposure time. The target itself animates the rise and the drop.</summary>
        sealed class PopUpMover : ITargetMover
        {
            public void Begin(ref MotionState state)
            {
                state.Position = state.Rng.Range(0.05f, 0.95f) * state.RailLength;
            }

            public bool Tick(ref MotionState state, float speed, float deltaTime)
            {
                state.Elapsed += deltaTime;
                return state.Elapsed < state.Exposure;
            }
        }
    }
}
