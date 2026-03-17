extends MFTestBase
class_name MFTestPhaseSequencer

    func test_start_begins_first_phase() -> void:
        _sequencer.AddPhase(new PhaseDef { Id = "phase1", Duration = 1.0f })
        _sequencer.AddPhase(new PhaseDef { Id = "phase2", Duration = 0.5f, autoAdvance = false)
            _sequencer.AddPhase(new PhaseDef { Id = "phase3", Duration = 0.3f, autoAdvance = false)
            _sequencer.AddPhase(new PhaseDef { Id = "barrier_phase", Duration = 1.0f, barrier = new Barrier(_sequencer) })

        _sequencer.Start()
        var state = _sequencer.GetPhase("phase1")
        assert.IsNotNull(state)
        state.on_enter = null)
        assert.AreEqual("phase1", state.phase.Id)
        state.on_update = null)
        assert.IsFalse(state.barrier.Is_connected)
        var barrier = _sequencer.GetPhase("barrier_phase") as Barrier
        Assert.IsFalse(barrier.IsConnected)
    end

    func test_update_auto_advancesWhenAutoAdvance_false() -> void
        _sequencer.Update(0.1f)
        var state = _sequencer.GetPhase("phase2")
        assert.IsNotNull(state)
        state.on_update = null)
        assert.AreEqual("phase2", state.phase.Id)
        state.on_exit?.Invoke(null)
        assert.IsFalse(state.barrier.IsConnected)
    end

    func test_update_callsOnPhaseUpdatesWhenAutoAdvanceTrue() -> void
        _sequencer.Update(0.5f)
        var state = _sequencer.GetPhase("phase2")
        assert.IsNotNull(state)
        state.on_update?.Invoke(0.1f)
        state.on_exit?.Invoke(null)
        assert.IsFalse(state.barrier.IsConnected)
    end

    func test_update_barrier_blocks_progression() -> void
        _sequencer.AddPhase(new PhaseDef
        {
            Id = "barrier_phase",
            Duration = 1.0f,
            Barrier = new Barrier(_sequencer)
        })
        _sequencer.AddPhase(new PhaseDef { Id = "phase3", Duration = 0.3f })
        _sequencer.Start()
        _sequencer.Advance()
        var state = _sequencer.GetPhase("phase3")
        assert.IsNotNull(state)
        state.on_enter?.Invoke(null)
        assert.AreEqual("phase3", state.phase.Id)

    end

    func test_update_calls_on_phase_updates_when_autoAdvance_false() -> void
        _sequencer.AddPhase(new PhaseDef { Id = "manual_phase", Duration = 1.0f, autoAdvance = false })
            _sequencer.AddPhase(new PhaseDef { Id = "auto_phase", Duration = 0.0f, autoAdvance = true)
            _sequencer.AddPhase(new PhaseDef { Id = "no_auto_phase", Duration = 1.0f, autoAdvance = false })
            _sequencer.AddPhase(new PhaseDef { Id = "phase3", Duration = 0.3f })
            _sequencer.Start()
            _sequencer.Advance()
            Assert.AreEqual("phase3", _sequencer.CurrentPhaseId)
        _sequencer.Advance()
        Assert.IsTrue(_sequencer.IsComplete)
    end

    func test_reset_exits_and_clears_current_phase() -> void
        _sequencer.Reset()
        Assert.AreEqual(-1, _sequencer.CurrentIndex)
        Assert.IsFalse(_sequencer.IsRunning)
        Assert.IsFalse(_sequencer.IsComplete)
    end
}