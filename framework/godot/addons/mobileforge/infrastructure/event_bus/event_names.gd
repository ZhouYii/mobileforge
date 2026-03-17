class_name EventNames
## StringName constants for all framework events.
## Game-specific events should be defined in game code, not here.

# Infrastructure
const STATE_LOADED := &"state_loaded"
const STATE_CHANGED := &"state_changed"
const SAVE_COMPLETED := &"save_completed"
const SAVE_FAILED := &"save_failed"

# Domain - Board
const BOARD_INITIALIZED := &"board_initialized"
const CASCADE_RESOLVED := &"cascade_resolved"
const COMBO_HIT := &"combo_hit"

# Domain - Combat
const DAMAGE_DEALT := &"damage_dealt"
const ENEMY_KILLED := &"enemy_killed"
const HEALING_APPLIED := &"healing_applied"

# Domain - Dungeon
const WAVE_STARTED := &"wave_started"
const WAVE_CLEARED := &"wave_cleared"
const BATTLE_WON := &"battle_won"
const BATTLE_LOST := &"battle_lost"
const ENEMY_ATTACKED := &"enemy_attacked"

# Domain - Skill
const SKILL_ACTIVATED := &"skill_activated"
const SKILL_DEACTIVATED := &"skill_deactivated"
const BUFF_APPLIED := &"buff_applied"
const BUFF_EXPIRED := &"buff_expired"

# Domain - Economy
const CURRENCY_CHANGED := &"currency_changed"
const STAMINA_CHANGED := &"stamina_changed"

# Domain - Monster
const MONSTER_ADDED := &"monster_added"
const MONSTER_LEVELED := &"monster_leveled"
const MONSTER_EVOLVED := &"monster_evolved"
const MONSTER_FUSED := &"monster_fused"

# Domain - Reward
const REWARDS_GRANTED := &"rewards_granted"

# Domain - Shop
const SHOP_PURCHASE := &"shop_purchase"

# Domain - Tutorial
const TUTORIAL_STARTED := &"tutorial_started"
const TUTORIAL_STEP := &"tutorial_step"
const TUTORIAL_COMPLETED := &"tutorial_completed"

# Domain - Battle Pass
const BATTLE_PASS_TIER_UP := &"battle_pass_tier_up"
const BATTLE_PASS_CLAIMED := &"battle_pass_claimed"
const BATTLE_PASS_PREMIUM_ACTIVATED := &"battle_pass_premium_activated"

# Domain - Quest
const QUEST_ACTIVATED := &"quest_activated"
const QUEST_PROGRESS := &"quest_progress"
const QUEST_COMPLETED := &"quest_completed"
const QUEST_CLAIMED := &"quest_claimed"

# Domain - Inventory
const INVENTORY_CHANGED := &"inventory_changed"
const ITEM_EQUIPPED := &"item_equipped"
const ITEM_UNEQUIPPED := &"item_unequipped"

# Infrastructure - Badge
const BADGE_CHANGED := &"badge_changed"

# Infrastructure - Preferences
const PREFERENCE_CHANGED := &"preference_changed"

# Presentation
const SCREEN_CHANGED := &"screen_changed"
const POPUP_SHOWN := &"popup_shown"
const POPUP_DISMISSED := &"popup_dismissed"
