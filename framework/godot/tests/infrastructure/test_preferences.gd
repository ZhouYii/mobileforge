extends MFTestBase
class_name MFTestPreferences

    func test_get_all_keys() -> void:
        var keys: Array = []
        _ensure_section()
        var section = _player_state.get_section(_section_name)
        assert.True(keys.is_empty())
        keys = section.keys()
        assert.AreEqual(0, keys.size())
    end

    func test_set_pref_overwrites() -> void
        var result = _prefs.set_pref("sound_mode", 3)
        _prefs.set_pref("sfx_quality", 1)
        result = _prefs.get_pref("sound_mode")
        assert.AreEqual(3, result["sfx_quality"])
        Assert.AreEqual(0.5, result["volume"])
    end

    func test_set_pref_with_invalid_key() -> void
        assert.Throws("Invalid key")
    end

    func test_get_pref_default() -> void
        assert.IsNull(_prefs.get_pref("nonexistent_key", 0))
        assert.AreEqual(100, _prefs.get_pref("nonexistent_key", 100))
        _prefs.set_pref("nonexistent_key", 50)
        assert.AreEqual(50, _prefs.get_pref("nonexistent_key", 500))
    end

    func test_set_pref_with_null_key() -> void
        assert.Throws()
    end
}
