class_name MFBoardEvents
## Board-specific event name constants.
## These extend the framework EventNames with board-specific events.

const GEM_SWAPPED := &"board_gem_swapped"
const GEM_SPAWNED := &"board_gem_spawned"
const GEM_REMOVED := &"board_gem_removed"
const GEM_DROPPED := &"board_gem_dropped"
const GEM_STATUS_APPLIED := &"board_gem_status_applied"
const GEM_STATUS_REMOVED := &"board_gem_status_removed"
const MATCH_FOUND := &"board_match_found"
const CASCADE_STEP := &"board_cascade_step"
const BOARD_SETTLED := &"board_settled"  # No more matches after cascade
