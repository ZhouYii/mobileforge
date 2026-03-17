extends MFTestBase
class_name MFTestLoginBonus
    func test_check_in_grants_first_day_reward():
        var schedule = [
            {"day": 1, "type": "currency", "currency": "coins", "count": 100},
            {"day": 2, "type": "currency", "currency": "gems", "count": 10},
        ]
        var player_state = createMockPlayerState()
        var lb = MFLoginBonus.new(player_state, schedule)
        var result = lb.check_in()
        assert_true(result.is_new_day)
        assert_eq(result.streak, 1)
        assert_eq(result.day_in_cycle, 1)
        assert_not_null(result.reward)
        assert_eq(result.reward.currency, "coins")
        assert_eq(result.reward.count, 100)
    end

    func test_check_in_increments_streak():
        var schedule = [
            {"day": 1, "type": "currency", "currency": "coins", "count": 100},
            {"day": 2, "type": "currency", "currency": "gems", "count": 10},
            {"day": 3, "type": "currency", "currency": "coins", "count": 200},
        ]
        var player_state = createMockPlayerState()
        var lb = MFLoginBonus.new(player_state, schedule)
        lb.check_in()
        lb.check_in()
        var result = lb.check_in()
        assert_eq(result.streak, 3)
        assert_eq(result.day_in_cycle, 3)
    end

    func test_check_in_resets_streak_after_gap():
        var schedule = [
            {"day": 1, "type": "currency", "currency": "coins", "count": 100},
            {"day": 2, "type": "currency", "currency": "gems", "count": 10},
        ]
        var player_state = createMockPlayerState()
        var lb = MFLoginBonus.new(player_state, schedule)
        lb.check_in()
        var section = player_state.get_section(&"login_bonus")
        section.set_value(&"last_login_date", "2020-01-01")
        var result = lb.check_in()
        assert_eq(result.streak, 1)
    end

    func test_check_in_returns_null_if_already_checked_in():
        var schedule = [
            {"day": 1, "type": "currency", "currency": "coins", "count": 100},
        ]
        var player_state = createMockPlayerState()
        var lb = MFLoginBonus.new(player_state, schedule)
        var result1 = lb.check_in()
        var result2 = lb.check_in()
        assert_null(result2.reward)
    end

    func test_cycle_wraps_correctly():
        var schedule = [
            {"day": 1, "type": "currency", "currency": "coins", "count": 100},
            {"day": 2, "type": "currency", "currency": "gems", "count": 10},
        ]
        var player_state = createMockPlayerState()
        var lb = MFLoginBonus.new(player_state, schedule)
        lb.check_in()
        lb.check_in()
        var result = lb.check_in()
        assert_eq(result.day_in_cycle, 1)
    end

    func createMockPlayerState() -> MockPlayerState
    {
        return MockPlayerState.new()
    end

    class MockPlayerState extends RefCounted
    {
        var _sections: Dictionary = {}
        func get_section(name: StringName) -> Dictionary
        {
            if not _sections.has(name):
                _sections[name] = {}
            return _sections[name]
        }
    end
}