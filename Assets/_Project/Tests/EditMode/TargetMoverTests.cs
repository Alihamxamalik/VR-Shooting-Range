using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Targets;

namespace RangeControl.Tests
{
    public class TargetMoverTests
    {
        const float k_Frame = 1f / 72f;
        const float k_Rail = 8f;

        static MotionState NewState(uint seed = 42, float exposure = 1.5f)
        {
            return new MotionState { RailLength = k_Rail, Direction = 1f, Exposure = exposure, Rng = new Rng(seed) };
        }

        /// <summary>Runs a mover until it finishes and returns how long that took, or -1 if it outlived the limit.</summary>
        static float RunToEnd(ITargetMover mover, ref MotionState state, float speed, float limitSeconds = 60f)
        {
            mover.Begin(ref state);
            for (var time = 0f; time < limitSeconds; time += k_Frame)
            {
                if (!mover.Tick(ref state, speed, k_Frame))
                    return time + k_Frame;
                Assert.That(state.Position, Is.InRange(0f, k_Rail), "target left its rail");
            }

            return -1f;
        }

        [TestCase(1f)]
        [TestCase(2.5f)]
        [TestCase(4f)]
        public void Linear_CrossesTheRailInLengthOverSpeed(float speed)
        {
            var state = NewState();
            var seconds = RunToEnd(TargetMovers.For(MovementPattern.LeftToRight), ref state, speed);
            Assert.That(seconds, Is.EqualTo(k_Rail / speed).Within(2f * k_Frame));
        }

        [Test]
        public void Linear_StartsAtTheRailStart()
        {
            var state = NewState();
            TargetMovers.For(MovementPattern.LeftToRight).Begin(ref state);
            Assert.That(state.Position, Is.EqualTo(0f));
        }

        [Test]
        public void PingPong_TurnsAtBothEndsAndLeavesAfterItsPasses()
        {
            var state = NewState();
            var seconds = RunToEnd(TargetMovers.For(MovementPattern.PingPong), ref state, 4f);
            Assert.That(seconds, Is.EqualTo(TargetMovers.PingPongPasses * k_Rail / 4f).Within(4f * k_Frame));
            Assert.That(state.Passes, Is.EqualTo(TargetMovers.PingPongPasses));
        }

        [Test]
        public void Strafe_StaysOnTheRailAndLeavesAfterItsLifetime()
        {
            for (uint seed = 1; seed <= 25; seed++)
            {
                var state = NewState(seed);
                var seconds = RunToEnd(TargetMovers.For(MovementPattern.Strafe), ref state, 6f);
                Assert.That(seconds, Is.EqualTo(TargetMovers.StrafeLifetime).Within(2f * k_Frame));
            }
        }

        [Test]
        public void Strafe_ActuallyMoves()
        {
            var state = NewState(7);
            var mover = TargetMovers.For(MovementPattern.Strafe);
            mover.Begin(ref state);
            var start = state.Position;
            var travelled = 0f;
            for (var i = 0; i < 144; i++)
            {
                var before = state.Position;
                mover.Tick(ref state, 3f, k_Frame);
                travelled += System.Math.Abs(state.Position - before);
            }

            Assert.That(travelled, Is.GreaterThan(1f), "started at " + start);
        }

        [Test]
        public void PopUp_StaysForItsExposureAndDoesNotTravel()
        {
            var state = NewState(3, 1.25f);
            var mover = TargetMovers.For(MovementPattern.PopUp);
            mover.Begin(ref state);
            var spot = state.Position;
            var seconds = 0f;
            while (mover.Tick(ref state, 4f, k_Frame))
                seconds += k_Frame;

            Assert.That(seconds, Is.EqualTo(1.25f).Within(2f * k_Frame));
            Assert.That(state.Position, Is.EqualTo(spot));
        }

        [Test]
        public void Static_StandsForTheDefaultLifetimeWhenNoneIsGiven()
        {
            var state = NewState(5, 0f);
            var seconds = RunToEnd(TargetMovers.For(MovementPattern.Static), ref state, 0f);
            Assert.That(seconds, Is.EqualTo(TargetMovers.StaticLifetime).Within(2f * k_Frame));
        }

        [Test]
        public void StandingTargets_SpawnAtDifferentSpotsForDifferentSeeds()
        {
            var first = NewState(1);
            var second = NewState(2);
            TargetMovers.For(MovementPattern.Static).Begin(ref first);
            TargetMovers.For(MovementPattern.Static).Begin(ref second);
            Assert.That(first.Position, Is.Not.EqualTo(second.Position));
        }

        [TestCase(MovementPattern.LeftToRight)]
        [TestCase(MovementPattern.PingPong)]
        [TestCase(MovementPattern.Strafe)]
        public void TravellingTarget_StalledAtZeroSpeed_EventuallyLeaves(MovementPattern pattern)
        {
            var state = NewState();
            var seconds = RunToEnd(TargetMovers.For(pattern), ref state, 0f);
            Assert.That(seconds, Is.EqualTo(TargetMovers.StalledLifetime).Within(2f * k_Frame));
        }

        [Test]
        public void SpeedChangeTakesEffectMidRun()
        {
            var state = NewState();
            var mover = TargetMovers.For(MovementPattern.LeftToRight);
            mover.Begin(ref state);
            for (var i = 0; i < 72; i++)
                mover.Tick(ref state, 1f, k_Frame);
            Assert.That(state.Position, Is.EqualTo(1f).Within(0.01f));

            for (var i = 0; i < 72; i++)
                mover.Tick(ref state, 4f, k_Frame);
            Assert.That(state.Position, Is.EqualTo(5f).Within(0.02f));
        }

        [Test]
        public void EffectivePattern_TurnsTravelIntoStaticAtZeroSpeed()
        {
            Assert.That(TargetMovers.Effective(MovementPattern.LeftToRight, 0f), Is.EqualTo(MovementPattern.Static));
            Assert.That(TargetMovers.Effective(MovementPattern.Strafe, 0f), Is.EqualTo(MovementPattern.Static));
            Assert.That(TargetMovers.Effective(MovementPattern.LeftToRight, 1f), Is.EqualTo(MovementPattern.LeftToRight));
            Assert.That(TargetMovers.Effective(MovementPattern.PopUp, 0f), Is.EqualTo(MovementPattern.PopUp));
        }
    }
}
