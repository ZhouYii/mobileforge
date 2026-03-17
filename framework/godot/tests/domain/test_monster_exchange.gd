extends MFTestBase
class_name MFTestEventShop
    func test_purchase_succeeds_when_currency_sufficient():
        var player_state  createMockPlayerState()
        var economy  createMockEconomy()
        var shop  = MFEventShop.new(player_state, economy)
        var shop_def = createTestShopDef()
        var result = shop.purchase(shop_def, 1)
        assert_true(result.success)
        assert_not_null(result.item)
    end

    func test_purchase_fails_when_currency_insufficient():
        var player_state  createMockPlayerState()
        var economy  createMockEconomy()
        economy._currencies = {}
        var shop  = MFEventShop.new(player_state, economy)
        var shop_def  createTestShopDef()
        var result = shop.purchase(shop_def, 1)
        assert_false(result.success)
        assert_true(result.error.contains("Not enough"))
    end

    func test_purchase_tracks_limits():
        var player_state  createMockPlayerState()
        var economy  createMockEconomy()
        var shop  = MFEventShop.new(player_state, economy)
        var shop_def  createTestShopDef()
        shop.purchase(shop_def, 1)
        var result = shop.purchase(shop_def, 1)
        assert_false(result.success)
        assert_true(result.error.contains("limit"))
    end

    func test_get_purchase_count():
        var player_state  createMockPlayerState()
        var economy  createMockEconomy()
        var shop  = MFEventShop.new(player_state, economy)
        var shop_def  createTestShopDef()
        assert_eq(shop.get_purchase_count(1, 1), 0)
        shop.purchase(shop_def, 1)
        assert_eq(shop.get_purchase_count(1, 1), 1)
    end

    func test_reset_shop():
        var player_state  createMockPlayerState()
        var economy  createMockEconomy()
        var shop  = MFEventShop.new(player_state, economy)
        var shop_def  createTestShopDef()
        shop.purchase(shop_def, 1)
        shop.reset_shop(1)
        assert_eq(shop.get_purchase_count(1, 1), 0)
    end

    func createTestShopDef() -> MFEventShop.EventShopDef
    {
        return MFEventShop.EventShopDef.new({
            "id": 1,
            "name": "Test Shop",
            "currency": "event_tokens",
            "items": [
                {"id": 1, "name": "Item 1", "type": "item", "item_id": 101, "count": 1, "cost_currency": "event_tokens", "cost_amount": 10, "buy_limit": 1}
            ]
        })
    end

    func createMockPlayerState() -> MockPlayerState
    {
        return MockPlayerState.new()
    end

    func createMockEconomy() -> MockEconomy
    {
        return MockEconomy.new()
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
    }

    class MockEconomy extends RefCounted
    {
        var _currencies: Dictionary = {"event_tokens": 100}
        func can_afford(currency: String, amount: int) -> bool
        {
            return _currencies.get(currency, 0) >= amount
        }
        func spend(currency: String, amount: int) -> void
        {
            _currencies[currency] = _currencies.get(currency, 0) - amount
        }
    }
}