using System;
using System.IO;
using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using UnityEngine;

namespace RangeControl.Tests
{
    public class GroupSizeTests
    {
        [Test]
        public void FewerThanTwoHoles_HaveNoGroup()
        {
            Assert.That(GroupSize.Compute(new Vector2[4], 0), Is.EqualTo(0f));
            Assert.That(GroupSize.Compute(new[] { new Vector2(0.1f, 0.1f) }, 1), Is.EqualTo(0f));
        }

        [Test]
        public void Group_IsTheWidestDistanceBetweenAnyTwoHoles()
        {
            var holes = new[] { new Vector2(0f, 0f), new Vector2(0.03f, 0f), new Vector2(0f, 0.04f), new Vector2(0.01f, 0.01f) };
            Assert.That(GroupSize.Compute(holes, holes.Length), Is.EqualTo(0.05f).Within(1e-6f));
        }

        [Test]
        public void OnlyTheFirstCountHolesAreMeasured()
        {
            var holes = new[] { new Vector2(0f, 0f), new Vector2(0.02f, 0f), new Vector2(5f, 5f) };
            Assert.That(GroupSize.Compute(holes, 2), Is.EqualTo(0.02f).Within(1e-6f));
        }
    }

    public class RngTests
    {
        [Test]
        public void SameSeed_GivesTheSameSequence()
        {
            var first = new Rng(1234);
            var second = new Rng(1234);
            for (var i = 0; i < 100; i++)
                Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
        }

        [Test]
        public void NextFloat_StaysInZeroToOneAndSpreadsOut()
        {
            var rng = new Rng(99);
            var sum = 0f;
            for (var i = 0; i < 10000; i++)
            {
                var value = rng.NextFloat();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                sum += value;
            }

            Assert.That(sum / 10000f, Is.EqualTo(0.5f).Within(0.02f));
        }

        [Test]
        public void DefaultStruct_StillProducesNumbers()
        {
            var rng = default(Rng);
            Assert.That(rng.NextUInt(), Is.Not.EqualTo(rng.NextUInt()));
        }
    }

    public class PresetStoreTests
    {
        string m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Path = Path.Combine(Path.GetTempPath(), "RangeControlPresets-" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(m_Path))
                File.Delete(m_Path);
        }

        [Test]
        public void SlotsStartEmpty()
        {
            var store = new PresetStore(m_Path);
            store.Load();
            for (var i = 0; i < PresetStore.SlotCount; i++)
                Assert.That(store.Get(i).used, Is.False);
        }

        [Test]
        public void SavedSlot_ComesBackAfterAReload()
        {
            var setup = RangeSettingsData.Default;
            setup.targetDistance = 35f;
            setup.pattern = MovementPattern.Strafe;
            setup.infiniteAmmo = true;
            new PresetStore(m_Path).Set(1, 2, 1, setup);

            var store = new PresetStore(m_Path);
            store.Load();

            Assert.That(store.Get(0).used, Is.False);
            Assert.That(store.Get(1).used, Is.True);
            Assert.That(store.Get(1).modeIndex, Is.EqualTo(2));
            Assert.That(store.Get(1).tierIndex, Is.EqualTo(1));
            Assert.That(store.Get(1).settings.targetDistance, Is.EqualTo(35f));
            Assert.That(store.Get(1).settings.pattern, Is.EqualTo(MovementPattern.Strafe));
            Assert.That(store.Get(1).settings.infiniteAmmo, Is.True);
        }
    }
}
