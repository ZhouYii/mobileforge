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

# Presentation
const SCREEN_CHANGED := &"screen_changed"
const POPUP_SHOWN := &"popup_shown"
const POPUP_DISMISSED := &"popup_dismissed"
