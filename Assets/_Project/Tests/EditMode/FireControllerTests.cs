using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Weapons;

namespace RangeControl.Tests
{
    public class FireControllerTests
    {
        const float k_Frame = 1f / 72f;

        static int Run(FireController fire, float trigger, float seconds, FireMode mode, float roundsPerMinute)
        {
            var shots = 0;
            for (var time = 0f; time < seconds; time += k_Frame)
            {
                if (fire.Tick(trigger, k_Frame, mode, roundsPerMinute))
                    shots++;
            }

            return shots;
        }

        [Test]
        public void Semi_FiresOncePerPullHoweverLongItIsHeld()
        {
            var fire = new FireController();
            Assert.That(Run(fire, 1f, 2f, FireMode.Semi, 400f), Is.EqualTo(1));
        }

        [Test]
        public void Semi_FiresAgainAfterTheTriggerIsReleased()
        {
            var fire = new FireController();
            var shots = Run(fire, 1f, 0.3f, FireMode.Semi, 400f);
            shots += Run(fire, 0f, 0.3f, FireMode.Semi, 400f);
            shots += Run(fire, 1f, 0.3f, FireMode.Semi, 400f);

            Assert.That(shots, Is.EqualTo(2));
        }

        [Test]
        public void Trigger_DoesNotFireBelowEightyPercent()
        {
            var fire = new FireController();
            Assert.That(Run(fire, 0.79f, 1f, FireMode.Semi, 400f), Is.EqualTo(0));
            Assert.That(Run(fire, 0.8f, 1f, FireMode.Semi, 400f), Is.EqualTo(1));
        }

        [Test]
        public void Trigger_DoesNotRearmUntilItFallsBelowThirtyPercent()
        {
            var fire = new FireController();
            var shots = Run(fire, 1f, 0.3f, FireMode.Semi, 400f);
            // A finger that only relaxes to 50% and squeezes again is one pull, not two.
            shots += Run(fire, 0.5f, 0.3f, FireMode.Semi, 400f);
            shots += Run(fire, 1f, 0.3f, FireMode.Semi, 400f);
            Assert.That(shots, Is.EqualTo(1));

            shots += Run(fire, 0.29f, 0.3f, FireMode.Semi, 400f);
            shots += Run(fire, 1f, 0.3f, FireMode.Semi, 400f);
            Assert.That(shots, Is.EqualTo(2));
        }

        [Test]
        public void Semi_IsCappedAtTheRateOfFire()
        {
            var fire = new FireController();
            var shots = 0;
            // Pull and release every frame for one second: far faster than 60 rounds per minute allows.
            for (var frame = 0; frame < 72; frame++)
            {
                if (fire.Tick(frame % 2 == 0 ? 1f : 0f, k_Frame, FireMode.Semi, 60f))
                    shots++;
            }

            Assert.That(shots, Is.EqualTo(1));
        }

        [TestCase(600f)]
        [TestCase(650f)]
        [TestCase(240f)]
        public void Auto_HoldsItsRateOfFire(float roundsPerMinute)
        {
            var fire = new FireController();
            var shots = Run(fire, 1f, 6f, FireMode.Auto, roundsPerMinute);
            Assert.That(shots, Is.EqualTo(roundsPerMinute / 10f).Within(1.5f));
        }

        [Test]
        public void Auto_StopsWhenTheTriggerIsReleased()
        {
            var fire = new FireController();
            Run(fire, 1f, 0.5f, FireMode.Auto, 600f);
            Assert.That(Run(fire, 0f, 1f, FireMode.Auto, 600f), Is.EqualTo(0));
        }

        [Test]
        public void Auto_DoesNotBankShotsWhileIdle()
        {
            var fire = new FireController();
            Run(fire, 0f, 5f, FireMode.Auto, 600f);
            // After a long pause the first frames of a pull must give one shot, not a burst of catch-up shots.
            Assert.That(Run(fire, 1f, 3f * k_Frame, FireMode.Auto, 600f), Is.EqualTo(1));
        }

        [Test]
        public void Interrupt_StopsAutoFireUntilTheNextPull()
        {
            var fire = new FireController();
            Run(fire, 1f, 0.5f, FireMode.Auto, 600f);
            fire.Interrupt();
            Assert.That(Run(fire, 1f, 1f, FireMode.Auto, 600f), Is.EqualTo(0));

            Run(fire, 0f, 0.1f, FireMode.Auto, 600f);
            Assert.That(Run(fire, 1f, 0.5f, FireMode.Auto, 600f), Is.GreaterThan(1));
        }

        [Test]
        public void Bloom_GrowsPerShotUpToItsMaxAndSettlesInTheRecoveryTime()
        {
            var bloom = new SpreadBloom();
            for (var shot = 0; shot < 20; shot++)
                bloom.AddShot(0.25f, 2.5f);
            Assert.That(bloom.Value, Is.EqualTo(2.5f));

            bloom.Recover(0.2f, 2.5f, 0.4f);
            Assert.That(bloom.Value, Is.EqualTo(1.25f).Within(1e-4f));
            bloom.Recover(0.2f, 2.5f, 0.4f);
            Assert.That(bloom.Value, Is.EqualTo(0f));
        }
    }
}
