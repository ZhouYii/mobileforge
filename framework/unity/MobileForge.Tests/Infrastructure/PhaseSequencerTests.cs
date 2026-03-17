using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class PhaseSequencerTests
    {
        private PhaseSequencer _sequencer;

        [SetUp]
        public void SetUp()
        {
            _sequencer = new PhaseSequencer();
            _sequencer.AddPhase(new PhaseDef("phase1") { Duration = 1.0f });
            _sequencer.AddPhase(new PhaseDef("phase2") { Duration = 0.5f });
        }

        [Test]
        public void Start_BeginsFirstPhase()
        {
            _sequencer.Start();
            Assert.AreEqual("phase1", _sequencer.CurrentPhaseId);
            Assert.IsTrue(_sequencer.IsRunning);
        }

        [Test]
        public void Update_AdvancesAfterDuration()
        {
            _sequencer.Start();
            _sequencer.Update(1.0f);
            Assert.AreEqual("phase2", _sequencer.CurrentPhaseId);
        }

        [Test]
        public void Update_CompletesSequence()
        {
            bool completed = false;
            _sequencer.SequenceCompleted += () => completed = true;
            _sequencer.Start();
            _sequencer.Update(1.0f); // past phase1
            _sequencer.Update(0.5f); // past phase2
            Assert.IsTrue(completed);
            Assert.IsTrue(_sequencer.IsComplete);
        }

        [Test]
        public void Advance_SkipsToNextPhase()
        {
            _sequencer.Start();
            _sequencer.Advance();
            Assert.AreEqual("phase2", _sequencer.CurrentPhaseId);
        }

        [Test]
        public void Reset_ClearsState()
        {
            _sequencer.Start();
            _sequencer.Reset();
            Assert.AreEqual(-1, _sequencer.CurrentIndex);
            Assert.IsFalse(_sequencer.IsRunning);
            Assert.IsFalse(_sequencer.IsComplete);
        }

        [Test]
        public void AutoAdvance_SkipsPhaseImmediately()
        {
            var seq = new PhaseSequencer();
            seq.AddPhase(new PhaseDef("auto") { AutoAdvance = true });
            seq.AddPhase(new PhaseDef("manual") { Duration = 1.0f });
            seq.Start();
            Assert.AreEqual("manual", seq.CurrentPhaseId);
        }

        [Test]
        public void Barrier_WaitsForResolution()
        {
            var barrier = new AsyncBarrier();
            barrier.Add("token1");

            var seq = new PhaseSequencer();
            seq.AddPhase(new PhaseDef("wait") { Barrier = barrier });
            seq.AddPhase(new PhaseDef("after") { Duration = 1.0f });
            seq.Start();
            Assert.AreEqual("wait", seq.CurrentPhaseId);

            barrier.Resolve("token1");
            Assert.AreEqual("after", seq.CurrentPhaseId);
        }
    }
}
