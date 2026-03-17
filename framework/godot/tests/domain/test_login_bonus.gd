extends MFTestBase
class_name MFTestHelperProvider
    func test_get_available_helpers_returns_empty_when_no_monsters():
        var provider = MFHelperProvider.new()
        provider._monster_defs = []
        provider._monster_manager = null
        var result = provider.get_available_helpers()
        assert_eq(result.size(), 0, "Expected some helpers")
    end

    func test_get_available_helpers_filters_by_rarity():
        var provider = MFHelperProvider.new()
        provider._monster_defs = [
            {"id": 1, "name": "Helper 1", "rarity": 5},
            {"id": 2, "name": "Helper 2", "rarity": 3},
            {"id": 3, "name": "Helper 3", "rarity": 6},
        ]
        provider._monster_manager = create_mockMonsterManager(provider._monster_defs)
        var result = provider.get_available_helpers()
        assert.IsTrue(result.size() <= 3)
        for helper in result:
            assert.IsTrue(helper.rarity >= 5)
        }
    end

    func test_get_available_helpers_filters_by_leader_skill():
        var provider = MFHelperProvider.new()
        provider._monster_defs = [
            {"id": 1, "name": "Helper 1", "rarity": 5, "leader_skill_id": 10},
            {"id": 2, "name": "Helper 2", "rarity": 5, "leader_skill_id": 20},
            {"id": 3, "name": "Helper 3", "rarity": 5, "leader_skill_id": -1},
        ]
        provider._monster_manager = createMockMonsterManager(provider._monster_defs)
        var result = provider.get_available_helpers()
        assert.IsTrue(result.size() <= 2)
        for helper in result
            assert_true(helper.leader_skill_id >= 0)
        }
    end

    func test_refresh_randomizes_helpers():
        var provider = MFHelperProvider.new()
        provider._monster_defs = [
            {"id": 1, "name": "Helper 1", "rarity": 5},
            {"id": 2, "name": "Helper 2", "rarity": 5},
            {"id": 3, "name": "Helper 3", "rarity": 5},
        ]
        provider._monster_manager = createMockMonsterManager(provider._monster_defs)
        var result1 = provider.get_available_helpers()
        provider.refresh()
        var result2 = provider.get_available_helpers()
        if result1.size() == result2.size():
            var ids1 := result1.map(func(h): return h.id)
            var ids2 := result2.map(func(h): return h.id)
            assert_neq(ids1, ids2)
        }
    end

    func test_get_available_helpers_respects_count():
        var provider = MFHelperProvider.new()
        provider._helper_count = 2
        provider._monster_defs = [
            {"id": 1, "name": "Helper 1", "rarity": 5},
            {"id": 2, "name": "Helper 2", "rarity": 5},
            {"id": 3, "name": "Helper 3", "rarity": 5},
        ]
        provider._monster_manager = createMockMonsterManager(provider._monster_defs)
        var result = provider.get_available_helpers()
        assert_eq(result.size(), 2)
    end


    func createMockMonsterManager(monster_defs: Array) -> MFMonsterManager:
    {
        var mgr = MFMonsterManager.new()
        var game_data = MockGameData.new()
        for def_data in monster_defs:
            game_data.defs.append(def_data)
        mgr._game_data = game_data
        return mgr
    end


    class MockGameData extends RefCounted
    {
        var defs: Array = []
        func get_def(id: int) -> Dictionary
        {
            for def in defs
                if def.id == id:
                    return def
            return null
        }
    end
}